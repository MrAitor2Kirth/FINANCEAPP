using System.Globalization;
using FINANCEAPP.Models;

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
        /// Devuelve null si no hay precio base o no hay acciones (admiten 2 decimales, SPEC-02-08).
        /// </summary>
        public static decimal? Calculate(decimal pricePerShare, decimal? livePrice, decimal shares, decimal commission)
        {
            var basePrice = ResolveBasePrice(pricePerShare, livePrice);
            if (basePrice == null || shares <= 0)
            {
                return null;
            }

            return Math.Round(basePrice.Value * shares + Math.Max(commission, 0m), 2);
        }

        /// <summary>
        /// Valida los datos de una operación. Devuelve el mensaje de error en español
        /// o null si es válida.
        /// </summary>
        public static string? Validate(string? type, decimal pricePerShare, decimal? livePrice, decimal shares, decimal amount)
        {
            if (IsAutoCalculated(type))
            {
                if (ResolveBasePrice(pricePerShare, livePrice) == null)
                {
                    return "Indique el precio por acción o espere al precio en vivo: debe ser mayor que 0.";
                }

                if (shares <= 0)
                {
                    return "El número de acciones debe ser mayor que 0.";
                }
            }

            // Acciones fraccionadas con hasta 2 decimales en cualquier tipo de operación (SPEC-02-08)
            if (shares < 0)
            {
                return "El número de acciones no puede ser negativo.";
            }

            if (!ShareQuantity.HasValidPrecision(shares))
            {
                return ShareQuantity.PrecisionMessage;
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

        /// <summary>
        /// Mensaje cuando la liquidez de una venta no es positiva (SPEC-02-04)
        /// </summary>
        public const string SaleNetNotPositiveMessage = "La comisión no puede ser igual o mayor que el importe vendido.";

        /// <summary>
        /// Liquidez de una venta que se ingresa en el banco (SPEC-02-04): precio de venta × acciones − comisión
        /// de la propia venta, redondeado a 2 decimales y, si hay tipo de cambio, convertido a la divisa base.
        /// </summary>
        public static decimal CalculateSaleNet(decimal sellPrice, decimal shares, decimal commission, decimal? exchangeRate)
        {
            var net = Math.Round(Math.Round(sellPrice, 2) * shares - commission, 2);
            return exchangeRate.HasValue ? Math.Round(net * exchangeRate.Value, 2) : net;
        }

        /// <summary>
        /// Valor de la opción «No registrar en banco» del desplegable de cuenta (SPEC-02-06); las páginas lo usan por aquí
        /// porque su propiedad BankService oculta el nombre de la clase
        /// </summary>
        public const string NoBankMovementValue = BankService.NoBankMovementValue;

        /// <summary>
        /// Mensaje cuando una compra o dividendo en otra divisa no tiene tipo de cambio para registrarlo en el banco (SPEC-02-06)
        /// </summary>
        public static string MissingTradeRateMessage(string baseCurrency) =>
            $"Indica el tipo de cambio: el movimiento en la cuenta bancaria se hace en {baseCurrency}.";

        /// <summary>
        /// Clave de enlace del movimiento bancario de una compra o dividendo (SPEC-02-06):
        /// el lote original (OriginOperationId ?? Id), para que partir el lote no cambie el movimiento.
        /// </summary>
        public static int GetTradeMovementKey(Operation operation) => operation.OriginOperationId ?? operation.Id;

        /// <summary>
        /// Indica si el tipo de operación genera movimiento bancario (SPEC-02-06): Compra y Dividendo
        /// </summary>
        public static bool GeneratesBankMovement(string? type) => type == "Compra" || type == "Dividendo";

        /// <summary>
        /// Tipo del movimiento bancario: Gasto para una Compra, Ingreso para un Dividendo (SPEC-02-06)
        /// </summary>
        public static string TradeMovementType(string operationType) => operationType == "Compra" ? "Gasto" : "Ingreso";

        /// <summary>
        /// Importe del movimiento bancario de un lote (SPEC-02-06): suma de los importes de sus filas, convertida
        /// fila a fila con su propio tipo de cambio (null = divisa base) y redondeada a 2 decimales.
        /// </summary>
        public static decimal CalculateTradeMovementAmount(IEnumerable<Operation> rows) =>
            rows.Sum(r => Math.Round(r.Amount * (r.ExchangeRate ?? 1m), 2));

        /// <summary>
        /// Descripción del movimiento bancario de una compra o dividendo (SPEC-02-06)
        /// </summary>
        public static string BuildTradeMovementDescription(string operationType, string companyName, decimal shares, int key) =>
            operationType == "Compra"
                ? $"Compra de {ShareQuantity.Format(shares)} acciones de {companyName} (operación nº {key})"
                : $"Dividendo de {companyName} (operación nº {key})";

        /// <summary>
        /// Descripción del ingreso bancario de una venta (SPEC-02-04): acciones, empresa, nº de venta,
        /// beneficio en la divisa de la venta y rentabilidad en porcentaje.
        /// </summary>
        public static string BuildSaleDepositDescription(SaleHistory record, string? currencyCode)
        {
            var text = $"Venta de {ShareQuantity.Format(record.NumberOfShares)} acciones de {record.CompanyName} (venta nº {record.Id}) · beneficio {CurrencyFormatter.Format(record.Profit, currencyCode)}";
            if (record.ReturnPercent.HasValue)
            {
                text += $" · {record.ReturnPercent.Value.ToString("+0.00;-0.00;0.00", CultureInfo.GetCultureInfo("es-ES"))} %";
            }

            return text;
        }
    }
}
