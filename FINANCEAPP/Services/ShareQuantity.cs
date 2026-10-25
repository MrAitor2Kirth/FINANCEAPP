using System.Globalization;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Número de acciones con hasta 2 decimales (acciones fraccionadas, SPEC-02-08): redondeo, precisión y formato.
    /// SQLite puede devolver las cantidades guardadas como reales (p. ej. 3,4499999): se redondean al calcular.
    /// </summary>
    public static class ShareQuantity
    {
        public const int Decimals = 2;

        public const string PrecisionMessage = "Las acciones admiten como máximo 2 decimales.";

        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        /// <summary>Redondea a 2 decimales</summary>
        public static decimal Round(decimal shares) => Math.Round(shares, Decimals, MidpointRounding.AwayFromZero);

        /// <summary>true si la cantidad tiene como máximo 2 decimales</summary>
        public static bool HasValidPrecision(decimal shares) => decimal.Round(shares, Decimals) == shares;

        /// <summary>Con los decimales que tenga, hasta 2 y en español: 12 · 12,5 · 0,25</summary>
        public static string Format(decimal shares) => Round(shares).ToString("#,##0.##", Spanish);
    }
}
