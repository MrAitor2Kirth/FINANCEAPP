namespace FINANCEAPP.Services
{
    /// <summary>
    /// Reglas de cálculo y validación del importe total de una operación (SPEC-02-02).
    /// Clase estática sin estado ni acceso a datos: la página solo la invoca.
    /// </summary>
    public static class OperationAmountCalculator
    {
        /// <summary>
        /// Importe mínimo permitido de una operación
        /// </summary>
        public const decimal MinAmount = 0.01m;

        /// <summary>
        /// Importe máximo permitido de una operación
        /// </summary>
        public const decimal MaxAmount = 999999999.99m;

        /// <summary>
        /// Indica si el importe de este tipo de operación se calcula automáticamente.
        /// Solo Compra y Venta; Dividendo y Traspaso llevan el importe a mano.
        /// </summary>
        public static bool IsAutoCalculated(string? type)
        {
            return type == "Compra" || type == "Venta";
        }

        /// <summary>
        /// Elige el precio base: el precio por acción si está informado (mayor que 0);
        /// si no, el precio en vivo si está informado; si no, null.
        /// </summary>
        public static decimal? ResolveBasePrice(decimal pricePerShare, decimal? livePrice)
        {
            if (pricePerShare > 0)
            {
                return pricePerShare;
            }

            if (livePrice.HasValue && livePrice.Value > 0)
            {
                return livePrice.Value;
            }

            return null;
        }

        /// <summary>
        /// Calcula precio base × acciones + comisión, redondeado a 2 decimales.
        /// Devuelve null si no hay precio base o las acciones son menores que 1.
        /// </summary>
        public static decimal? Calculate(decimal pricePerShare, decimal? livePrice, int shares, decimal commission)
        {
            var basePrice = ResolveBasePrice(pricePerShare, livePrice);
            if (basePrice == null || shares < 1)
            {
                return null;
            }

            return Math.Round(basePrice.Value * shares + Math.Max(commission, 0m), 2);
        }

        /// <summary>
        /// Valida los datos de una operación. Devuelve el mensaje de error en español
        /// o null si es válida.
        /// </summary>
        public static string? Validate(string? type, decimal pricePerShare, decimal? livePrice, int shares, decimal amount)
        {
            if (IsAutoCalculated(type))
            {
                if (ResolveBasePrice(pricePerShare, livePrice) == null)
                {
                    return "Indique el precio por acción o espere al precio en vivo: debe ser mayor que 0.";
                }

                if (shares < 1)
                {
                    return "El número de acciones debe ser un entero mayor o igual que 1.";
                }
            }

            if (amount < MinAmount)
            {
                return "El importe total debe ser al menos 0,01.";
            }

            if (amount > MaxAmount)
            {
                return "El importe total supera el máximo permitido (999.999.999,99).";
            }

            return null;
        }
    }
}
