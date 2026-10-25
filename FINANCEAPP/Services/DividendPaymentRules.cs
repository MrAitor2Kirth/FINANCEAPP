using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Reglas puras del calendario de dividendos (SPEC-21-01): literales de estado y origen, validación de una línea,
    /// importe total, cuándo se puede generar la operación y normalización de tickers. Sin acceso a datos: las usan
    /// DividendService, CompanyService y OperationService sin depender unos de otros.
    /// </summary>
    public static class DividendPaymentRules
    {
        public const string StatusPending = "Pendiente";
        public const string StatusCollected = "Cobrado";

        public const string SourceManual = "Manual";
        public const string SourceCompanyForm = "Ficha";
        public const string SourceSearch = "Búsqueda";

        public const int MaxNotesLength = 200;

        /// <summary>
        /// Valida los datos de una línea (RF-1 y RF-3); null si son correctos. La empresa existente y el duplicado
        /// empresa + fecha de pago los comprueba el servicio.
        /// </summary>
        public static string? Validate(DividendPaymentInput input)
        {
            if (input.CompanyId <= 0)
            {
                return "Elige la empresa.";
            }

            if (input.PaymentDate == default)
            {
                return "La fecha de pago es obligatoria.";
            }

            if (input.ExDividendDate.HasValue && input.ExDividendDate.Value.Date > input.PaymentDate.Date)
            {
                return "La fecha ex-dividendo no puede ser posterior a la fecha de pago.";
            }

            if (input.AmountPerShare.HasValue && input.AmountPerShare.Value <= 0)
            {
                return "El importe por acción debe ser mayor que 0.";
            }

            if (input.AmountPerShare.HasValue && decimal.Round(input.AmountPerShare.Value, 4) != input.AmountPerShare.Value)
            {
                return "El importe por acción admite como máximo 4 decimales.";
            }

            if (input.NumberOfShares < 0)
            {
                return "El número de acciones no puede ser negativo.";
            }

            if (!ShareQuantity.HasValidPrecision(input.NumberOfShares))
            {
                return ShareQuantity.PrecisionMessage;
            }

            if (input.DividendPercent.HasValue && (input.DividendPercent.Value < 0 || input.DividendPercent.Value > 100))
            {
                return "El % dividendo debe estar entre 0 y 100.";
            }

            if ((input.Notes?.Trim().Length ?? 0) > MaxNotesLength)
            {
                return $"Las notas no pueden superar los {MaxNotesLength} caracteres.";
            }

            return null;
        }

        /// <summary>
        /// Importe total = acciones × importe por acción, redondeado a 2 decimales; null sin importe por acción
        /// </summary>
        public static decimal? CalculateTotal(decimal shares, decimal? amountPerShare)
        {
            return amountPerShare.HasValue
                ? Math.Round(shares * amountPerShare.Value, 2, MidpointRounding.AwayFromZero)
                : null;
        }

        /// <summary>
        /// Motivo por el que no se puede generar la operación de la línea (RF-6), o null si se puede:
        /// Pendiente, con importe por acción, con acciones y con la fecha de pago ya alcanzada.
        /// </summary>
        public static string? GetGenerateBlockReason(DividendPayment line, DateTime today)
        {
            if (line.Status != StatusPending || line.OperationId.HasValue)
            {
                return "Este dividendo ya está cobrado.";
            }

            if (!line.AmountPerShare.HasValue)
            {
                return "Falta el importe por acción.";
            }

            if (line.NumberOfShares <= 0)
            {
                return "Faltan las acciones.";
            }

            if (line.PaymentDate.Date > today.Date)
            {
                return $"Disponible el {line.PaymentDate:dd/MM/yyyy}.";
            }

            return null;
        }

        /// <summary>
        /// Ticker en mayúsculas y sin sufijo de bolsa (ACS.MC → ACS), para enlazar propuestas con empresas
        /// </summary>
        public static string NormalizeTicker(string? ticker)
        {
            var value = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            foreach (var suffix in StockPriceService.ExchangeSuffixFallbacks)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && value.Length > suffix.Length)
                {
                    return value[..^suffix.Length];
                }
            }

            return value;
        }
    }
}
