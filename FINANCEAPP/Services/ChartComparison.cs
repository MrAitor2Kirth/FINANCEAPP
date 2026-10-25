using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Cálculos de la comparación de dos series en el gráfico (SPEC-05-04, RF-4 a RF-6): alineación de la serie
    /// comparada con las sesiones de la empresa, base 100 desde el primer periodo visible, rentabilidad de cada
    /// serie y correlación de sus rentabilidades de periodo. Sin estado ni acceso a datos.
    /// </summary>
    public static class ChartComparison
    {
        /// <summary>Periodos comunes mínimos para calcular la correlación</summary>
        public const int MinCorrelationPeriods = 10;

        /// <summary>
        /// Cierre de la serie comparada en cada periodo de la empresa: el último conocido con fecha (u hora, en Horas)
        /// menor o igual que la del periodo; null antes de su primer dato (RF-5). Las dos listas van ordenadas por fecha.
        /// </summary>
        public static List<decimal?> Align(IReadOnlyList<PriceHistoryPoint> mainPoints, IReadOnlyList<PriceHistoryPoint> comparedPoints)
        {
            var aligned = new List<decimal?>(mainPoints.Count);
            var j = 0;
            decimal? last = null;
            foreach (var point in mainPoints)
            {
                while (j < comparedPoints.Count && comparedPoints[j].Date <= point.Date)
                {
                    last = comparedPoints[j].Close;
                    j++;
                }

                aligned.Add(last);
            }

            return aligned;
        }

        /// <summary>
        /// Base 100, rentabilidades y correlación del tramo visible [<paramref name="startIndex"/>, <paramref name="endIndex"/>]
        /// (índices de periodo de la empresa). Las series en base 100 cubren todos los periodos (para poder desplazarse
        /// sin huecos) y valen 100 en el primer periodo del tramo con valor en las dos.
        /// </summary>
        public static ComparisonResult Compute(IReadOnlyList<decimal?> mainCloses, IReadOnlyList<decimal?> comparedCloses, int startIndex, int endIndex)
        {
            var count = Math.Min(mainCloses.Count, comparedCloses.Count);
            var result = new ComparisonResult
            {
                Main = new List<decimal?>(new decimal?[count]),
                Compared = new List<decimal?>(new decimal?[count])
            };

            if (count == 0)
            {
                result.CorrelationLabel = "Pocos datos para calcular la correlación";
                return result;
            }

            startIndex = Math.Clamp(startIndex, 0, count - 1);
            endIndex = Math.Clamp(endIndex, startIndex, count - 1);

            // Primer y último periodo del tramo con precio en las dos series
            var first = -1;
            var lastIndex = -1;
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (IsValid(mainCloses[i]) && IsValid(comparedCloses[i]))
                {
                    if (first < 0)
                    {
                        first = i;
                    }

                    lastIndex = i;
                }
            }

            // Sin periodo común en el tramo: cada serie toma su primer valor visible como base (caso límite de la spec)
            var mainBase = first >= 0 ? mainCloses[first] : FirstValid(mainCloses, startIndex, endIndex);
            var comparedBase = first >= 0 ? comparedCloses[first] : FirstValid(comparedCloses, startIndex, endIndex);

            for (var i = 0; i < count; i++)
            {
                result.Main[i] = Rebase(mainCloses[i], mainBase);
                result.Compared[i] = Rebase(comparedCloses[i], comparedBase);
            }

            if (first >= 0 && lastIndex > first)
            {
                result.MainReturnPercent = Math.Round((mainCloses[lastIndex]!.Value / mainCloses[first]!.Value - 1m) * 100m, 2);
                result.ComparedReturnPercent = Math.Round((comparedCloses[lastIndex]!.Value / comparedCloses[first]!.Value - 1m) * 100m, 2);
            }

            // Rentabilidades de periodo con pares completos (periodo y anterior con precio en las dos)
            var mainReturns = new List<decimal>();
            var comparedReturns = new List<decimal>();
            for (var i = Math.Max(startIndex + 1, 1); i <= endIndex; i++)
            {
                if (IsValid(mainCloses[i]) && IsValid(mainCloses[i - 1]) && IsValid(comparedCloses[i]) && IsValid(comparedCloses[i - 1]))
                {
                    mainReturns.Add(mainCloses[i]!.Value / mainCloses[i - 1]!.Value - 1m);
                    comparedReturns.Add(comparedCloses[i]!.Value / comparedCloses[i - 1]!.Value - 1m);
                }
            }

            result.CommonPeriods = mainReturns.Count;
            result.Correlation = mainReturns.Count >= MinCorrelationPeriods ? Pearson(mainReturns, comparedReturns) : null;
            result.CorrelationLabel = result.Correlation.HasValue
                ? GetCorrelationLabel(result.Correlation.Value)
                : "Pocos datos para calcular la correlación";

            return result;
        }

        /// <summary>
        /// Palabra guía de la correlación (RF-6): fuerte ≥ 0,7; moderada 0,4–0,7; débil 0,2–0,4; sin relación −0,2–0,2; inversa &lt; −0,2
        /// </summary>
        public static string GetCorrelationLabel(decimal correlation)
        {
            if (correlation >= 0.7m)
            {
                return "fuerte";
            }

            if (correlation >= 0.4m)
            {
                return "moderada";
            }

            if (correlation >= 0.2m)
            {
                return "débil";
            }

            if (correlation >= -0.2m)
            {
                return "sin relación";
            }

            return "inversa";
        }

        /// <summary>
        /// Coeficiente de correlación de Pearson, redondeado a 2 decimales; null si alguna serie no varía
        /// </summary>
        private static decimal? Pearson(IReadOnlyList<decimal> x, IReadOnlyList<decimal> y)
        {
            var n = x.Count;
            var meanX = x.Sum() / n;
            var meanY = y.Sum() / n;

            decimal covariance = 0m, varianceX = 0m, varianceY = 0m;
            for (var i = 0; i < n; i++)
            {
                var dx = x[i] - meanX;
                var dy = y[i] - meanY;
                covariance += dx * dy;
                varianceX += dx * dx;
                varianceY += dy * dy;
            }

            if (varianceX == 0m || varianceY == 0m)
            {
                return null;
            }

            // La raíz cuadrada no existe en decimal: se calcula en double
            var denominator = Math.Sqrt((double)varianceX) * Math.Sqrt((double)varianceY);
            var correlation = (double)covariance / denominator;
            return Math.Round((decimal)Math.Clamp(correlation, -1d, 1d), 2);
        }

        private static bool IsValid(decimal? value) => value.HasValue && value.Value > 0m;

        private static decimal? FirstValid(IReadOnlyList<decimal?> values, int startIndex, int endIndex)
        {
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (IsValid(values[i]))
                {
                    return values[i];
                }
            }

            return null;
        }

        private static decimal? Rebase(decimal? value, decimal? baseValue)
        {
            if (!IsValid(value) || !IsValid(baseValue))
            {
                return null;
            }

            return Math.Round(value!.Value / baseValue!.Value * 100m, 2);
        }
    }

    /// <summary>
    /// Resultado de la comparación en el tramo visible (SPEC-05-04)
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>Empresa en base 100, un valor por periodo (null sin precio)</summary>
        public List<decimal?> Main { get; set; } = new();

        /// <summary>Serie comparada en base 100, alineada con los periodos de la empresa</summary>
        public List<decimal?> Compared { get; set; } = new();

        /// <summary>Rentabilidad de la empresa en el tramo (%); null sin dos periodos comunes</summary>
        public decimal? MainReturnPercent { get; set; }

        /// <summary>Rentabilidad de la serie comparada en el tramo (%)</summary>
        public decimal? ComparedReturnPercent { get; set; }

        /// <summary>Correlación de −1 a 1 con 2 decimales; null con pocos datos</summary>
        public decimal? Correlation { get; set; }

        /// <summary>Palabra guía o aviso de pocos datos</summary>
        public string CorrelationLabel { get; set; } = string.Empty;

        /// <summary>Rentabilidades de periodo comunes usadas en la correlación</summary>
        public int CommonPeriods { get; set; }
    }
}
