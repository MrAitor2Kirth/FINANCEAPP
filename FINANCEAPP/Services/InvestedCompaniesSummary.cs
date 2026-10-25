using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Reglas de las empresas con inversión y de los indicadores de resumen de /companies (SPEC-01-05).
    /// Clase estática sin estado ni acceso a datos: la página solo la invoca.
    /// </summary>
    public static class InvestedCompaniesSummary
    {
        /// <summary>
        /// Una empresa tiene inversión cuando su total invertido inicial es mayor que 0
        /// </summary>
        public static bool HasInvestment(CompanyDetail company) => company.TotalInvestedInitial > 0;

        /// <summary>
        /// Empresas con inversión de la lista indicada, en el mismo orden
        /// </summary>
        public static List<CompanyDetail> GetInvested(IEnumerable<CompanyDetail> companies) =>
            companies.Where(HasInvestment).ToList();

        /// <summary>
        /// Media simple del dividend yield de las empresas con inversión; null si no hay ninguna
        /// </summary>
        public static decimal? AverageDividendYield(IReadOnlyCollection<CompanyDetail> invested) =>
            invested.Count == 0 ? null : invested.Average(c => c.DividendYield);

        /// <summary>
        /// Peso de cada empresa en la cartera (SPEC-01-06): su inversión convertida a la divisa base ÷ la suma de la
        /// inversión de las empresas con inversión, × 100 y redondeado a 2 decimales. Id de empresa → porcentaje;
        /// las empresas sin inversión o sin tipo de cambio no aparecen. Es el peso que usa la rentabilidad media.
        /// </summary>
        public static Dictionary<int, decimal> GetWeights(ConvertedCompanyTotals totals)
        {
            var total = totals.InitialByCompanyId.Values.Where(v => v > 0).Sum();
            if (total <= 0)
            {
                return new Dictionary<int, decimal>();
            }

            return totals.InitialByCompanyId
                .Where(kv => kv.Value > 0)
                .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value / total * 100m, 2, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Resultado de la cartera en la divisa base (SPEC-01-08): valor actual − invertido de las empresas con inversión,
        /// sin dividendos y redondeado a 2 decimales. Null si el total invertido no es mayor que 0.
        /// </summary>
        public static decimal? UnrealizedResult(ConvertedCompanyTotals totals)
        {
            if (totals.InvestedInitial <= 0)
            {
                return null;
            }

            return Math.Round(totals.CurrentValue - totals.InvestedInitial, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rentabilidad media por precio ponderada por lo invertido: (valor actual − invertido) ÷ invertido × 100,
        /// con los totales ya convertidos a la divisa base de las empresas con inversión. Equivale a la media de
        /// sus rentabilidades ponderada por su inversión. Null si el total invertido no es mayor que 0.
        /// </summary>
        public static decimal? WeightedReturnPercent(ConvertedCompanyTotals totals)
        {
            if (totals.InvestedInitial <= 0)
            {
                return null;
            }

            return Math.Round(
                (totals.CurrentValue - totals.InvestedInitial) / totals.InvestedInitial * 100m,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rentabilidad media con dividendos (SPEC-01-07): (valor actual + dividendos − invertido) ÷ invertido × 100,
        /// con los totales convertidos de las empresas con inversión. Equivale a la media de su rentabilidad «Con div.»
        /// ponderada por su inversión; los dividendos de empresas ya vendidas por completo no entran.
        /// Null si el total invertido no es mayor que 0.
        /// </summary>
        public static decimal? WeightedReturnWithDividendsPercent(ConvertedCompanyTotals totals)
        {
            if (totals.InvestedInitial <= 0)
            {
                return null;
            }

            return Math.Round(
                (totals.CurrentValue + totals.Dividends - totals.InvestedInitial) / totals.InvestedInitial * 100m,
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}
