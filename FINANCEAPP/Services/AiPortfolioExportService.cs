using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IAiPortfolioExportService"/> (SPEC-17-03). Lee las capturas diarias ya guardadas y
    /// arma el JSON: el modelo no tiene que hacer ninguna cuenta. Claves en español con unidades en el nombre
    /// (_eur, _pct), fechas ISO y números con 2 decimales.
    /// </summary>
    public class AiPortfolioExportService : IAiPortfolioExportService
    {
        private const int SchemaVersion = 1;
        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

        private readonly AppDbContext _context;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ICompanyService _companyService;

        public AiPortfolioExportService(AppDbContext context, IAppSettingsService appSettingsService, ICompanyService companyService)
        {
            _context = context;
            _appSettingsService = appSettingsService;
            _companyService = companyService;
        }

        public async Task<string> WriteAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _appSettingsService.GetAiExportSettingsAsync();
            var document = await BuildAsync(settings, cancellationToken);

            Directory.CreateDirectory(settings.EffectiveFolder);
            var path = settings.FilePath;
            var temp = path + ".tmp";
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(document, options), cancellationToken);
            File.Move(temp, path, overwrite: true);
            return path;
        }

        // ---------------------------------------------------------------- Documento

        private async Task<Dictionary<string, object?>> BuildAsync(AiExportSettings settings, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var series = await _context.DailyPortfolioSnapshots
                .AsNoTracking()
                .Where(s => s.Date >= today.AddDays(-Math.Max(settings.CompactDays, 400)))
                .OrderBy(s => s.Date)
                .ToListAsync(cancellationToken);
            var doc = new Dictionary<string, object?>
            {
                ["version"] = SchemaVersion,
                ["fecha"] = Iso(today),
                ["generado"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            };

            var last = series.LastOrDefault();
            if (last == null)
            {
                doc["divisa_base"] = CurrencyFormatter.DefaultCurrency;
                doc["resumen_texto"] = "No hay datos: todavía no hay operaciones registradas.";
                doc["alertas"] = new List<object>();
                return doc;
            }

            var currency = last.CurrencyCode;
            var companies = await _context.DailyCompanySnapshots
                .AsNoTracking()
                .Where(c => c.Date >= last.Date.AddDays(-40))
                .ToListAsync(cancellationToken);
            var todayCompanies = companies.Where(c => c.Date == last.Date).OrderByDescending(c => c.ValueBase).ToList();
            var catalog = await _companyService.GetAllAsync();

            // Variaciones (todas calculadas aquí: el modelo no hace cuentas)
            var ytdStart = new DateTime(last.Date.Year, 1, 1).AddDays(-1);
            var variations = new Dictionary<string, object?>
            {
                ["patrimonio_var_1d_pct"] = Change(series, last, last.Date.AddDays(-1), s => s.TotalValue),
                ["patrimonio_var_7d_pct"] = Change(series, last, last.Date.AddDays(-7), s => s.TotalValue),
                ["patrimonio_var_1m_pct"] = Change(series, last, last.Date.AddMonths(-1), s => s.TotalValue),
                ["patrimonio_var_ytd_pct"] = Change(series, last, ytdStart, s => s.TotalValue),
                ["patrimonio_var_12m_pct"] = Change(series, last, last.Date.AddYears(-1), s => s.TotalValue),
                ["cartera_twr_7d_pct"] = Twr(series, last.Date.AddDays(-7), last.Date),
                ["cartera_twr_1m_pct"] = Twr(series, last.Date.AddMonths(-1), last.Date),
                ["cartera_twr_ytd_pct"] = Twr(series, ytdStart, last.Date),
                ["cartera_twr_12m_pct"] = Twr(series, last.Date.AddYears(-1), last.Date)
            };
            var yearRows = series.Where(s => s.Date > ytdStart).ToList();
            if (yearRows.Count > 0)
            {
                var max = yearRows.MaxBy(s => s.TotalValue)!;
                variations["patrimonio_max_ano_eur"] = R(max.TotalValue);
                variations["patrimonio_max_ano_fecha"] = Iso(max.Date);
                variations["patrimonio_caida_desde_max_pct"] = max.TotalValue > 0 ? R((last.TotalValue / max.TotalValue - 1m) * 100m) : 0m;
            }
            variations["nota"] = "twr = rentabilidad de la cartera de acciones sin el efecto de las compras y ventas (ponderada por tiempo)";

            var alerts = BuildAlerts(settings, last, todayCompanies, companies, catalog, series);
            var indices = await BuildIndicesAsync(last.Date, ytdStart, variations["cartera_twr_ytd_pct"] as decimal?, cancellationToken);

            doc["divisa_base"] = currency;
            doc["resumen_texto"] = Summary(last, variations, alerts.Count, indices.FirstOrDefault());
            doc["alertas"] = alerts;
            doc["registros_hoy"] = TodayRecords(last);
            doc["variaciones"] = variations;
            doc["reparto"] = Distribution(last, todayCompanies);
            doc["empresas"] = todayCompanies.Select(c => CompanyEntry(c, companies, last.Date)).ToList();
            doc["indices"] = indices;
            doc["proximos_dividendos"] = await UpcomingDividendsAsync(catalog, cancellationToken);
            doc["serie_diaria"] = Series(series, settings, last.Date);
            doc["calidad"] = Quality(last, series, catalog, settings);
            return doc;
        }

        // ---------------------------------------------------------------- Secciones

        private static Dictionary<string, object?> TodayRecords(DailyPortfolioSnapshot s) => new()
        {
            ["patrimonio_total"] = new Dictionary<string, object?>
            {
                ["valor_eur"] = R(s.TotalValue),
                ["invertido_eur"] = R(s.TotalInvested),
                ["resultado_eur"] = R(s.TotalValue - s.BankBalance - s.TotalInvested),
                ["rentabilidad_pct"] = Pct(s.TotalValue - s.BankBalance - s.TotalInvested, s.TotalInvested),
                ["acciones_eur"] = R(s.StocksValue),
                ["fondos_eur"] = R(s.FundsValue),
                ["refugio_eur"] = R(s.SafeHavenValue),
                ["inmuebles_eur"] = R(s.RealEstateValue),
                ["banco_eur"] = R(s.BankBalance),
                ["liquidez_pct"] = R(s.LiquidityPct),
                ["nota"] = "el banco no cuenta como invertido; resultado y rentabilidad sin el banco"
            },
            ["cartera_actual"] = new Dictionary<string, object?>
            {
                ["invertido_eur"] = R(s.OpenInvested),
                ["valor_eur"] = R(s.OpenValue),
                ["resultado_eur"] = R(s.OpenValue - s.OpenInvested),
                ["rentabilidad_pct"] = R(s.OpenReturnPct),
                ["dividendos_eur"] = R(s.OpenDividends),
                ["rentabilidad_con_dividendos_pct"] = R(s.OpenReturnWithDivPct)
            },
            ["compras_del_dia"] = new Dictionary<string, object?>
            {
                ["comprado_eur"] = R(s.DayBought),
                ["operaciones"] = s.DayBuyCount
            },
            ["empresas_con_vendidas"] = new Dictionary<string, object?>
            {
                ["invertido_eur"] = R(s.AllInvested),
                ["valor_eur"] = R(s.AllValue),
                ["resultado_eur"] = R(s.AllValue - s.AllInvested),
                ["rentabilidad_pct"] = R(s.AllReturnPct),
                ["dividendos_eur"] = R(s.AllDividends),
                ["rentabilidad_con_dividendos_pct"] = R(s.AllReturnWithDivPct)
            },
            ["solo_vendidas"] = new Dictionary<string, object?>
            {
                ["coste_eur"] = R(s.SoldCost),
                ["cobrado_eur"] = R(s.SoldProceeds),
                ["resultado_eur"] = R(s.SoldResult),
                ["rentabilidad_pct"] = R(s.SoldReturnPct)
            },
            ["flujos_del_dia"] = new Dictionary<string, object?>
            {
                ["comprado_eur"] = R(s.DayBought),
                ["vendido_neto_eur"] = R(s.DaySoldNet),
                ["aportacion_neta_eur"] = R(s.DayNetContribution),
                ["dividendos_eur"] = R(s.DayDividends),
                ["comisiones_eur"] = R(s.DayCommissions)
            }
        };

        private static Dictionary<string, object?> Distribution(DailyPortfolioSnapshot s, List<DailyCompanySnapshot> companies)
        {
            var stocks = companies.Sum(c => c.ValueBase);
            Dictionary<string, decimal> Group(Func<DailyCompanySnapshot, string> key) => companies
                .GroupBy(c => string.IsNullOrWhiteSpace(key(c)) ? "Sin dato" : key(c))
                .OrderByDescending(g => g.Sum(c => c.ValueBase))
                .ToDictionary(g => g.Key, g => Pct(g.Sum(c => c.ValueBase), stocks));

            return new Dictionary<string, object?>
            {
                ["por_sector_pct"] = Group(c => c.Sector),
                ["por_divisa_pct"] = Group(c => c.CurrencyCode),
                ["por_activo_pct"] = new Dictionary<string, decimal>
                {
                    ["acciones"] = Pct(s.StocksValue, s.TotalValue),
                    ["fondos"] = Pct(s.FundsValue, s.TotalValue),
                    ["refugio"] = Pct(s.SafeHavenValue, s.TotalValue),
                    ["inmuebles"] = Pct(s.RealEstateValue, s.TotalValue),
                    ["banco"] = Pct(s.BankBalance, s.TotalValue)
                },
                ["nota"] = "sector y divisa sobre la cartera de acciones; activo sobre el patrimonio total"
            };
        }

        private static Dictionary<string, object?> CompanyEntry(DailyCompanySnapshot c, List<DailyCompanySnapshot> history, DateTime day)
        {
            decimal? PriceChange(DateTime target)
            {
                var past = history.Where(h => h.CompanyId == c.CompanyId && h.Date <= target).MaxBy(h => h.Date);
                return past != null && past.Price > 0 ? R((c.Price / past.Price - 1m) * 100m) : null;
            }

            return new Dictionary<string, object?>
            {
                ["nombre"] = c.CompanyName,
                ["ticker"] = c.Ticker,
                ["sector"] = c.Sector,
                ["divisa"] = c.CurrencyCode,
                ["acciones"] = R(c.Shares),
                ["precio"] = Math.Round(c.Price, 2, MidpointRounding.AwayFromZero),
                ["valor_eur"] = R(c.ValueBase),
                ["coste_eur"] = R(c.CostBase),
                ["rentabilidad_pct"] = R(c.ReturnPct),
                ["peso_pct"] = R(c.WeightPct),
                ["dividendos_eur"] = R(c.DividendsBase),
                ["precio_var_7d_pct"] = PriceChange(day.AddDays(-7)),
                ["precio_var_1m_pct"] = PriceChange(day.AddMonths(-1))
            };
        }

        private List<Dictionary<string, object?>> BuildAlerts(AiExportSettings settings, DailyPortfolioSnapshot last,
            List<DailyCompanySnapshot> today, List<DailyCompanySnapshot> history, List<CompanyDetail> catalog, List<DailyPortfolioSnapshot> series)
        {
            var alerts = new List<Dictionary<string, object?>>();
            void Add(string level, string type, string text) =>
                alerts.Add(new Dictionary<string, object?> { ["nivel"] = level, ["tipo"] = type, ["texto"] = text });

            foreach (var company in today.Where(c => c.WeightPct > settings.MaxCompanyPct))
            {
                Add("aviso", "concentracion_empresa", $"{company.CompanyName} pesa el {N(company.WeightPct)} % de la cartera de acciones (límite {N(settings.MaxCompanyPct)} %).");
            }

            var stocks = today.Sum(c => c.ValueBase);
            foreach (var sector in today.GroupBy(c => c.Sector).Select(g => (Name: g.Key, Pct: Pct(g.Sum(c => c.ValueBase), stocks))).Where(s => s.Pct > settings.MaxSectorPct))
            {
                Add("aviso", "concentracion_sector", $"El sector {sector.Name} pesa el {N(sector.Pct)} % de la cartera de acciones (límite {N(settings.MaxSectorPct)} %).");
            }

            var foreign = Pct(today.Where(c => c.CurrencyCode != last.CurrencyCode).Sum(c => c.ValueBase), stocks);
            if (foreign > settings.MaxForeignPct)
            {
                Add("aviso", "exposicion_divisa", $"El {N(foreign)} % de la cartera de acciones está en divisas distintas de {last.CurrencyCode} (límite {N(settings.MaxForeignPct)} %).");
            }

            foreach (var company in today)
            {
                var past = history.Where(h => h.CompanyId == company.CompanyId && h.Date <= last.Date.AddDays(-7)).MaxBy(h => h.Date);
                if (past != null && past.Price > 0)
                {
                    var change = R((company.Price / past.Price - 1m) * 100m);
                    if (change < settings.MaxDrop7dPct)
                    {
                        Add("aviso", "caida_7d", $"{company.CompanyName} ha caído un {N(-change)} % en 7 días (límite {N(-settings.MaxDrop7dPct)} %).");
                    }
                }
            }

            if (last.LiquidityPct < settings.MinLiquidityPct)
            {
                Add("aviso", "liquidez_baja", $"La liquidez es el {N(last.LiquidityPct)} % del patrimonio (mínimo {N(settings.MinLiquidityPct)} %).");
            }

            var heldIds = today.Select(c => c.CompanyId).ToHashSet();
            foreach (var company in catalog.Where(c => heldIds.Contains(c.Id)))
            {
                var updated = company.LivePriceUpdatedAt ?? company.UpdatedAt;
                if ((DateTime.Now - updated).TotalDays > settings.StalePriceDays)
                {
                    Add("info", "precio_sin_actualizar", $"El precio de {company.Name} no se actualiza desde el {updated:dd/MM/yyyy}.");
                }
            }

            var approximateDays = series.Count(s => s.Date > last.Date.AddDays(-7) && s.IsApproximate);
            if (approximateDays > 0)
            {
                Add("info", "datos_aproximados", $"{approximateDays} de los últimos 7 días tienen datos aproximados (ver calidad).");
            }

            return alerts.OrderBy(a => (string?)a["nivel"] == "aviso" ? 0 : 1).ToList();
        }

        private async Task<List<Dictionary<string, object?>>> BuildIndicesAsync(DateTime day, DateTime ytdStart, decimal? portfolioTwrYtd, CancellationToken cancellationToken)
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var index in await _context.BenchmarkIndices.AsNoTracking().OrderBy(i => i.Name).ToListAsync(cancellationToken))
            {
                var points = await _context.BenchmarkPricePoints
                    .AsNoTracking()
                    .Where(p => p.BenchmarkIndexId == index.Id && p.Date <= day && p.Date >= ytdStart.AddDays(-10))
                    .OrderBy(p => p.Date)
                    .Select(p => new { p.Date, p.Close })
                    .ToListAsync(cancellationToken);
                var start = points.LastOrDefault(p => p.Date.Date <= ytdStart);
                var end = points.LastOrDefault();
                decimal? ytd = start != null && end != null && start.Close > 0 ? R((end.Close / start.Close - 1m) * 100m) : null;
                result.Add(new Dictionary<string, object?>
                {
                    ["nombre"] = index.Name,
                    ["simbolo"] = index.Symbol,
                    ["var_ytd_pct"] = ytd,
                    ["cartera_vs_indice_pp"] = ytd.HasValue && portfolioTwrYtd.HasValue ? R(portfolioTwrYtd.Value - ytd.Value) : null,
                    ["ultimo_cierre_fecha"] = end != null ? Iso(end.Date) : null
                });
            }

            return result;
        }

        private async Task<List<Dictionary<string, object?>>> UpcomingDividendsAsync(List<CompanyDetail> catalog, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var lines = await _context.DividendPayments
                .AsNoTracking()
                .Where(d => d.Status == DividendPaymentRules.StatusPending && d.PaymentDate >= today)
                .OrderBy(d => d.PaymentDate)
                .Take(10)
                .ToListAsync(cancellationToken);

            return lines.Select(d =>
            {
                var company = catalog.FirstOrDefault(c => c.Id == d.CompanyId);
                return new Dictionary<string, object?>
                {
                    ["empresa"] = company?.Name ?? $"Empresa {d.CompanyId}",
                    ["fecha_pago"] = Iso(d.PaymentDate),
                    ["importe_estimado"] = d.AmountPerShare.HasValue ? R(d.AmountPerShare.Value * d.NumberOfShares) : null,
                    ["divisa"] = company?.CurrencyCode
                };
            }).ToList();
        }

        private static Dictionary<string, object?> Series(List<DailyPortfolioSnapshot> series, AiExportSettings settings, DateTime day)
        {
            var fullFrom = day.AddDays(-(settings.FullDays - 1));
            var compactFrom = day.AddDays(-(settings.CompactDays - 1));
            return new Dictionary<string, object?>
            {
                ["completa"] = new Dictionary<string, object?>
                {
                    ["columnas"] = new[] { "fecha", "patrimonio_eur", "invertido_eur", "cartera_valor_eur", "cartera_rent_pct", "cartera_rent_div_pct", "con_vendidas_rent_pct", "vendidas_resultado_eur", "aportacion_eur", "dividendos_eur", "liquidez_pct", "aproximado" },
                    ["filas"] = series.Where(s => s.Date >= fullFrom).Select(s => new object[]
                    {
                        Iso(s.Date), R(s.TotalValue), R(s.TotalInvested), R(s.OpenValue), R(s.OpenReturnPct), R(s.OpenReturnWithDivPct),
                        R(s.AllReturnPct), R(s.SoldResult), R(s.DayNetContribution), R(s.DayDividends), R(s.LiquidityPct), s.IsApproximate
                    }).ToList()
                },
                ["compacta"] = new Dictionary<string, object?>
                {
                    ["columnas"] = new[] { "fecha", "patrimonio_eur", "cartera_rent_pct", "aportacion_eur" },
                    ["filas"] = series.Where(s => s.Date >= compactFrom && s.Date < fullFrom).Select(s => new object[]
                    {
                        Iso(s.Date), R(s.TotalValue), R(s.OpenReturnPct), R(s.DayNetContribution)
                    }).ToList()
                }
            };
        }

        private static Dictionary<string, object?> Quality(DailyPortfolioSnapshot last, List<DailyPortfolioSnapshot> series, List<CompanyDetail> catalog, AiExportSettings settings) => new()
        {
            ["dias_en_serie"] = series.Count,
            ["dias_reconstruidos"] = series.Count(s => s.IsReconstructed),
            ["dias_aproximados"] = series.Count(s => s.IsApproximate),
            ["hoy_aproximado"] = last.IsApproximate,
            ["notas_hoy"] = last.ApproximateNote,
            ["nota"] = "reconstruido = calculado después con históricos; aproximado = faltaba algún precio, tipo o histórico"
        };

        private static string Summary(DailyPortfolioSnapshot s, Dictionary<string, object?> v, int alerts, Dictionary<string, object?>? index)
        {
            string P(object? value) => value is decimal d ? (d >= 0 ? "+" : "") + N(d) + " %" : "sin dato";
            var text = $"Patrimonio de {N(s.TotalValue)} {s.CurrencyCode} el {s.Date:dd/MM/yyyy}: {P(v["patrimonio_var_1d_pct"])} en el día y {P(v["patrimonio_var_ytd_pct"])} en el año (incluye aportaciones). " +
                       $"La cartera de acciones vale {N(s.OpenValue)} {s.CurrencyCode}, con una rentabilidad del {N(s.OpenReturnPct)} % ({N(s.OpenReturnWithDivPct)} % con dividendos) y {P(v["cartera_twr_ytd_pct"])} en el año sin el efecto de las compras. " +
                       $"Liquidez del {N(s.LiquidityPct)} %. ";
            if (index != null && index["cartera_vs_indice_pp"] is decimal diff)
            {
                text += $"Frente al {index["nombre"]}, la cartera va {(diff >= 0 ? "por delante" : "por detrás")} {N(Math.Abs(diff))} puntos en el año. ";
            }
            text += alerts == 0 ? "Sin alertas." : $"{alerts} {(alerts == 1 ? "alerta" : "alertas")}.";
            return text;
        }

        // ---------------------------------------------------------------- Cálculos

        /// <summary>Variación % entre el valor de la última captura ≤ fecha y el de hoy</summary>
        private static decimal? Change(List<DailyPortfolioSnapshot> series, DailyPortfolioSnapshot last, DateTime target, Func<DailyPortfolioSnapshot, decimal> value)
        {
            var past = series.LastOrDefault(s => s.Date <= target);
            return past != null && value(past) > 0 ? R((value(last) / value(past) - 1m) * 100m) : null;
        }

        /// <summary>
        /// Rentabilidad ponderada por tiempo de la cartera de acciones (SPEC-17-03): r_d = (V_d − A_d) ÷ V_{d−1} − 1,
        /// con V = valor de la cartera actual y A = aportación neta del día; se encadena entre las dos fechas.
        /// </summary>
        private static decimal? Twr(List<DailyPortfolioSnapshot> series, DateTime fromExclusive, DateTime to)
        {
            var start = series.LastOrDefault(s => s.Date <= fromExclusive);
            if (start == null)
            {
                return null;
            }

            var product = 1m;
            var previous = start.OpenValue;
            foreach (var day in series.Where(s => s.Date > fromExclusive && s.Date <= to))
            {
                if (previous > 0)
                {
                    product *= (day.OpenValue - day.DayNetContribution) / previous;
                }
                previous = day.OpenValue;
            }

            return R((product - 1m) * 100m);
        }

        private static decimal Pct(decimal part, decimal total) => total > 0 ? R(part / total * 100m) : 0m;

        private static decimal R(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string N(decimal value) => value.ToString("N2", Es);

        private static string Iso(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
