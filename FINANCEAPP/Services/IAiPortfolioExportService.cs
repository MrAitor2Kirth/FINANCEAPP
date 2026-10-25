namespace FINANCEAPP.Services
{
    /// <summary>
    /// JSON para una IA local pequeña (SPEC-17-03, RF-6 y RF-7): plano, en español, con todo ya calculado (resumen,
    /// alertas, variaciones, rentabilidad sin aportaciones, reparto, índices, dividendos y serie), a partir de las
    /// capturas diarias. No usa ninguna IA ni envía nada: solo escribe un fichero local.
    /// </summary>
    public interface IAiPortfolioExportService
    {
        /// <summary>
        /// Escribe cartera-ia.json en la carpeta configurada (primero un .tmp que luego se renombra) y devuelve su ruta.
        /// Lanza si no se puede escribir.
        /// </summary>
        Task<string> WriteAsync(CancellationToken cancellationToken = default);
    }
}
