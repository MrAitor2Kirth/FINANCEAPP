namespace FINANCEAPP.Services
{
    /// <summary>
    /// Granularidad con la que se solicita el histórico de cotización para el gráfico.
    /// Los valores nuevos van al final para no cambiar el número de los existentes.
    /// </summary>
    public enum ChartGranularity
    {
        Daily,
        Monthly,
        Yearly,

        /// <summary>
        /// Barras de una hora descargadas de Yahoo y guardadas solo en memoria (SPEC-05-02)
        /// </summary>
        Hourly,

        /// <summary>
        /// Semanas de lunes a domingo agregadas desde el histórico diario (SPEC-05-02)
        /// </summary>
        Weekly
    }
}
