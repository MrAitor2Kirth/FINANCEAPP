using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Fila de la tarjeta «Tipos de cambio» de Configuración: una divisa en uso distinta de la base
    /// </summary>
    public class ExchangeRateRow
    {
        /// <summary>Divisa de origen (por ejemplo USD)</summary>
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Divisa base actual</summary>
        public string BaseCurrencyCode { get; set; } = string.Empty;

        /// <summary>Número de empresas que usan esa divisa</summary>
        public int CompaniesCount { get; set; }

        /// <summary>Tipo actual guardado; nulo si todavía no hay</summary>
        public ExchangeRate? Rate { get; set; }
    }

    /// <summary>
    /// Resultado de actualizar o guardar un tipo de cambio
    /// </summary>
    public class ExchangeRateResult
    {
        /// <summary>True si el tipo se guardó</summary>
        public bool Success { get; set; }

        /// <summary>Motivo del fallo en español</summary>
        public string? Error { get; set; }

        /// <summary>Tipo guardado (si Success)</summary>
        public ExchangeRate? Rate { get; set; }

        public static ExchangeRateResult Ok(ExchangeRate rate) => new() { Success = true, Rate = rate };

        public static ExchangeRateResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// Totales de las empresas convertidos a la divisa base con el tipo actual
    /// </summary>
    public class ConvertedCompanyTotals
    {
        /// <summary>Divisa base de los importes</summary>
        public string BaseCurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Suma del total invertido inicial</summary>
        public decimal InvestedInitial { get; set; }

        /// <summary>Suma del valor actual (precio en vivo o manual por acciones)</summary>
        public decimal CurrentValue { get; set; }

        /// <summary>Suma de los dividendos</summary>
        public decimal Dividends { get; set; }

        /// <summary>Empresas incluidas en los totales (divisa base o con tipo)</summary>
        public List<CompanyDetail> Included { get; set; } = new();

        /// <summary>Empresas en otra divisa sin tipo actual: quedan fuera de los totales</summary>
        public int MissingRateCompanies { get; set; }
    }

    /// <summary>
    /// Totales del histórico de ventas convertidos a la divisa base con el tipo actual
    /// </summary>
    public class ConvertedSaleTotals
    {
        /// <summary>Divisa base de los importes</summary>
        public string BaseCurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Suma del importe vendido</summary>
        public decimal Amount { get; set; }

        /// <summary>Suma de las comisiones</summary>
        public decimal Commission { get; set; }

        /// <summary>Suma del beneficio</summary>
        public decimal Profit { get; set; }

        /// <summary>Ventas en otra divisa sin tipo actual: quedan fuera de los totales</summary>
        public int MissingRateSales { get; set; }

        /// <summary>Ventas en otra divisa convertidas con el tipo actual</summary>
        public int ConvertedSales { get; set; }
    }

    /// <summary>
    /// Una línea de la ventana «Detalles»: concepto, importe original y, si hay tipo, importe convertido
    /// </summary>
    public class ExchangeDetailLine
    {
        public string Label { get; set; } = string.Empty;
        public string Original { get; set; } = string.Empty;
        public string? Converted { get; set; }
    }

    /// <summary>
    /// Datos de la ventana «Detalles» de una operación, una venta o una empresa
    /// </summary>
    public class ExchangeDetails
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string OriginalCurrencyCode { get; set; } = string.Empty;
        public string? TargetCurrencyCode { get; set; }

        /// <summary>True si la divisa es la base: la ventana muestra una sola columna de importes, sin tipo ni conversión</summary>
        public bool IsBaseCurrency { get; set; }

        /// <summary>True si hay un tipo (guardado o actual) con el que convertir</summary>
        public bool HasRate { get; set; }

        /// <summary>Texto del tipo, por ejemplo «1 USD = 0,92 EUR»</summary>
        public string? RateText { get; set; }

        /// <summary>Procedencia del tipo: guardado en la operación, actual (con origen y fecha) o ninguno</summary>
        public string? RateNote { get; set; }

        public List<ExchangeDetailLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Servicio de tipos de cambio (SPEC-01-03): tipos actuales, conversión, totales convertidos y «Detalles»
    /// </summary>
    public interface IExchangeRateService
    {
        /// <summary>Divisas en uso por las empresas distintas de la base, con su tipo actual si existe</summary>
        Task<List<ExchangeRateRow>> GetRowsAsync();

        /// <summary>Tipo actual de la divisa respecto a la divisa base actual; nulo si no hay</summary>
        Task<ExchangeRate?> GetCurrentRateAsync(string? currencyCode);

        /// <summary>Pide el tipo a Yahoo (par directo o inverso) y lo guarda con origen Yahoo</summary>
        Task<ExchangeRateResult> RefreshAsync(string currencyCode);

        /// <summary>Actualiza uno a uno los tipos de las divisas en uso; avisa tras cada una y devuelve cuántas salieron bien</summary>
        Task<int> RefreshAllAsync(Func<string, ExchangeRateResult, Task>? onResult = null);

        /// <summary>Guarda un tipo escrito a mano (debe ser mayor que 0)</summary>
        Task<ExchangeRateResult> SaveManualAsync(string currencyCode, decimal rate);

        /// <summary>Totales de las empresas dadas convertidos a la divisa base con el tipo actual</summary>
        Task<ConvertedCompanyTotals> GetConvertedTotalsAsync(IEnumerable<CompanyDetail> companies);

        /// <summary>
        /// Totales del histórico de ventas (importe, comisiones y beneficio) convertidos a la divisa base
        /// con el tipo actual. La divisa de cada venta es la de su empresa (EUR si no se encuentra).
        /// </summary>
        /// <param name="sales">Ventas del histórico</param>
        /// <param name="currencyByCompany">Divisa de cada empresa por nombre</param>
        Task<ConvertedSaleTotals> GetConvertedSaleTotalsAsync(IEnumerable<SaleHistory> sales, IReadOnlyDictionary<string, string> currencyByCompany);

        /// <summary>Datos de «Detalles» de una operación en otra divisa</summary>
        Task<ExchangeDetails> GetOperationDetailsAsync(Operation operation);

        /// <summary>Datos de «Detalles» de una venta del histórico</summary>
        Task<ExchangeDetails> GetSaleDetailsAsync(SaleHistory sale);

        /// <summary>Datos de «Detalles» de una empresa (invertido, valor actual y dividendos)</summary>
        Task<ExchangeDetails> GetCompanyDetailsAsync(CompanyDetail company);
    }
}
