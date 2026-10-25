using System;
using System.Collections.Generic;

namespace FINANCEAPP.Data
{
    /// <summary>
    /// Propuesta de un dividendo ya anunciado por la empresa (SPEC-21-01, RF-11), investigada en la web al implementar.
    /// Se lista en /dividends para que el usuario la convierta en línea; nunca crea operaciones.
    /// </summary>
    /// <param name="Ticker">Ticker de la empresa (se compara sin sufijo de bolsa)</param>
    /// <param name="ExDividendDate">Fecha ex-dividendo, si la da la fuente</param>
    /// <param name="PaymentDate">Fecha de pago anunciada</param>
    /// <param name="AmountPerShare">Importe por acción anunciado, en la divisa de cotización</param>
    /// <param name="DividendPercent">% dividendo si lo da la fuente</param>
    /// <param name="Source">Fuente de la información</param>
    public sealed record DividendProposal(
        string Ticker,
        DateTime? ExDividendDate,
        DateTime PaymentDate,
        decimal? AmountPerShare,
        decimal? DividendPercent,
        string Source);

    /// <summary>
    /// Dividendos anunciados de las empresas del usuario. Solo dividendos confirmados, nunca estimaciones.
    /// Se actualiza a mano (no hay búsqueda automática).
    /// Búsqueda del 24/10/2026: de las empresas con acciones, solo Microsoft tenía anunciado su próximo dividendo.
    /// Mapfre, Enagás, Endesa, Thermo Fisher, Airbus, Eiffage y Clínica Baviera solo tenían fechas estimadas por
    /// terceros (se descartan); AST SpaceMobile no paga dividendo.
    /// </summary>
    public static class DividendProposalsData
    {
        public static IReadOnlyList<DividendProposal> Proposals { get; } = new List<DividendProposal>
        {
            new("MSFT", new DateTime(2026, 11, 19), new DateTime(2026, 12, 10), 0.98m, null,
                "Microsoft, comunicado del 15/09/2026: dividendo trimestral de 0,98 $ (registro 19/11/2026)"),
        };
    }
}
