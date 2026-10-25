namespace FINANCEAPP.Services
{
    /// <summary>
    /// Histórico diario de tipos de cambio por par de divisas (SPEC-01-09, RF-4), descargado de Yahoo
    /// (<c>{DIVISA}{BASE}=X</c>) y guardado en CurrencyRatePoints con las reglas del histórico de cotización.
    /// </summary>
    public interface ICurrencyHistoryService
    {
        /// <summary>
        /// Tipo de cambio (unidades de la divisa base por 1 unidad de <paramref name="currencyCode"/>) de cada fecha:
        /// el cierre de ese día o el del último anterior. Descarga y guarda lo que falte hasta la fecha más antigua.
        /// Las fechas sin dato quedan fuera del diccionario (sin conexión o par desconocido en Yahoo).
        /// </summary>
        Task<Dictionary<DateTime, decimal>> GetRatesAtAsync(string currencyCode, string baseCurrencyCode, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);
    }
}
