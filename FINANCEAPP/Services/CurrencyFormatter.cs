using System.Globalization;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Formato de importes con símbolo de divisa (SPEC-01-02). Un único sitio para que
    /// ninguna pantalla dependa de la cultura del sistema para el símbolo.
    /// </summary>
    public static class CurrencyFormatter
    {
        /// <summary>Divisa por defecto (empresas y operaciones sin país)</summary>
        public const string DefaultCurrency = "EUR";

        /// <summary>Normaliza un código de divisa: mayúsculas, o EUR si está vacío</summary>
        public static string Normalize(string? code) =>
            string.IsNullOrWhiteSpace(code) ? DefaultCurrency : code.Trim().ToUpperInvariant();

        /// <summary>Símbolo de la divisa; si no es una de las conocidas, devuelve el propio código</summary>
        public static string Symbol(string? code) => Normalize(code) switch
        {
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            "JPY" => "¥",
            var other => other
        };

        /// <summary>Importe con separadores de la cultura actual, un espacio y el símbolo (p. ej. 1.234,50 €)</summary>
        public static string Format(decimal amount, string? code, int decimals = 2) =>
            $"{amount.ToString("N" + decimals, CultureInfo.CurrentCulture)} {Symbol(code)}";

        /// <summary>Etiqueta de divisa para listados y columnas (p. ej. «USD $»)</summary>
        public static string Label(string? code)
        {
            var normalized = Normalize(code);
            var symbol = Symbol(normalized);
            return symbol == normalized ? normalized : $"{normalized} {symbol}";
        }
    }
}
