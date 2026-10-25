using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IPriceHistoryService"/> que cachea el histórico diario
    /// en SQLite (tabla PriceHistoryPoints) y solo consulta a Yahoo Finance cuando la caché
    /// está vacía o desactualizada. La agregación semanal/mensual/anual se calcula siempre en C#
    /// a partir de los puntos diarios ya cacheados.
    /// SPEC-05-02: el histórico se descarga por bloques (2 años recientes y bloques de 10 años hacia atrás
    /// hasta la primera cotización) y las barras de una hora se guardan solo en memoria.
    /// </summary>
    public class PriceHistoryService : IPriceHistoryService
    {
        // Si el punto más reciente en caché tiene menos de este tiempo, se considera
        // "fresco" y no se vuelve a consultar Yahoo Finance.
        private static readonly TimeSpan CacheFreshness = TimeSpan.FromHours(12);

        // Bloques de descarga del histórico diario (SPEC-05-02, D7)
        private const int RecentBlockYears = 2;
        private const int OlderBlockYears = 10;

        // Días que se vuelven a pedir al actualizar, para corregir el último cierre provisional (RF-3)
        private const int RefreshOverlapDays = 7;

        // Barras de una hora: primer bloque, bloques siguientes, límite de Yahoo y vigencia en memoria (RF-9)
        private const int HourlyInitialDays = 31;
        private const int HourlyBlockMonths = 6;
        private const int HourlyMaxDays = 729;
        private static readonly TimeSpan HourlyCacheLifetime = TimeSpan.FromMinutes(15);

        // Yahoo no admite fechas anteriores a 1970 en period1: el histórico más antiguo se corta ahí
        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Claves de AppSettings que marcan el histórico diario de una empresa o de un índice como completo (RF-2)
        private const string CompleteKeyPrefix = "PriceHistory.Complete.";
        private const string BenchmarkCompleteKeyPrefix = "BenchmarkHistory.Complete.";

        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;

        // El servicio vive toda la app (Scoped en MAUI Blazor Hybrid): la memoria de Horas dura entre aperturas del modal.
        // Clave por serie (empresa o índice, SPEC-05-04)
        private readonly Dictionary<string, HourlyCacheEntry> _hourlyCache = new();
        private readonly object _hourlyLock = new();

        public PriceHistoryService(AppDbContext context, IStockPriceService stockPriceService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
        }

        /// <summary>
        /// Clave de AppSettings que indica que el histórico diario de la empresa está completo
        /// </summary>
        public static string GetCompleteKey(int companyId) => CompleteKeyPrefix + companyId;

        /// <summary>
        /// Clave de AppSettings que indica que el histórico diario del índice está completo (SPEC-05-04)
        /// </summary>
        public static string GetBenchmarkCompleteKey(int benchmarkIndexId) => BenchmarkCompleteKeyPrefix + benchmarkIndexId;

        private static string GetCompleteKey(ChartSeriesRef series) => series.Kind == ChartSeriesKind.Benchmark
            ? GetBenchmarkCompleteKey(series.Id)
            : GetCompleteKey(series.Id);

        public async Task<List<PriceHistoryPoint>> GetHistoryAsync(int companyId, string ticker, ChartGranularity granularity)
        {
            var result = await GetHistoryWithStatusAsync(companyId, ticker, granularity, false);
            return result.Points;
        }

        public Task<PriceHistoryResult> GetHistoryWithStatusAsync(int companyId, string ticker, ChartGranularity granularity, bool forceRefresh)
        {
            return GetSeriesHistoryWithStatusAsync(ChartSeriesRef.ForCompany(companyId, ticker), granularity, forceRefresh);
        }

        public Task<PriceHistoryResult> GetCachedHistoryAsync(int companyId, ChartGranularity granularity)
        {
            return GetCachedSeriesHistoryAsync(ChartSeriesRef.ForCompany(companyId, string.Empty), granularity);
        }

        public async Task<PriceHistoryResult> GetSeriesHistoryWithStatusAsync(ChartSeriesRef series, ChartGranularity granularity, bool forceRefresh)
        {
            var refreshFailed = await RefreshCacheIfNeededAsync(series, forceRefresh);
            return await BuildResultAsync(series, granularity, refreshFailed);
        }

        public Task<PriceHistoryResult> GetCachedSeriesHistoryAsync(ChartSeriesRef series, ChartGranularity granularity)
        {
            return BuildResultAsync(series, granularity, false);
        }

        // ---------------------------------------------------------------- Histórico diario reciente

        /// <summary>
        /// Refresca la caché si está desactualizada o si se fuerza. Devuelve true si se intentó descargar
        /// y Yahoo no devolvió datos.
        /// </summary>
        private async Task<bool> RefreshCacheIfNeededAsync(ChartSeriesRef series, bool forceRefresh)
        {
            // La frescura se mide con el punto más reciente: los bloques antiguos se guardan con la hora
            // actual y no deben hacer pasar por fresca una caché cuyo último día es viejo
            var latest = await GetLatestAsync(series);

            var isFresh = latest.HasValue && DateTime.Now - latest.Value.FetchedAt < CacheFreshness;
            if (isFresh && !forceRefresh)
            {
                return false;
            }

            // Caché vacía o recarga forzada: los últimos 2 años (RF-1, RF-13); si no, solo los días nuevos (RF-3)
            var from = !latest.HasValue || forceRefresh
                ? DateTime.UtcNow.Date.AddYears(-RecentBlockYears)
                : latest.Value.Date.Date.AddDays(-RefreshOverlapDays);

            var chunk = await _stockPriceService.GetHistoryRangeAsync(series.Symbol, "1d", from, DateTime.UtcNow.AddDays(1));
            if (chunk.Bars.Count == 0)
            {
                // No se pudo obtener nada nuevo; se sigue sirviendo lo que hubiera en caché.
                return true;
            }

            var touched = await UpsertDailyBarsAsync(series, chunk.Bars);
            await _context.SaveChangesAsync();
            Detach(touched);
            return false;
        }

        private async Task<PriceHistoryResult> BuildResultAsync(ChartSeriesRef series, ChartGranularity granularity, bool refreshFailed)
        {
            var dailyPoints = await LoadDailyPointsAsync(series);

            // Dinero negociado aproximado de cada sesión: acciones × cierre (SPEC-05-03, RF-1)
            foreach (var point in dailyPoints)
            {
                point.TradedValue = point.Volume * point.Close;
            }

            // Horas no sale del diario (se pide con GetHourlyAsync): si llega aquí, se devuelve el diario
            var points = granularity switch
            {
                ChartGranularity.Daily => dailyPoints,
                ChartGranularity.Weekly or ChartGranularity.Monthly or ChartGranularity.Yearly
                    => Aggregate(dailyPoints, p => PeriodStart(p.Date, granularity)),
                _ => dailyPoints,
            };

            var (viewStart, viewEnd) = GetInitialView(granularity, points);

            return new PriceHistoryResult
            {
                Points = points,
                RefreshFailed = refreshFailed,
                ViewStart = viewStart,
                ViewEnd = viewEnd,
                IsComplete = await IsCompleteAsync(series),
                OldestDate = dailyPoints.Count > 0 ? dailyPoints[0].Date : null
            };
        }

        /// <summary>
        /// Tramo visible al abrir cada temporalidad: cuanto más fina, menos tiempo (SPEC-05-02, RF-8).
        /// Termina en el último punto; null en el inicio = todo el histórico.
        /// </summary>
        public static (DateTime? Start, DateTime? End) GetInitialView(ChartGranularity granularity, IReadOnlyList<PriceHistoryPoint> points)
        {
            if (points.Count == 0)
            {
                return (null, null);
            }

            var first = points[0].Date;
            var last = points[^1].Date;

            DateTime? start = granularity switch
            {
                ChartGranularity.Hourly => last.AddMonths(-1),
                ChartGranularity.Daily => last.AddYears(-1),
                ChartGranularity.Weekly => last.AddYears(-5),
                ChartGranularity.Monthly => last.AddYears(-20),
                _ => null,
            };

            // Con menos histórico que el tramo se ve todo
            if (start.HasValue && start.Value <= first)
            {
                start = null;
            }

            return (start, last);
        }

        // ---------------------------------------------------------------- Histórico diario antiguo (segundo plano)

        public Task<HistoryBackfillState> GetBackfillStateAsync(int companyId)
        {
            return GetSeriesBackfillStateAsync(ChartSeriesRef.ForCompany(companyId, string.Empty));
        }

        public async Task<HistoryBackfillState> GetSeriesBackfillStateAsync(ChartSeriesRef series)
        {
            return new HistoryBackfillState
            {
                IsComplete = await IsCompleteAsync(series),
                OldestDate = await GetOldestDateAsync(series)
            };
        }

        public async Task<HistoryBlockResult> FetchOlderDailyBlockAsync(string ticker, DateTime beforeDate, CancellationToken cancellationToken)
        {
            var to = DateTime.SpecifyKind(beforeDate.Date, DateTimeKind.Utc);
            var from = to.AddYears(-OlderBlockYears);
            var reachedEpoch = false;
            if (from <= UnixEpoch)
            {
                from = UnixEpoch;
                reachedEpoch = true;
            }

            var chunk = await _stockPriceService.GetHistoryRangeAsync(ticker, "1d", from, to, cancellationToken);
            if (chunk.Failed)
            {
                return new HistoryBlockResult { Failed = true, BlockStart = from };
            }

            var bars = chunk.Bars.Where(b => b.Date.Date < beforeDate.Date).ToList();

            // Con firstTradeDate se sigue hasta ella aunque un bloque venga vacío (suspensiones);
            // sin ella, el primer bloque vacío marca el final
            var reachedStart = reachedEpoch || (chunk.FirstTradeDateUtc.HasValue
                ? from <= chunk.FirstTradeDateUtc.Value.Date
                : bars.Count == 0);

            return new HistoryBlockResult
            {
                Bars = bars,
                BlockStart = from,
                OldestDate = bars.Count > 0 ? bars.Min(b => b.Date.Date) : null,
                ReachedStart = reachedStart
            };
        }

        public Task SaveDailyBlockAsync(int companyId, HistoryBlockResult block)
        {
            return SaveSeriesDailyBlockAsync(ChartSeriesRef.ForCompany(companyId, string.Empty), block);
        }

        public async Task SaveSeriesDailyBlockAsync(ChartSeriesRef series, HistoryBlockResult block)
        {
            var touched = await UpsertDailyBarsAsync(series, block.Bars);

            if (block.ReachedStart)
            {
                var key = GetCompleteKey(series);
                var setting = await _context.AppSettings.FindAsync(key);
                if (setting == null)
                {
                    _context.AppSettings.Add(new AppSetting { Key = key, TextValue = "true", UpdatedAt = DateTime.Now });
                }
                else
                {
                    setting.TextValue = "true";
                    setting.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            Detach(touched);
        }

        private async Task<bool> IsCompleteAsync(ChartSeriesRef series)
        {
            var key = GetCompleteKey(series);
            return await _context.AppSettings
                .AsNoTracking()
                .AnyAsync(s => s.Key == key && s.TextValue == "true");
        }

        // ---------------------------------------------------------------- Almacén por tipo de serie (SPEC-05-04)

        /// <summary>
        /// Fecha y hora de guardado del punto diario más reciente de la serie; null si no hay caché
        /// </summary>
        private async Task<(DateTime Date, DateTime FetchedAt)?> GetLatestAsync(ChartSeriesRef series)
        {
            if (series.Kind == ChartSeriesKind.Benchmark)
            {
                var latest = await _context.BenchmarkPricePoints
                    .AsNoTracking()
                    .Where(p => p.BenchmarkIndexId == series.Id)
                    .OrderByDescending(p => p.Date)
                    .Select(p => new { p.Date, p.FetchedAt })
                    .FirstOrDefaultAsync();
                return latest == null ? null : (latest.Date, latest.FetchedAt);
            }

            var companyLatest = await _context.PriceHistoryPoints
                .AsNoTracking()
                .Where(p => p.CompanyId == series.Id)
                .OrderByDescending(p => p.Date)
                .Select(p => new { p.Date, p.FetchedAt })
                .FirstOrDefaultAsync();
            return companyLatest == null ? null : (companyLatest.Date, companyLatest.FetchedAt);
        }

        private async Task<DateTime?> GetOldestDateAsync(ChartSeriesRef series)
        {
            if (series.Kind == ChartSeriesKind.Benchmark)
            {
                return await _context.BenchmarkPricePoints
                    .AsNoTracking()
                    .Where(p => p.BenchmarkIndexId == series.Id)
                    .OrderBy(p => p.Date)
                    .Select(p => (DateTime?)p.Date)
                    .FirstOrDefaultAsync();
            }

            return await _context.PriceHistoryPoints
                .AsNoTracking()
                .Where(p => p.CompanyId == series.Id)
                .OrderBy(p => p.Date)
                .Select(p => (DateTime?)p.Date)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Puntos diarios guardados de la serie, del más antiguo al más reciente. Los de un índice se devuelven
        /// como PriceHistoryPoint (CompanyId = 0) para que el resto del servicio y el gráfico los traten igual.
        /// </summary>
        private async Task<List<PriceHistoryPoint>> LoadDailyPointsAsync(ChartSeriesRef series)
        {
            if (series.Kind == ChartSeriesKind.Benchmark)
            {
                return await _context.BenchmarkPricePoints
                    .AsNoTracking()
                    .Where(p => p.BenchmarkIndexId == series.Id)
                    .OrderBy(p => p.Date)
                    .Select(p => new PriceHistoryPoint
                    {
                        Id = p.Id,
                        Date = p.Date,
                        Open = p.Open,
                        High = p.High,
                        Low = p.Low,
                        Close = p.Close,
                        Volume = p.Volume,
                        FetchedAt = p.FetchedAt
                    })
                    .ToListAsync();
            }

            return await _context.PriceHistoryPoints
                .AsNoTracking()
                .Where(p => p.CompanyId == series.Id)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Actualiza por fecha los puntos existentes y añade los nuevos (sin guardar). Devuelve las entidades
        /// tocadas para soltarlas del contexto tras guardar: el DbContext es de toda la app y no debe
        /// acumular miles de puntos de histórico en seguimiento.
        /// </summary>
        private async Task<List<object>> UpsertDailyBarsAsync(ChartSeriesRef series, IReadOnlyList<StockHistoryBar> bars)
        {
            var touched = new List<object>();
            if (bars.Count == 0)
            {
                return touched;
            }

            // Una barra por día (Yahoo puede repetir el día en curso)
            var barsByDate = bars
                .GroupBy(b => b.Date.Date)
                .Select(g => g.Last())
                .ToList();

            var minDate = barsByDate.Min(b => b.Date.Date);
            var maxDate = barsByDate.Max(b => b.Date.Date);
            var now = DateTime.Now;

            if (series.Kind == ChartSeriesKind.Benchmark)
            {
                var existingBenchmark = await _context.BenchmarkPricePoints
                    .Where(p => p.BenchmarkIndexId == series.Id && p.Date >= minDate && p.Date <= maxDate)
                    .ToDictionaryAsync(p => p.Date.Date);

                foreach (var bar in barsByDate)
                {
                    if (!existingBenchmark.TryGetValue(bar.Date.Date, out var point))
                    {
                        point = new BenchmarkPricePoint { BenchmarkIndexId = series.Id, Date = bar.Date.Date };
                        _context.BenchmarkPricePoints.Add(point);
                    }

                    point.Open = bar.Open;
                    point.High = bar.High;
                    point.Low = bar.Low;
                    point.Close = bar.Close;
                    point.Volume = bar.Volume;
                    point.FetchedAt = now;
                    touched.Add(point);
                }

                return touched;
            }

            var existingByDate = await _context.PriceHistoryPoints
                .Where(p => p.CompanyId == series.Id && p.Date >= minDate && p.Date <= maxDate)
                .ToDictionaryAsync(p => p.Date.Date);

            foreach (var bar in barsByDate)
            {
                if (existingByDate.TryGetValue(bar.Date.Date, out var existing))
                {
                    existing.Open = bar.Open;
                    existing.High = bar.High;
                    existing.Low = bar.Low;
                    existing.Close = bar.Close;
                    existing.Volume = bar.Volume;
                    existing.FetchedAt = now;
                    touched.Add(existing);
                }
                else
                {
                    var point = new PriceHistoryPoint
                    {
                        CompanyId = series.Id,
                        Date = bar.Date.Date,
                        Open = bar.Open,
                        High = bar.High,
                        Low = bar.Low,
                        Close = bar.Close,
                        Volume = bar.Volume,
                        FetchedAt = now,
                    };
                    _context.PriceHistoryPoints.Add(point);
                    touched.Add(point);
                }
            }

            return touched;
        }

        private void Detach(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
        }

        // ---------------------------------------------------------------- Barras de una hora (solo memoria)

        public Task<PriceHistoryResult> GetHourlyAsync(int companyId, string ticker, CancellationToken cancellationToken)
        {
            return GetSeriesHourlyAsync(ChartSeriesRef.ForCompany(companyId, ticker), cancellationToken);
        }

        public async Task<PriceHistoryResult> GetSeriesHourlyAsync(ChartSeriesRef series, CancellationToken cancellationToken)
        {
            lock (_hourlyLock)
            {
                if (TryGetHourlyEntry(series, out var cached) && DateTime.Now - cached.CreatedAt < HourlyCacheLifetime)
                {
                    return BuildHourlyResult(cached);
                }
            }

            var to = DateTime.UtcNow;
            var from = to.AddDays(-HourlyInitialDays);
            var chunk = await _stockPriceService.GetHistoryRangeAsync(series.Symbol, "1h", from, to.AddHours(1), cancellationToken);
            if (chunk.Failed)
            {
                return new PriceHistoryResult { RefreshFailed = true };
            }

            if (chunk.Bars.Count == 0)
            {
                // Yahoo no da barras de una hora para este ticker: no se guarda nada en memoria
                return new PriceHistoryResult { IsComplete = true };
            }

            var entry = new HourlyCacheEntry
            {
                Symbol = series.Symbol,
                Points = ToHourlyPoints(series, chunk.Bars),
                CreatedAt = DateTime.Now,
                Cursor = from
            };

            lock (_hourlyLock)
            {
                _hourlyCache[series.Key] = entry;
                return BuildHourlyResult(entry);
            }
        }

        public Task<HistoryBlockResult> FetchOlderHourlyBlockAsync(int companyId, string ticker, CancellationToken cancellationToken)
        {
            return FetchOlderSeriesHourlyBlockAsync(ChartSeriesRef.ForCompany(companyId, ticker), cancellationToken);
        }

        public async Task<HistoryBlockResult> FetchOlderSeriesHourlyBlockAsync(ChartSeriesRef series, CancellationToken cancellationToken)
        {
            HourlyCacheEntry? entry;
            DateTime to;
            lock (_hourlyLock)
            {
                if (!TryGetHourlyEntry(series, out entry) || entry.IsComplete)
                {
                    return new HistoryBlockResult { ReachedStart = true };
                }

                to = entry.Cursor;
            }

            var limit = DateTime.UtcNow.AddDays(-HourlyMaxDays);
            var from = to.AddMonths(-HourlyBlockMonths);
            var reachedLimit = false;
            if (from <= limit)
            {
                from = limit;
                reachedLimit = true;
            }

            if (from >= to)
            {
                lock (_hourlyLock)
                {
                    entry.IsComplete = true;
                }

                return new HistoryBlockResult { ReachedStart = true, BlockStart = to };
            }

            var chunk = await _stockPriceService.GetHistoryRangeAsync(entry.Symbol, "1h", from, to, cancellationToken);
            if (chunk.Failed)
            {
                return new HistoryBlockResult { Failed = true, BlockStart = from };
            }

            lock (_hourlyLock)
            {
                var oldest = entry.Points.Count > 0 ? entry.Points[0].Date : DateTime.MaxValue;
                var older = ToHourlyPoints(series, chunk.Bars.Where(b => b.Date < oldest));
                entry.Points.InsertRange(0, older);
                entry.Cursor = from;
                entry.IsComplete = reachedLimit || (chunk.FirstTradeDateUtc.HasValue && from <= chunk.FirstTradeDateUtc.Value);

                return new HistoryBlockResult
                {
                    Bars = chunk.Bars,
                    BlockStart = from,
                    OldestDate = entry.Points.Count > 0 ? entry.Points[0].Date : null,
                    ReachedStart = entry.IsComplete
                };
            }
        }

        public PriceHistoryResult GetCachedHourly(int companyId)
        {
            return GetCachedSeriesHourly(ChartSeriesRef.ForCompany(companyId, string.Empty));
        }

        public PriceHistoryResult GetCachedSeriesHourly(ChartSeriesRef series)
        {
            lock (_hourlyLock)
            {
                return TryGetHourlyEntry(series, out var entry)
                    ? BuildHourlyResult(entry)
                    : new PriceHistoryResult();
            }
        }

        public void ClearHourly(int companyId)
        {
            lock (_hourlyLock)
            {
                _hourlyCache.Remove(ChartSeriesRef.ForCompany(companyId, string.Empty).Key);
            }
        }

        /// <summary>
        /// Barras de una hora en memoria de la serie. Con símbolo conocido, una entrada de otro símbolo (índice
        /// editado) no vale. Se llama siempre dentro de _hourlyLock.
        /// </summary>
        private bool TryGetHourlyEntry(ChartSeriesRef series, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out HourlyCacheEntry? entry)
        {
            if (_hourlyCache.TryGetValue(series.Key, out entry)
                && (series.Kind == ChartSeriesKind.Company || string.Equals(entry.Symbol, series.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public List<ChartMarker> AlignMarkers(IEnumerable<ChartMarker> markers, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity)
        {
            var align = BuildAligner(points, granularity);
            var aligned = new List<ChartMarker>();
            foreach (var marker in markers)
            {
                var date = align(marker.Date);
                if (date.HasValue)
                {
                    aligned.Add(new ChartMarker
                    {
                        Kind = marker.Kind,
                        Date = date.Value,
                        OperationDate = marker.OperationDate ?? marker.Date,
                        Price = marker.Price,
                        Shares = marker.Shares,
                        Amount = marker.Amount
                    });
                }
            }

            return aligned;
        }

        public List<ChartMarkPoint> AlignMarks(IEnumerable<ChartMark> marks, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity)
        {
            var align = BuildAligner(points, granularity);
            var aligned = new List<ChartMarkPoint>();
            foreach (var mark in marks)
            {
                var date = align(mark.Date);
                if (date.HasValue)
                {
                    aligned.Add(new ChartMarkPoint
                    {
                        Id = mark.Id,
                        Date = date.Value,
                        MarkDate = mark.Date,
                        Description = mark.Description,
                        IsGlobal = mark.IsGlobal
                    });
                }
            }

            return aligned;
        }

        public List<DateTime> BuildFutureSessions(IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity, IEnumerable<DateTime> pendingPaymentDates)
        {
            var sessions = new List<DateTime>();
            if (points.Count == 0 || granularity == ChartGranularity.Hourly)
            {
                return sessions;
            }

            // Hasta lo más lejano de: un año desde hoy o el pago pendiente más lejano (SPEC-21-01, D12)
            var until = DateTime.Today.AddYears(1);
            foreach (var date in pendingPaymentDates)
            {
                if (date.Date > until)
                {
                    until = date.Date;
                }
            }

            var last = points[^1].Date.Date;
            if (granularity == ChartGranularity.Daily)
            {
                for (var day = last.AddDays(1); day <= until; day = day.AddDays(1))
                {
                    if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                    {
                        sessions.Add(day);
                    }
                }

                return sessions;
            }

            var lastPeriod = PeriodStart(last, granularity);
            var untilPeriod = PeriodStart(until, granularity);
            for (var period = NextPeriod(lastPeriod, granularity); period <= untilPeriod; period = NextPeriod(period, granularity))
            {
                sessions.Add(period);
            }

            return sessions;
        }

        public List<DividendMarkPoint> AlignDividendMarks(IEnumerable<DividendChartMark> marks, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity)
        {
            var aligned = new List<DividendMarkPoint>();
            if (points.Count == 0)
            {
                return aligned;
            }

            var align = BuildAligner(points, granularity);
            var last = points[^1].Date;
            foreach (var mark in marks)
            {
                var payment = mark.PaymentDate.Date;
                var isFuture = granularity == ChartGranularity.Hourly
                    ? payment > last.Date
                    : (granularity == ChartGranularity.Daily ? payment : PeriodStart(payment, granularity)) > last.Date;

                DateTime? date;
                if (!isFuture)
                {
                    date = align(payment);
                }
                else
                {
                    // En Horas no hay tramo futuro; en el resto, la sesión futura de su periodo
                    date = granularity == ChartGranularity.Hourly
                        ? null
                        : granularity == ChartGranularity.Daily ? payment : PeriodStart(payment, granularity);
                }

                if (!date.HasValue)
                {
                    continue;
                }

                aligned.Add(new DividendMarkPoint
                {
                    Id = mark.Id,
                    Date = date.Value,
                    PaymentDate = payment,
                    ExDividendDate = mark.ExDividendDate,
                    Status = mark.Status,
                    TotalAmount = mark.TotalAmount,
                    CurrencyCode = mark.CurrencyCode,
                    IsFuture = isFuture
                });
            }

            return aligned;
        }

        private static DateTime NextPeriod(DateTime period, ChartGranularity granularity)
        {
            return granularity switch
            {
                ChartGranularity.Weekly => period.AddDays(7),
                ChartGranularity.Monthly => period.AddMonths(1),
                ChartGranularity.Yearly => period.AddYears(1),
                _ => period.AddDays(1),
            };
        }

        /// <summary>
        /// Fecha del periodo de la temporalidad en el que cae una fecha (SPEC-05-03, plan §3.2): en Diario la propia
        /// fecha (el gráfico usa esa sesión o la anterior), en Semanal/Mensual/Anual el inicio del periodo y en Horas
        /// la primera barra de ese día. null si no se puede dibujar (anterior al primer dato o día sin barras horarias).
        /// </summary>
        private static Func<DateTime, DateTime?> BuildAligner(IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity)
        {
            if (points.Count == 0)
            {
                return _ => null;
            }

            var first = points[0].Date;

            if (granularity == ChartGranularity.Hourly)
            {
                // Primera barra de cada día (las barras vienen ordenadas por hora)
                var firstBarByDay = new Dictionary<DateTime, DateTime>();
                foreach (var point in points)
                {
                    firstBarByDay.TryAdd(point.Date.Date, point.Date);
                }

                return date => firstBarByDay.TryGetValue(date.Date, out var bar) ? bar : null;
            }

            return date =>
            {
                var period = granularity == ChartGranularity.Daily ? date.Date : PeriodStart(date, granularity);
                return period < first.Date ? null : period;
            };
        }

        /// <summary>
        /// Inicio del periodo que contiene la fecha: lunes de la semana, día 1 del mes o 1 de enero
        /// </summary>
        private static DateTime PeriodStart(DateTime date, ChartGranularity granularity)
        {
            return granularity switch
            {
                ChartGranularity.Weekly => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
                ChartGranularity.Monthly => new DateTime(date.Year, date.Month, 1),
                ChartGranularity.Yearly => new DateTime(date.Year, 1, 1),
                _ => date.Date,
            };
        }

        // Se llama siempre dentro de _hourlyLock: devuelve una copia para que el modal no vea cambios a medias
        private static PriceHistoryResult BuildHourlyResult(HourlyCacheEntry entry)
        {
            var points = entry.Points.ToList();
            var (viewStart, viewEnd) = GetInitialView(ChartGranularity.Hourly, points);
            return new PriceHistoryResult
            {
                Points = points,
                ViewStart = viewStart,
                ViewEnd = viewEnd,
                IsComplete = entry.IsComplete,
                OldestDate = points.Count > 0 ? points[0].Date : null
            };
        }

        private static List<PriceHistoryPoint> ToHourlyPoints(ChartSeriesRef series, IEnumerable<StockHistoryBar> bars)
        {
            var now = DateTime.Now;
            return bars
                .OrderBy(b => b.Date)
                .Select(b => new PriceHistoryPoint
                {
                    // Los índices no tienen empresa (SPEC-05-04)
                    CompanyId = series.Kind == ChartSeriesKind.Company ? series.Id : 0,
                    Date = b.Date,
                    Open = b.Open,
                    High = b.High,
                    Low = b.Low,
                    Close = b.Close,
                    Volume = b.Volume,
                    FetchedAt = now,
                    TradedValue = b.Volume * b.Close,
                })
                .ToList();
        }

        private static List<PriceHistoryPoint> Aggregate(List<PriceHistoryPoint> dailyPoints, Func<PriceHistoryPoint, DateTime> bucketSelector)
        {
            return dailyPoints
                .GroupBy(bucketSelector)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var ordered = g.OrderBy(p => p.Date).ToList();
                    return new PriceHistoryPoint
                    {
                        CompanyId = ordered[0].CompanyId,
                        Date = g.Key,
                        Open = ordered.First().Open,
                        Close = ordered.Last().Close,
                        High = ordered.Max(p => p.High),
                        Low = ordered.Min(p => p.Low),
                        Volume = ordered.Sum(p => p.Volume),
                        FetchedAt = ordered.Max(p => p.FetchedAt),
                        // Suma día a día de acciones × cierre (no volumen total × cierre final)
                        TradedValue = ordered.Sum(p => p.TradedValue),
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Barras de una hora de una serie (empresa o índice) en memoria (no se guardan en la base de datos)
        /// </summary>
        private sealed class HourlyCacheEntry
        {
            /// <summary>Símbolo con el que se descargaron (los bloques antiguos se piden con el mismo)</summary>
            public string Symbol { get; set; } = string.Empty;

            public List<PriceHistoryPoint> Points { get; set; } = new();
            public DateTime CreatedAt { get; set; }

            /// <summary>Inicio (UTC) del tramo más antiguo ya pedido: el siguiente bloque se pide antes</summary>
            public DateTime Cursor { get; set; }

            public bool IsComplete { get; set; }
        }
    }
}
