using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IWealthSnapshotService"/> (SPEC-17-01).
    /// Los importes salen de <see cref="IDashboardService.GetDashboardSummaryAsync"/> para que la
    /// captura cuadre con lo que enseña el Dashboard. Las variaciones se calculan al leer.
    /// </summary>
    public class WealthSnapshotService : IWealthSnapshotService
    {
        /// <summary>Longitud máxima de la nota</summary>
        public const int MaxNoteLength = 200;

        /// <summary>Horas a partir de las cuales un precio en vivo se considera antiguo</summary>
        private const int StalePriceHours = 24;

        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

        private readonly AppDbContext _context;
        private readonly IDashboardService _dashboardService;
        private readonly ICompanyService _companyService;
        private readonly ILogger<WealthSnapshotService> _logger;

        public WealthSnapshotService(
            AppDbContext context,
            IDashboardService dashboardService,
            ICompanyService companyService,
            ILogger<WealthSnapshotService> logger)
        {
            _context = context;
            _dashboardService = dashboardService;
            _companyService = companyService;
            _logger = logger;
        }

        public IReadOnlyList<SnapshotPeriod> GetCapturePeriods(DateTime today)
        {
            var previous = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            return new List<SnapshotPeriod>
            {
                new SnapshotPeriod(today.Year, today.Month),
                new SnapshotPeriod(previous.Year, previous.Month)
            };
        }

        public async Task<WealthSnapshotPreview> PreviewAsync(int year, int month)
        {
            var snapshot = await BuildSnapshotAsync(year, month, null);

            var existing = await _context.WealthSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Year == year && s.Month == month);

            // Aviso de precios antiguos: solo empresas con acciones en cartera
            var now = DateTime.Now;
            var companies = await _companyService.GetAllAsync();
            var stale = companies.Count(c => c.NumberOfShares > 0
                && (c.LivePriceUpdatedAt == null || (now - c.LivePriceUpdatedAt.Value).TotalHours > StalePriceHours));

            return new WealthSnapshotPreview
            {
                Snapshot = snapshot,
                ExistingCapturedAt = existing?.CapturedAt,
                ExistingNote = existing?.Note,
                StalePriceCompanies = stale,
                MissingRateCompanies = snapshot.ExcludedCompanies
            };
        }

        public async Task<WealthSnapshot> CaptureAsync(int year, int month, string? note)
        {
            // El periodo se valida con la fecha de hoy (por si el modal se quedó abierto al cambiar de mes)
            var allowed = GetCapturePeriods(DateTime.Now);
            if (!allowed.Any(p => p.Year == year && p.Month == month))
            {
                throw new InvalidOperationException(
                    $"Solo se puede capturar {allowed[0].Label} o {allowed[1].Label}. Vuelve a abrir la ventana de captura.");
            }

            var cleanNote = NormalizeNote(note);

            // Se recalculan los importes: no se usa la vista previa
            var fresh = await BuildSnapshotAsync(year, month, cleanNote);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.WealthSnapshots
                    .FirstOrDefaultAsync(s => s.Year == year && s.Month == month);

                WealthSnapshot saved;
                if (existing != null)
                {
                    CopyAmounts(fresh, existing);
                    existing.CapturedAt = fresh.CapturedAt;
                    existing.Note = cleanNote;
                    existing.UpdatedAt = DateTime.Now;
                    saved = existing;
                }
                else
                {
                    _context.WealthSnapshots.Add(fresh);
                    saved = fresh;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Captura del patrimonio guardada para {Period}", saved.PeriodLabel);
                return saved;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<WealthSnapshotRow>> GetRowsAsync(WealthView view, int? year)
        {
            var all = await LoadAscendingAsync();
            var rows = view == WealthView.Annual ? BuildAnnualRows(all) : BuildMonthlyRows(all);

            // El filtro se aplica después de calcular, para que enero se compare con diciembre del año anterior
            if (year.HasValue)
            {
                rows = rows.Where(r => r.Snapshot.Year == year.Value).ToList();
            }

            rows.Reverse();
            return rows;
        }

        public async Task<WealthChartData> GetChartDataAsync(WealthView view, int? year)
        {
            var currency = (await _dashboardService.GetCurrencyInfoAsync()).CurrencyCode;
            var rows = await GetRowsAsync(view, year);
            rows.Reverse(); // el gráfico va de antiguo a reciente

            var visible = rows.Where(r => SameCurrency(r.Snapshot.CurrencyCode, currency)).ToList();

            return new WealthChartData
            {
                CurrencyCode = currency,
                HiddenOtherCurrency = rows.Count - visible.Count,
                Points = visible.Select(r => new WealthChartPoint
                {
                    Label = view == WealthView.Annual
                        ? $"{r.Snapshot.Year}"
                        : r.Snapshot.PeriodLabel,
                    Stocks = r.Snapshot.StocksValue,
                    Funds = r.Snapshot.FundsValue,
                    Bank = r.Snapshot.BankBalance,
                    SafeHaven = r.Snapshot.SafeHavenValue,
                    RealEstate = r.Snapshot.RealEstateValue,
                    Total = r.Snapshot.TotalPatrimony,
                    Invested = r.Snapshot.TotalInvested,
                    Note = r.Snapshot.Note
                }).ToList()
            };
        }

        public async Task<List<int>> GetYearsAsync()
        {
            return await _context.WealthSnapshots
                .AsNoTracking()
                .Select(s => s.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
        }

        public async Task<WealthSnapshot?> GetLatestAsync()
        {
            return await _context.WealthSnapshots
                .AsNoTracking()
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .FirstOrDefaultAsync();
        }

        public async Task<WealthVariation?> GetDashboardVariationAsync(DateTime today, decimal currentTotal, string currencyCode)
        {
            var currentKey = today.Year * 100 + today.Month;
            var previous = (await LoadAscendingAsync())
                .Where(s => s.PeriodKey < currentKey)
                .LastOrDefault();

            if (previous == null || !SameCurrency(previous.CurrencyCode, currencyCode))
            {
                return null;
            }

            return Variation(currentTotal, previous);
        }

        public async Task UpdateNoteAsync(int id, string? note)
        {
            var cleanNote = NormalizeNote(note);
            var snapshot = await _context.WealthSnapshots.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("La captura ya no existe.");

            snapshot.Note = cleanNote;
            snapshot.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var snapshot = await _context.WealthSnapshots.FirstOrDefaultAsync(s => s.Id == id);
            if (snapshot == null)
            {
                return;
            }

            _context.WealthSnapshots.Remove(snapshot);
            await _context.SaveChangesAsync();
        }

        public string BuildCsv(IEnumerable<WealthSnapshotRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("Periodo;Fecha de captura;Divisa;Total;Total invertido;Plusvalía latente;");
            sb.Append("Acciones;Acciones invertido;Fondos;Fondos invertido;Banco;");
            sb.Append("Refugio;Refugio invertido;Inmuebles;Inmuebles invertido;");
            sb.Append("Variación;Variación %;Nota\r\n");

            foreach (var row in rows)
            {
                var s = row.Snapshot;
                var fields = new[]
                {
                    $"{s.Year:0000}-{s.Month:00}",
                    s.CapturedAt.ToString("dd/MM/yyyy HH:mm", Es),
                    s.CurrencyCode,
                    Num(s.TotalPatrimony),
                    Num(s.TotalInvested),
                    Num(s.LatentGain),
                    Num(s.StocksValue),
                    Num(s.StocksInvested),
                    Num(s.FundsValue),
                    Num(s.FundsInvested),
                    Num(s.BankBalance),
                    Num(s.SafeHavenValue),
                    Num(s.SafeHavenInvested),
                    Num(s.RealEstateValue),
                    Num(s.RealEstateInvested),
                    row.VsPrevious == null ? string.Empty : Num(row.VsPrevious.Amount),
                    row.VsPrevious?.Percent == null ? string.Empty : Num(row.VsPrevious.Percent.Value),
                    Quote(s.Note)
                };
                sb.Append(string.Join(";", fields)).Append("\r\n");
            }

            return sb.ToString();
        }

        public async Task ExportCsvAsync(WealthView view, int? year)
        {
            var rows = await GetRowsAsync(view, year);
            if (rows.Count == 0)
            {
                throw new InvalidOperationException("No hay capturas que exportar.");
            }

            var fileName = $"patrimonio_{DateTime.Now:yyyyMMdd}.csv";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);

            // UTF-8 con BOM para que Excel lea bien las tildes
            await File.WriteAllTextAsync(path, BuildCsv(rows), new UTF8Encoding(true));

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar histórico del patrimonio",
                File = new ShareFile(path)
            });
        }

        // ---------------------------------------------------------------- privados

        /// <summary>Calcula una captura con los importes actuales del Dashboard (algoritmo A)</summary>
        private async Task<WealthSnapshot> BuildSnapshotAsync(int year, int month, string? note)
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();
            var currency = await _dashboardService.GetCurrencyInfoAsync();
            var now = DateTime.Now;

            return new WealthSnapshot
            {
                Year = year,
                Month = month,
                CapturedAt = now,
                CurrencyCode = CurrencyFormatter.Normalize(currency.CurrencyCode),
                TotalPatrimony = Math.Round(summary.TotalPatrimony, 2),
                StocksValue = Math.Round(summary.AccionesTotal, 2),
                StocksInvested = Math.Round(summary.AccionesInitialTotal, 2),
                FundsValue = Math.Round(summary.FundsTotal, 2),
                FundsInvested = Math.Round(summary.FundsInitialTotal, 2),
                BankBalance = Math.Round(summary.BankTotal, 2),
                SafeHavenValue = Math.Round(summary.SafeHavenTotal, 2),
                SafeHavenInvested = Math.Round(summary.SafeHavenInitialTotal, 2),
                RealEstateValue = Math.Round(summary.RealEstateTotal, 2),
                RealEstateInvested = Math.Round(summary.RealEstateInvestedTotal, 2),
                // El banco no tiene invertido (decisión QA-1)
                TotalInvested = Math.Round(summary.AccionesInitialTotal + summary.FundsInitialTotal
                    + summary.SafeHavenInitialTotal + summary.RealEstateInvestedTotal, 2),
                ExcludedCompanies = currency.ExcludedCompanies,
                Note = note,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private static void CopyAmounts(WealthSnapshot from, WealthSnapshot to)
        {
            to.CurrencyCode = from.CurrencyCode;
            to.TotalPatrimony = from.TotalPatrimony;
            to.TotalInvested = from.TotalInvested;
            to.StocksValue = from.StocksValue;
            to.StocksInvested = from.StocksInvested;
            to.FundsValue = from.FundsValue;
            to.FundsInvested = from.FundsInvested;
            to.BankBalance = from.BankBalance;
            to.SafeHavenValue = from.SafeHavenValue;
            to.SafeHavenInvested = from.SafeHavenInvested;
            to.RealEstateValue = from.RealEstateValue;
            to.RealEstateInvested = from.RealEstateInvested;
            to.ExcludedCompanies = from.ExcludedCompanies;
        }

        private static string? NormalizeNote(string? note)
        {
            var clean = note?.Trim();
            if (string.IsNullOrEmpty(clean))
            {
                return null;
            }

            if (clean.Length > MaxNoteLength)
            {
                throw new InvalidOperationException($"La nota no puede exceder {MaxNoteLength} caracteres.");
            }

            return clean;
        }

        private async Task<List<WealthSnapshot>> LoadAscendingAsync()
        {
            return await _context.WealthSnapshots
                .AsNoTracking()
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToListAsync();
        }

        /// <summary>Vista mensual (algoritmo D): anterior existente y mismo mes del año anterior</summary>
        private static List<WealthSnapshotRow> BuildMonthlyRows(List<WealthSnapshot> ascending)
        {
            var byKey = ascending.ToDictionary(s => s.PeriodKey);
            var rows = new List<WealthSnapshotRow>();

            for (var i = 0; i < ascending.Count; i++)
            {
                var current = ascending[i];
                var row = new WealthSnapshotRow { Snapshot = current };

                if (i > 0)
                {
                    var previous = ascending[i - 1];
                    var expected = new DateTime(current.Year, current.Month, 1).AddMonths(-1);
                    row.PreviousIsGap = previous.Year != expected.Year || previous.Month != expected.Month;

                    if (SameCurrency(previous.CurrencyCode, current.CurrencyCode))
                    {
                        row.VsPrevious = Variation(current.TotalPatrimony, previous);
                    }
                    else
                    {
                        row.PreviousCurrencyMismatch = true;
                        row.PreviousCurrencyCode = previous.CurrencyCode;
                    }
                }

                if (byKey.TryGetValue((current.Year - 1) * 100 + current.Month, out var lastYear))
                {
                    if (SameCurrency(lastYear.CurrencyCode, current.CurrencyCode))
                    {
                        row.VsLastYear = Variation(current.TotalPatrimony, lastYear);
                    }
                    else
                    {
                        row.LastYearCurrencyMismatch = true;
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>Vista anual (algoritmo E): la captura del mes más alto de cada año</summary>
        private static List<WealthSnapshotRow> BuildAnnualRows(List<WealthSnapshot> ascending)
        {
            var currentYear = DateTime.Now.Year;
            var lastOfYear = ascending
                .GroupBy(s => s.Year)
                .Select(g => g.OrderBy(s => s.Month).Last())
                .OrderBy(s => s.Year)
                .ToList();

            var rows = new List<WealthSnapshotRow>();
            for (var i = 0; i < lastOfYear.Count; i++)
            {
                var current = lastOfYear[i];
                var row = new WealthSnapshotRow
                {
                    Snapshot = current,
                    IsCurrentYear = current.Year == currentYear
                };

                if (i > 0)
                {
                    var previous = lastOfYear[i - 1];
                    row.PreviousIsGap = previous.Year != current.Year - 1;

                    if (SameCurrency(previous.CurrencyCode, current.CurrencyCode))
                    {
                        row.VsPrevious = Variation(current.TotalPatrimony, previous);
                        row.VsPrevious.ComparedToLabel = previous.Year.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        row.PreviousCurrencyMismatch = true;
                        row.PreviousCurrencyCode = previous.CurrencyCode;
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        private static WealthVariation Variation(decimal currentTotal, WealthSnapshot compared)
        {
            var amount = currentTotal - compared.TotalPatrimony;
            return new WealthVariation
            {
                Amount = amount,
                Percent = compared.TotalPatrimony == 0m
                    ? null
                    : Math.Round(amount / compared.TotalPatrimony * 100m, 2),
                ComparedToLabel = compared.PeriodLabel
            };
        }

        private static bool SameCurrency(string? a, string? b) =>
            CurrencyFormatter.Normalize(a) == CurrencyFormatter.Normalize(b);

        /// <summary>Número para Excel en español: coma decimal y sin separador de miles</summary>
        private static string Num(decimal value) => value.ToString("0.00", Es);

        /// <summary>Texto entre comillas, comillas internas duplicadas y sin saltos de línea</summary>
        private static string Quote(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var clean = text.Replace("\r", " ").Replace("\n", " ").Replace("\"", "\"\"");
            return $"\"{clean}\"";
        }
    }
}
