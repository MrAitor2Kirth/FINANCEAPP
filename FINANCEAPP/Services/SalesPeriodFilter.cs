using System.Globalization;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Modos de periodo del histórico de ventas (SPEC-02-05). Para añadir otro (p. ej. rango o trimestre)
    /// basta con un valor nuevo y su caso en <see cref="SalesPeriodFilter"/>.
    /// </summary>
    public enum SalesPeriodMode
    {
        /// <summary>Total histórico: sin límite de fechas</summary>
        All,
        /// <summary>Un día natural</summary>
        Day,
        /// <summary>Un mes natural</summary>
        Month,
        /// <summary>Un año natural</summary>
        Year
    }

    /// <summary>
    /// Filtro por periodo (SPEC-02-05): un modo y un día de referencia. Calcula su rango de fechas semiabierto
    /// [desde, hasta), su etiqueta en español y el periodo anterior o siguiente. No toca la base de datos.
    /// </summary>
    public sealed class SalesPeriodFilter
    {
        /// <summary>Primer y último año admitidos al informar un año a mano</summary>
        public const int MinYear = 2000;
        public const int MaxYear = 2100;

        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        private SalesPeriodFilter(SalesPeriodMode mode, DateTime reference)
        {
            Mode = mode;
            Reference = Normalize(mode, reference);
        }

        /// <summary>Total histórico (periodo por defecto)</summary>
        public static SalesPeriodFilter All { get; } = new(SalesPeriodMode.All, DateTime.Today);

        public SalesPeriodMode Mode { get; }

        /// <summary>Inicio del periodo (día, día 1 del mes o 1 de enero); en Total histórico no se usa</summary>
        public DateTime Reference { get; }

        /// <summary>Crea un filtro; la fecha de referencia se lleva al inicio del periodo</summary>
        public static SalesPeriodFilter Create(SalesPeriodMode mode, DateTime reference) => new(mode, reference);

        /// <summary>Crea un filtro de año; devuelve null si el año está fuera de 2000–2100</summary>
        public static SalesPeriodFilter? TryCreateYear(int year) =>
            year is >= MinYear and <= MaxYear ? new(SalesPeriodMode.Year, new DateTime(year, 1, 1)) : null;

        /// <summary>Cambia de modo conservando la fecha de referencia; desde Total histórico usa hoy (RF-4)</summary>
        public SalesPeriodFilter WithMode(SalesPeriodMode mode) =>
            new(mode, Mode == SalesPeriodMode.All ? DateTime.Today : Reference);

        /// <summary>Rango semiabierto [desde, hasta); ambos nulos en Total histórico</summary>
        public (DateTime? From, DateTime? ToExclusive) GetRange() => Mode switch
        {
            SalesPeriodMode.Day => (Reference, Reference.AddDays(1)),
            SalesPeriodMode.Month => (Reference, Reference.AddMonths(1)),
            SalesPeriodMode.Year => (Reference, Reference.AddYears(1)),
            _ => (null, null)
        };

        /// <summary>Periodo anterior; en Total histórico devuelve el mismo filtro</summary>
        public SalesPeriodFilter Previous() => Shift(-1);

        /// <summary>Periodo siguiente (puede ser futuro); en Total histórico devuelve el mismo filtro</summary>
        public SalesPeriodFilter Next() => Shift(1);

        /// <summary>Texto del periodo activo en español</summary>
        public string Label => Mode switch
        {
            SalesPeriodMode.Day => Reference.ToString("dd/MM/yyyy", Spanish),
            SalesPeriodMode.Month => char.ToUpper(Reference.ToString("MMMM 'de' yyyy", Spanish)[0], Spanish)
                + Reference.ToString("MMMM 'de' yyyy", Spanish)[1..],
            SalesPeriodMode.Year => $"Año {Reference.Year}",
            _ => "Total histórico"
        };

        private SalesPeriodFilter Shift(int direction)
        {
            try
            {
                return Mode switch
                {
                    SalesPeriodMode.Day => new(Mode, Reference.AddDays(direction)),
                    SalesPeriodMode.Month => new(Mode, Reference.AddMonths(direction)),
                    SalesPeriodMode.Year => new(Mode, Reference.AddYears(direction)),
                    _ => this
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                // Fuera del rango de fechas de .NET: se queda donde está
                return this;
            }
        }

        private static DateTime Normalize(SalesPeriodMode mode, DateTime reference) => mode switch
        {
            SalesPeriodMode.Month => new DateTime(reference.Year, reference.Month, 1),
            SalesPeriodMode.Year => new DateTime(reference.Year, 1, 1),
            _ => reference.Date
        };
    }
}
