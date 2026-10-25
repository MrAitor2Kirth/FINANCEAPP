using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio que implementa la lógica de negocio para gestionar operaciones financieras
    /// Utiliza Entity Framework Core para el acceso a datos
    /// </summary>
    public class OperationService : IOperationService
    {
        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly ICountryService _countryService;
        private readonly IBankService _bankService;

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        /// <param name="companyService">Servicio de empresas para recalcular importes</param>
        public OperationService(AppDbContext context, ICompanyService companyService, ICountryService countryService, IBankService bankService)
        {
            _context = context;
            _companyService = companyService;
            _countryService = countryService;
            _bankService = bankService;
        }

        /// <summary>
        /// Mensaje cuando una venta en otra divisa no tiene tipo de cambio para ingresarla en el banco (SPEC-02-04)
        /// </summary>
        private static string MissingRateMessage(string baseCurrency) =>
            $"Indica el tipo de cambio: el ingreso en la cuenta bancaria se hace en {baseCurrency}.";

        /// <summary>
        /// Normaliza el tipo de cambio de la operación (SPEC-01-03): en divisa base se borra; en otra divisa,
        /// si viene debe ser mayor que 0, se redondea a 6 decimales y se sella con la divisa base como destino.
        /// Sin tipo se guarda la operación sin él.
        /// </summary>
        private async Task ApplyExchangeRateAsync(Operation operation)
        {
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            if (operation.EffectiveCurrencyCode == baseCurrency || !operation.ExchangeRate.HasValue)
            {
                operation.ExchangeRate = null;
                operation.ExchangeRateTo = null;
                return;
            }

            if (operation.ExchangeRate.Value <= 0)
            {
                throw new InvalidOperationException("El tipo de cambio debe ser mayor que 0.");
            }

            operation.ExchangeRate = Math.Round(operation.ExchangeRate.Value, 6);
            operation.ExchangeRateTo = baseCurrency;
        }

        /// <summary>
        /// Obtiene todas las operaciones ordenadas por fecha descendente
        /// </summary>
        public async Task<List<Operation>> GetAllAsync()
        {
            return await _context.Operations
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene una operación por su ID
        /// </summary>
        public async Task<Operation?> GetByIdAsync(int id)
        {
            return await _context.Operations
                .FindAsync(id);
        }

        /// <summary>
        /// Obtiene todas las operaciones de una empresa específica
        /// </summary>
        public async Task<List<Operation>> GetByCompanyAsync(string companyName)
        {
            return await _context.Operations
                .Where(o => o.CompanyName.Contains(companyName))
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene operaciones filtradas por tipo
        /// </summary>
        public async Task<List<Operation>> GetByTypeAsync(string type)
        {
            return await _context.Operations
                .Where(o => o.Type == type)
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las operaciones que no tienen fecha de cierre (operaciones abiertas)
        /// </summary>
        public async Task<List<Operation>> GetOpenOperationsAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate == null)
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las operaciones que tienen fecha de cierre (operaciones cerradas)
        /// </summary>
        public async Task<List<Operation>> GetClosedOperationsAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate != null)
                .OrderByDescending(o => o.CloseDate)
                .ToListAsync();
        }

        /// <summary>
        /// Divisa de una empresa a partir de su país (SPEC-01-02). Null si la empresa no existe,
        /// no tiene país o el país ya no está en el catálogo (la operación se queda sin divisa = EUR).
        /// </summary>
        private async Task<string?> GetCompanyCurrencyAsync(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return null;
            }

            var countryCode = await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Name == companyName)
                .Select(c => c.CountryCode)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            var currency = await _context.Countries
                .AsNoTracking()
                .Where(c => c.Code == countryCode)
                .Select(c => c.CurrencyCode)
                .FirstOrDefaultAsync();
            return currency == null ? null : CurrencyFormatter.Normalize(currency);
        }

        /// <summary>
        /// Añade una nueva operación a la base de datos
        /// </summary>
        public async Task<Operation> AddAsync(Operation operation, string? bankAccountName = null, int? dividendPaymentId = null)
        {
            operation.CreatedAt = DateTime.Now;
            operation.CurrencyCode = await GetCompanyCurrencyAsync(operation.CompanyName);
            await ApplyExchangeRateAsync(operation);

            // Movimiento bancario de una Compra o Dividendo (SPEC-02-06): cuenta y tipo de cambio se validan antes de tocar nada
            BankAccount? account = null;
            if (OperationAmountCalculator.GeneratesBankMovement(operation.Type))
            {
                account = await _bankService.ResolveTradeAccountAsync(bankAccountName);
                if (account != null)
                {
                    var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
                    if (operation.EffectiveCurrencyCode != baseCurrency && !operation.ExchangeRate.HasValue)
                    {
                        throw new InvalidOperationException(OperationAmountCalculator.MissingTradeRateMessage(baseCurrency));
                    }
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Operations.Add(operation);
                await _context.SaveChangesAsync();

                // Línea del calendario de dividendos cobrada con esta operación (SPEC-21-01, RF-6). Se relee aquí:
                // si otra acción ya la cobró, se deshace todo y no hay dos operaciones del mismo dividendo
                if (dividendPaymentId.HasValue)
                {
                    var line = await _context.DividendPayments.FirstOrDefaultAsync(d => d.Id == dividendPaymentId.Value);
                    if (line == null || line.Status != DividendPaymentRules.StatusPending || line.OperationId.HasValue)
                    {
                        throw new InvalidOperationException("Este dividendo ya está cobrado o ya no existe.");
                    }

                    line.Status = DividendPaymentRules.StatusCollected;
                    line.OperationId = operation.Id;
                    line.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                if (account != null)
                {
                    await _bankService.AddTradeMovementAsync(
                        account.Name,
                        OperationAmountCalculator.TradeMovementType(operation.Type),
                        OperationAmountCalculator.CalculateTradeMovementAmount(new[] { operation }),
                        operation.Date,
                        OperationAmountCalculator.BuildTradeMovementDescription(operation.Type, operation.CompanyName, operation.NumberOfShares, operation.Id),
                        operation.Id);
                }

                if (!string.IsNullOrWhiteSpace(operation.CompanyName))
                {
                    await _companyService.RecalculateTotalInvestedAsync(operation.CompanyName);
                }

                await transaction.CommitAsync();
                return operation;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Filas del lote de una compra o dividendo (SPEC-02-06): la fila original y las partidas por una venta
        /// </summary>
        private async Task<List<Operation>> GetTradeFamilyAsync(int key)
        {
            return await _context.Operations
                .Where(o => o.Id == key || o.OriginOperationId == key)
                .ToListAsync();
        }

        /// <summary>
        /// Crea el movimiento bancario de una Compra o Dividendo abierta que no lo tenía (SPEC-02-07, RF-4):
        /// suma del lote convertida a la divisa base. Rechaza operaciones cerradas y divisas sin tipo de cambio.
        /// Debe llamarse dentro de una transacción.
        /// </summary>
        private async Task CreateTradeMovementForFamilyAsync(Operation operation, int key, string accountName)
        {
            if (operation.CloseDate.HasValue)
            {
                throw new InvalidOperationException("Una operación cerrada no se puede registrar en el banco.");
            }

            var family = await GetTradeFamilyAsync(key);
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            if (family.Any(o => o.EffectiveCurrencyCode != baseCurrency && !o.ExchangeRate.HasValue))
            {
                throw new InvalidOperationException(OperationAmountCalculator.MissingTradeRateMessage(baseCurrency));
            }

            await _bankService.AddTradeMovementAsync(
                accountName,
                OperationAmountCalculator.TradeMovementType(operation.Type),
                OperationAmountCalculator.CalculateTradeMovementAmount(family),
                family.Min(o => o.Date),
                OperationAmountCalculator.BuildTradeMovementDescription(operation.Type, operation.CompanyName, family.Sum(o => o.NumberOfShares), key),
                key);
        }

        /// <summary>
        /// Recalcula el movimiento bancario de un lote con las filas que quedan (SPEC-02-06, RF-13): su suma convertida,
        /// la fecha más antigua y el total de acciones. Sin filas, borra el movimiento. Debe llamarse dentro de una transacción.
        /// </summary>
        private async Task SyncTradeMovementAsync(int key, string operationType, string companyName)
        {
            var family = await GetTradeFamilyAsync(key);
            if (family.Count == 0)
            {
                await _bankService.RemoveTradeMovementsAsync(new[] { key });
                return;
            }

            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            if (family.Any(o => o.EffectiveCurrencyCode != baseCurrency && !o.ExchangeRate.HasValue))
            {
                throw new InvalidOperationException(OperationAmountCalculator.MissingTradeRateMessage(baseCurrency));
            }

            await _bankService.UpdateTradeMovementAsync(
                key,
                OperationAmountCalculator.CalculateTradeMovementAmount(family),
                family.Min(o => o.Date),
                OperationAmountCalculator.BuildTradeMovementDescription(operationType, companyName, family.Sum(o => o.NumberOfShares), key));
        }

        /// <summary>
        /// Registra una Venta de acciones cerrando por FIFO las compras abiertas y guardando el histórico
        /// </summary>
        public async Task<SaleRegistrationResult> RegisterSaleAsync(Operation sale, decimal sellPrice, string? bankAccountName = null)
        {
            // 1. Validación
            if (!string.Equals(sale.Type, "Venta", StringComparison.Ordinal))
            {
                return SaleRegistrationResult.Fail("La operación debe ser de tipo Venta.");
            }

            if (string.IsNullOrWhiteSpace(sale.CompanyName))
            {
                return SaleRegistrationResult.Fail("El nombre de la empresa es obligatorio.");
            }

            if (sale.NumberOfShares <= 0)
            {
                return SaleRegistrationResult.Fail("El número de acciones de la venta debe ser mayor que 0.");
            }

            // Acciones fraccionadas con hasta 2 decimales (SPEC-02-08)
            if (!ShareQuantity.HasValidPrecision(sale.NumberOfShares))
            {
                return SaleRegistrationResult.Fail(ShareQuantity.PrecisionMessage);
            }

            if (sale.PricePerShare <= 0)
            {
                return SaleRegistrationResult.Fail("El precio por acción de la venta debe ser mayor que 0.");
            }

            if (sellPrice <= 0)
            {
                return SaleRegistrationResult.Fail("El precio de venta debe ser mayor que 0.");
            }

            var company = await _context.CompanyDetails.FirstOrDefaultAsync(c => c.Name == sale.CompanyName);
            if (company == null)
            {
                return SaleRegistrationResult.Fail($"La empresa «{sale.CompanyName}» no existe.");
            }

            // Ingreso en el banco (SPEC-02-04): cuenta, tipo de cambio y liquidez se validan antes de tocar nada
            BankAccount depositAccount;
            try
            {
                depositAccount = await _bankService.ResolveSaleAccountAsync(bankAccountName);
            }
            catch (InvalidOperationException ex)
            {
                return SaleRegistrationResult.Fail(ex.Message);
            }

            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            var saleCurrency = CurrencyFormatter.Normalize(await GetCompanyCurrencyAsync(sale.CompanyName));
            if (saleCurrency != baseCurrency && (!sale.ExchangeRate.HasValue || sale.ExchangeRate.Value <= 0))
            {
                return SaleRegistrationResult.Fail(MissingRateMessage(baseCurrency));
            }

            if (OperationAmountCalculator.CalculateSaleNet(sellPrice, sale.NumberOfShares, sale.Commission, null) <= 0)
            {
                return SaleRegistrationResult.Fail(OperationAmountCalculator.SaleNetNotPositiveMessage);
            }

            // 2. Transacción (SQLite no admite transacciones anidadas: el recálculo se llama dentro de esta)
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Compras abiertas por FIFO (fecha e Id)
                var openOperations = await _context.Operations
                    .Where(o => o.CompanyName == sale.CompanyName && o.CloseDate == null)
                    .ToListAsync();

                var lots = openOperations
                    .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                    .OrderBy(o => o.Date)
                    .ThenBy(o => o.Id)
                    .ToList();

                // Las ventas antiguas que siguen abiertas ya restan acciones en la empresa
                var availableShares = ComputeAvailableShares(openOperations);

                // 4. Acciones insuficientes: no se cambia nada
                if (availableShares < sale.NumberOfShares)
                {
                    await transaction.RollbackAsync();
                    return SaleRegistrationResult.Fail(
                        $"No hay acciones suficientes: se venden {ShareQuantity.Format(sale.NumberOfShares)} y solo hay {ShareQuantity.Format(Math.Max(availableShares, 0m))} abiertas de «{sale.CompanyName}».");
                }

                // 5. Consumir lotes hasta cubrir las acciones vendidas
                // Las cantidades se redondean a 2 decimales: SQLite puede devolverlas como reales (SPEC-02-08)
                var remaining = ShareQuantity.Round(sale.NumberOfShares);
                decimal totalCost = 0m;
                decimal closedCommissions = 0m;
                var pendingParts = new List<Operation>();
                var closedLots = new List<Operation>();

                foreach (var lot in lots)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    if (lot.NumberOfShares <= remaining)
                    {
                        // Lote completo
                        totalCost += lot.PricePerShare * lot.NumberOfShares;
                        closedCommissions += lot.Commission;
                        remaining = ShareQuantity.Round(remaining - lot.NumberOfShares);
                        lot.CloseDate = sale.Date;
                        closedLots.Add(lot);
                    }
                    else
                    {
                        // Lote parcial: se divide en una fila cerrada y otra abierta duplicada.
                        // La parte pendiente se queda con el resto para no perder céntimos.
                        var closedShares = remaining;
                        var pendingShares = ShareQuantity.Round(lot.NumberOfShares - closedShares);
                        var ratio = closedShares / lot.NumberOfShares;

                        var closedCommission = Math.Round(lot.Commission * ratio, 2);
                        var closedAmount = Math.Round(lot.Amount * ratio, 2);

                        // Las dos filas comparten el origen del lote para poder volver a unirlas al deshacer la venta
                        var origin = lot.OriginOperationId ?? lot.Id;
                        lot.OriginOperationId = origin;

                        pendingParts.Add(new Operation
                        {
                            CompanyName = lot.CompanyName,
                            Type = lot.Type,
                            Date = lot.Date,
                            CloseDate = null,
                            PricePerShare = lot.PricePerShare,
                            NumberOfShares = pendingShares,
                            Commission = lot.Commission - closedCommission,
                            Amount = lot.Amount - closedAmount,
                            OriginOperationId = origin,
                            CurrencyCode = lot.CurrencyCode,
                            ExchangeRate = lot.ExchangeRate,
                            ExchangeRateTo = lot.ExchangeRateTo,
                            CreatedAt = lot.CreatedAt
                        });

                        totalCost += lot.PricePerShare * closedShares;
                        closedCommissions += closedCommission;

                        lot.NumberOfShares = closedShares;
                        lot.Commission = closedCommission;
                        lot.Amount = closedAmount;
                        lot.CloseDate = sale.Date;
                        closedLots.Add(lot);
                        remaining = 0;
                    }
                }

                _context.Operations.AddRange(pendingParts);

                // 6. Precio medio ponderado de compra
                var averageBuyPrice = Math.Round(totalCost / sale.NumberOfShares, 4);

                // 7. La Venta se guarda cerrada
                sale.CloseDate = sale.Date;
                sale.CreatedAt = DateTime.Now;
                sale.CurrencyCode = await GetCompanyCurrencyAsync(sale.CompanyName);
                await ApplyExchangeRateAsync(sale);
                _context.Operations.Add(sale);

                // 8. Importe, comisiones y beneficio (mismo cálculo que al editar el precio de venta)
                var roundedSellPrice = Math.Round(sellPrice, 2);
                var amount = Math.Round(roundedSellPrice * sale.NumberOfShares, 2);
                var commission = Math.Round(sale.Commission + closedCommissions, 2);
                var profit = Math.Round(amount - (averageBuyPrice * sale.NumberOfShares) - commission, 2);

                var record = new SaleHistory
                {
                    CompanyName = sale.CompanyName,
                    Ticker = company.Ticker ?? string.Empty,
                    SaleDate = sale.Date,
                    NumberOfShares = sale.NumberOfShares,
                    AverageBuyPrice = averageBuyPrice,
                    SellPrice = roundedSellPrice,
                    Amount = amount,
                    Commission = commission,
                    Profit = profit,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Primero se guarda la Venta para conocer su Id y enlazarla en el histórico
                await _context.SaveChangesAsync();
                record.OperationId = sale.Id;
                _context.SalesHistory.Add(record);
                await _context.SaveChangesAsync();

                // Cada compra cerrada guarda el Id del registro que la cerró (para poder deshacer la venta)
                foreach (var closedLot in closedLots)
                {
                    closedLot.SaleHistoryId = record.Id;
                }
                await _context.SaveChangesAsync();

                // La liquidez de la venta entra en la cuenta bancaria (en divisa base, con el tipo ya normalizado)
                var deposit = OperationAmountCalculator.CalculateSaleNet(roundedSellPrice, sale.NumberOfShares, sale.Commission, sale.ExchangeRate);
                await _bankService.AddSaleDepositAsync(
                    depositAccount.Name,
                    deposit,
                    sale.Date,
                    OperationAmountCalculator.BuildSaleDepositDescription(record, sale.CurrencyCode),
                    record.Id);

                // 9. Recalcular la empresa dentro de la transacción y confirmar
                await _companyService.RecalculateTotalInvestedAsync(sale.CompanyName);
                await transaction.CommitAsync();

                return new SaleRegistrationResult { Success = true, SaleHistoryId = record.Id };
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return SaleRegistrationResult.Fail(ex.Message);
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Divisa de la venta, divisa base y operación Venta de un registro del histórico (SPEC-02-04)
        /// </summary>
        private async Task<(string Currency, string BaseCurrency, Operation? Sale)> GetSaleDepositContextAsync(SaleHistory record)
        {
            var saleOperation = await _context.Operations
                .FirstOrDefaultAsync(o => o.Id == record.OperationId && o.Type == "Venta");
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            var currency = saleOperation != null
                ? saleOperation.EffectiveCurrencyCode
                : CurrencyFormatter.Normalize(await GetCompanyCurrencyAsync(record.CompanyName));
            return (currency, baseCurrency, saleOperation);
        }

        public async Task<SaleDepositInfo?> GetSaleDepositInfoAsync(int saleHistoryId)
        {
            var record = await _context.SalesHistory.AsNoTracking().FirstOrDefaultAsync(s => s.Id == saleHistoryId);
            if (record == null)
            {
                return null;
            }

            var (currency, baseCurrency, sale) = await GetSaleDepositContextAsync(record);
            return new SaleDepositInfo
            {
                CurrencyCode = currency,
                BaseCurrencyCode = baseCurrency,
                RequiresExchangeRate = currency != baseCurrency && !(sale?.ExchangeRate > 0)
            };
        }

        public async Task<SaleRegistrationResult> DepositExistingSaleAsync(int saleHistoryId, string? bankAccountName, decimal? exchangeRate)
        {
            var record = await _context.SalesHistory.FirstOrDefaultAsync(s => s.Id == saleHistoryId);
            if (record == null)
            {
                return SaleRegistrationResult.Fail("El registro ya no existe.");
            }

            // Una venta solo se ingresa una vez
            var existing = await _bankService.GetSaleDepositsAsync(new[] { saleHistoryId });
            if (existing.TryGetValue(saleHistoryId, out var depositedIn))
            {
                return SaleRegistrationResult.Fail($"Esta venta ya está ingresada en {depositedIn.Trim()}.");
            }

            BankAccount account;
            try
            {
                account = await _bankService.ResolveSaleAccountAsync(bankAccountName);
            }
            catch (InvalidOperationException ex)
            {
                return SaleRegistrationResult.Fail(ex.Message);
            }

            var (currency, baseCurrency, sale) = await GetSaleDepositContextAsync(record);

            // Tipo de cambio: el guardado en la Venta o, si no tiene, el indicado por el usuario
            decimal? rate = null;
            var saveRateInSale = false;
            if (currency != baseCurrency)
            {
                if (sale?.ExchangeRate > 0)
                {
                    rate = sale.ExchangeRate;
                }
                else if (exchangeRate > 0)
                {
                    rate = Math.Round(exchangeRate.Value, 6);
                    saveRateInSale = sale != null;
                }
                else
                {
                    return SaleRegistrationResult.Fail(MissingRateMessage(baseCurrency));
                }
            }

            var commission = sale?.Commission ?? 0m;
            if (OperationAmountCalculator.CalculateSaleNet(record.SellPrice, record.NumberOfShares, commission, null) <= 0)
            {
                return SaleRegistrationResult.Fail(OperationAmountCalculator.SaleNetNotPositiveMessage);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (saveRateInSale)
                {
                    sale!.ExchangeRate = rate;
                    await ApplyExchangeRateAsync(sale);
                    await _context.SaveChangesAsync();
                }

                var deposit = OperationAmountCalculator.CalculateSaleNet(record.SellPrice, record.NumberOfShares, commission, rate);
                await _bankService.AddSaleDepositAsync(
                    account.Name,
                    deposit,
                    record.SaleDate,
                    OperationAmountCalculator.BuildSaleDepositDescription(record, currency),
                    record.Id);

                await transaction.CommitAsync();
                return new SaleRegistrationResult { Success = true, SaleHistoryId = record.Id };
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return SaleRegistrationResult.Fail(ex.Message);
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Deshace una venta: reabre las compras que cerró, une los lotes partidos y elimina la Venta y su registro
        /// </summary>
        public async Task<SaleUndoResult> UndoSaleAsync(int saleHistoryId)
        {
            var record = await _context.SalesHistory.FirstOrDefaultAsync(s => s.Id == saleHistoryId);
            if (record == null)
            {
                return SaleUndoResult.Fail("El registro ya no existe.");
            }

            // SQLite no admite transacciones anidadas: el recálculo se llama dentro de esta
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Reabrir las compras cerradas por esta venta
                var linked = await _context.Operations
                    .Where(o => o.SaleHistoryId == saleHistoryId)
                    .ToListAsync();

                var reopenedIds = linked.Select(o => o.Id).ToHashSet();
                var companies = new HashSet<string>(StringComparer.Ordinal);

                foreach (var operation in linked)
                {
                    operation.SaleHistoryId = null;
                    operation.CloseDate = null;
                    companies.Add(operation.CompanyName);
                }

                await _context.SaveChangesAsync();

                // 2. Volver a unir los lotes partidos: filas abiertas del mismo origen, empresa, tipo, fecha y precio
                var mergedCount = 0;
                var origins = linked
                    .Where(o => o.OriginOperationId.HasValue)
                    .Select(o => o.OriginOperationId!.Value)
                    .Distinct()
                    .ToList();

                foreach (var origin in origins)
                {
                    var family = await _context.Operations
                        .Where(o => o.OriginOperationId == origin && o.CloseDate == null
                                    && (o.Type == "Compra" || o.Type == "Traspaso"))
                        .ToListAsync();

                    var groups = family
                        .GroupBy(o => new { o.CompanyName, o.Type, o.Date, o.PricePerShare })
                        .Where(g => g.Count() > 1 && g.Any(o => reopenedIds.Contains(o.Id)))
                        .ToList();

                    foreach (var group in groups)
                    {
                        var ordered = group.OrderBy(o => o.Id).ToList();
                        var survivor = ordered[0];

                        foreach (var other in ordered.Skip(1))
                        {
                            // Importe y comisión ya redondeados: la suma devuelve exactamente los del original
                            survivor.NumberOfShares = ShareQuantity.Round(survivor.NumberOfShares + other.NumberOfShares);
                            survivor.Amount += other.Amount;
                            survivor.Commission += other.Commission;
                            _context.Operations.Remove(other);
                            mergedCount++;
                        }
                    }
                }

                // 3. Eliminar la operación Venta (si ya no existe se ignora) y el registro del histórico
                var saleOperation = await _context.Operations
                    .FirstOrDefaultAsync(o => o.Id == record.OperationId && o.Type == "Venta");
                if (saleOperation != null)
                {
                    companies.Add(saleOperation.CompanyName);
                    _context.Operations.Remove(saleOperation);
                }

                companies.Add(record.CompanyName);
                _context.SalesHistory.Remove(record);
                await _context.SaveChangesAsync();

                // 4. Recalcular cada empresa implicada (sin repetir) dentro de la transacción y confirmar
                foreach (var company in companies)
                {
                    if (!string.IsNullOrWhiteSpace(company))
                    {
                        await _companyService.RecalculateTotalInvestedAsync(company);
                    }
                }

                // El ingreso bancario de la venta desaparece con ella (SPEC-02-04)
                await _bankService.RemoveSaleDepositsAsync(saleHistoryId);

                await transaction.CommitAsync();

                return new SaleUndoResult
                {
                    Success = true,
                    ReopenedCount = linked.Count,
                    MergedCount = mergedCount
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Repara compras y traspasos que siguen cerrados por una venta que ya no existe en el histórico
        /// (p. ej. una venta deshecha cuyas compras no se reabrieron): los reabre, limpia el enlace y
        /// recalcula sus empresas, igual que haría «Deshacer venta». Es idempotente.
        /// </summary>
        /// <returns>Número de operaciones reabiertas</returns>
        public async Task<int> RepairOrphanSaleLinksAsync()
        {
            var existingRecordIds = await _context.SalesHistory
                .Select(h => h.Id)
                .ToListAsync();

            var orphans = await _context.Operations
                .Where(o => o.SaleHistoryId != null && !existingRecordIds.Contains(o.SaleHistoryId.Value))
                .ToListAsync();

            if (orphans.Count == 0)
            {
                return 0;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companies = new HashSet<string>(StringComparer.Ordinal);
                foreach (var operation in orphans)
                {
                    operation.SaleHistoryId = null;
                    operation.CloseDate = null;
                    companies.Add(operation.CompanyName);
                }

                await _context.SaveChangesAsync();

                foreach (var company in companies)
                {
                    await _companyService.RecalculateTotalInvestedAsync(company);
                }

                await transaction.CommitAsync();
                return orphans.Count;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Aviso al editar una operación ligada a una venta del histórico (se puede editar, pero el
        /// beneficio guardado no cambia y, si se reabre una compra, se desvincula de la venta)
        /// </summary>
        public async Task<string?> GetSaleEditWarningAsync(int operationId)
        {
            var operation = await _context.Operations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == operationId);
            if (operation == null)
            {
                return null;
            }

            var recordId = await GetLinkedSaleRecordIdAsync(operation);
            if (!recordId.HasValue)
            {
                return null;
            }

            return operation.Type == "Venta"
                ? $"Esta venta está en el histórico (nº {recordId.Value}). Si cambias acciones o precio, no cambian el beneficio guardado ni el ingreso en la cuenta bancaria (el precio se cambia desde «Histórico de ventas»); si la reabres, volverá a restar acciones como una venta antigua."
                : $"Esta operación la cerró la venta nº {recordId.Value} del histórico. El beneficio guardado de esa venta no cambia; si la reabres, se desvincula de la venta y ya no se reabrirá al deshacerla.";
        }

        /// <summary>
        /// Id del registro del histórico de ventas ligado a la operación: el que cerró una compra o traspaso,
        /// o el de la propia Venta. Null si no está ligada.
        /// </summary>
        private async Task<int?> GetLinkedSaleRecordIdAsync(Operation operation)
        {
            if (operation.Type == "Compra" || operation.Type == "Traspaso")
            {
                return operation.SaleHistoryId;
            }

            if (operation.Type == "Venta")
            {
                return await _context.SalesHistory
                    .Where(h => h.OperationId == operation.Id)
                    .Select(h => (int?)h.Id)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        /// <summary>
        /// Devuelve el mensaje de bloqueo si la operación está cerrada por una venta del histórico.
        /// Solo se aplica al eliminar; editar está permitido con aviso (<see cref="GetSaleEditWarningAsync"/>).
        /// </summary>
        public async Task<string?> GetSaleLockMessageAsync(int operationId)
        {
            var operation = await _context.Operations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == operationId);
            if (operation == null)
            {
                return null;
            }

            int? recordId = null;
            if (operation.Type == "Compra" || operation.Type == "Traspaso")
            {
                recordId = operation.SaleHistoryId;
            }
            else if (operation.Type == "Venta")
            {
                recordId = await _context.SalesHistory
                    .Where(h => h.OperationId == operationId)
                    .Select(h => (int?)h.Id)
                    .FirstOrDefaultAsync();
            }

            return recordId.HasValue
                ? $"Esta operación está cerrada por la venta nº {recordId.Value} del histórico. Para eliminarla, ve a «Histórico de ventas» y deshaz la venta (editarla sí se puede)."
                : null;
        }

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        public async Task<bool> UpdateAsync(Operation operation, string? bankAccountName = null)
        {
            // Las operaciones ligadas a una venta del histórico se pueden editar (con aviso en pantalla)
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.Operations.FindAsync(operation.Id);
                if (existing == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Movimiento bancario de una Compra o Dividendo (SPEC-02-06): clave del lote antes de editar
                var movementKey = OperationAmountCalculator.GetTradeMovementKey(existing);
                var previousType = existing.Type;
                string? currentAccount = null;
                if (OperationAmountCalculator.GeneratesBankMovement(existing.Type))
                {
                    (await _bankService.GetTradeMovementAccountsAsync(new[] { movementKey })).TryGetValue(movementKey, out currentAccount);
                }

                var hasMovement = currentAccount != null;
                if (hasMovement && !string.Equals(previousType, operation.Type, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Esta operación tiene un movimiento bancario: bórrala y créala de nuevo.");
                }

                // Una operación con movimiento no puede quedarse sin él (SPEC-02-07, RF-5): solo cambiar de cuenta
                if (hasMovement && bankAccountName == OperationAmountCalculator.NoBankMovementValue)
                {
                    throw new InvalidOperationException("Esta operación tiene un movimiento bancario: elige otra cuenta o bórrala.");
                }

                var previousCompanyName = existing.CompanyName;

                existing.CompanyName = operation.CompanyName;
                existing.Type = operation.Type;
                existing.Date = operation.Date;
                existing.CloseDate = operation.CloseDate;

                // Una compra o traspaso cerrado por una venta que se reabre deja de pertenecer a esa venta:
                // así «Deshacer venta» no la vuelve a tocar
                if (!operation.CloseDate.HasValue && existing.SaleHistoryId.HasValue)
                {
                    existing.SaleHistoryId = null;
                }
                existing.Amount = operation.Amount;
                existing.PricePerShare = operation.PricePerShare;
                existing.NumberOfShares = operation.NumberOfShares;
                existing.Commission = operation.Commission;

                // La divisa es de solo lectura: solo cambia si la operación pasa a otra empresa
                if (!string.Equals(previousCompanyName, operation.CompanyName, StringComparison.Ordinal))
                {
                    existing.CurrencyCode = await GetCompanyCurrencyAsync(operation.CompanyName);
                }

                // El tipo de cambio lo manda la pantalla (conserva el guardado o precarga el actual)
                existing.ExchangeRate = operation.ExchangeRate;
                existing.ExchangeRateTo = operation.ExchangeRateTo;
                await ApplyExchangeRateAsync(existing);

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(previousCompanyName))
                {
                    await _companyService.RecalculateTotalInvestedAsync(previousCompanyName);
                }

                if (!string.IsNullOrWhiteSpace(operation.CompanyName) && operation.CompanyName != previousCompanyName)
                {
                    await _companyService.RecalculateTotalInvestedAsync(operation.CompanyName);
                }

                // El movimiento bancario sigue a la operación (SPEC-02-06): suma del lote, fecha más antigua y acciones
                if (hasMovement)
                {
                    await SyncTradeMovementAsync(movementKey, existing.Type, existing.CompanyName);
                }

                // Cuenta elegida al editar (SPEC-02-07): pasar el movimiento a otra cuenta o crearlo en una operación abierta sin él
                if (OperationAmountCalculator.GeneratesBankMovement(existing.Type)
                    && !string.IsNullOrWhiteSpace(bankAccountName)
                    && bankAccountName != OperationAmountCalculator.NoBankMovementValue)
                {
                    var account = await _bankService.ResolveTradeAccountAsync(bankAccountName);
                    if (account != null)
                    {
                        if (hasMovement)
                        {
                            if (!string.Equals(account.Name, currentAccount, StringComparison.Ordinal))
                            {
                                await _bankService.MoveTradeMovementAsync(movementKey, account.Name);
                            }
                        }
                        else
                        {
                            await CreateTradeMovementForFamilyAsync(existing, movementKey, account.Name);
                        }
                    }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                if (!await _context.Operations.AnyAsync(o => o.Id == operation.Id))
                {
                    return false;
                }
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Elimina una operación de la base de datos
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var lockMessage = await GetSaleLockMessageAsync(id);
            if (lockMessage != null)
            {
                throw new InvalidOperationException(lockMessage);
            }

            var operation = await _context.Operations.FindAsync(id);
            if (operation == null)
            {
                return false;
            }

            var companyName = operation.CompanyName;
            var operationType = operation.Type;
            var movementKey = OperationAmountCalculator.GetTradeMovementKey(operation);
            var hasMovement = OperationAmountCalculator.GeneratesBankMovement(operationType)
                && (await _bankService.GetTradeMovementAccountsAsync(new[] { movementKey })).ContainsKey(movementKey);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Operations.Remove(operation);

                // La línea de dividendo que generó esta operación vuelve a Pendiente (SPEC-21-01, RF-7)
                var dividendLines = await _context.DividendPayments
                    .Where(d => d.OperationId == id)
                    .ToListAsync();
                foreach (var line in dividendLines)
                {
                    line.Status = DividendPaymentRules.StatusPending;
                    line.OperationId = null;
                    line.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(companyName))
                {
                    await _companyService.RecalculateTotalInvestedAsync(companyName);
                }

                // El movimiento bancario baja con la operación; en un lote partido queda con las filas restantes (SPEC-02-06, RF-13)
                if (hasMovement)
                {
                    await SyncTradeMovementAsync(movementKey, operationType, companyName);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        /// <summary>
        /// Acciones disponibles para vender a partir de las operaciones abiertas de una empresa:
        /// lotes de Compra y Traspaso menos las Ventas antiguas que siguen abiertas. Sin redondear a 0.
        /// Es la misma cuenta que usa <see cref="RegisterSaleAsync"/>.
        /// </summary>
        private static decimal ComputeAvailableShares(IEnumerable<Operation> openOperations)
        {
            var list = openOperations.ToList();
            var lotShares = list
                .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                .Sum(o => o.NumberOfShares);
            var legacyOpenSoldShares = list
                .Where(o => o.Type == "Venta")
                .Sum(o => o.NumberOfShares);
            return ShareQuantity.Round(lotShares - legacyOpenSoldShares);
        }

        /// <summary>
        /// Devuelve las acciones disponibles para vender de una empresa (mínimo 0)
        /// </summary>
        public async Task<decimal> GetAvailableSharesAsync(string companyName)
        {
            var openOperations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.CompanyName == companyName && o.CloseDate == null)
                .ToListAsync();

            return Math.Max(ComputeAvailableShares(openOperations), 0m);
        }

        /// <summary>
        /// Motivo por el que no se puede registrar una operación rápida desde el gráfico, o null si se puede
        /// (SPEC-05-01, RF-6). Con fecha de hoy y mercado Cerrado se bloquea; con fecha anterior no se exige
        /// mercado abierto; con mercado Desconocido se permite.
        /// </summary>
        public string? GetQuickTradeBlockReason(string type, DateTime date, MarketState marketState, decimal availableShares, bool hasBankAccounts)
        {
            var today = DateTime.Today;

            if (date.Date > today)
            {
                return "La fecha no puede ser futura.";
            }

            if (date.Date == today && marketState == MarketState.Closed)
            {
                return "Mercado cerrado: no se puede operar hoy a precio en vivo.";
            }

            if (type == "Venta" && availableShares <= 0)
            {
                return "No tienes acciones de esta empresa.";
            }

            if (type == "Venta" && !hasBankAccounts)
            {
                return BankService.NoBankAccountsMessage;
            }

            return null;
        }

        /// <summary>
        /// Registra una Compra o una Venta desde el gráfico de cotización (SPEC-05-01): revalida el bloqueo,
        /// calcula y valida el importe con <see cref="OperationAmountCalculator"/> y la guarda con
        /// <see cref="AddAsync"/> (Compra) o <see cref="RegisterSaleAsync"/> (Venta).
        /// </summary>
        public async Task<QuickTradeResult> RegisterQuickTradeAsync(QuickTradeRequest request)
        {
            if (request.Type != "Compra" && request.Type != "Venta")
            {
                return QuickTradeResult.Fail("El tipo de operación debe ser Compra o Venta.");
            }

            var company = await _context.CompanyDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CompanyId);
            if (company == null)
            {
                return QuickTradeResult.Fail("La empresa no existe.");
            }

            var availableShares = await GetAvailableSharesAsync(company.Name);
            var hasBankAccounts = await _bankService.GetDefaultBankAccountAsync() != null;
            var blockReason = GetQuickTradeBlockReason(request.Type, request.Date, request.MarketState, availableShares, hasBankAccounts);
            if (blockReason != null)
            {
                return QuickTradeResult.Fail(blockReason);
            }

            if (request.Commission < 0)
            {
                return QuickTradeResult.Fail("La comisión debe ser mayor o igual que 0.");
            }

            if (request.ExchangeRate.HasValue && request.ExchangeRate.Value <= 0)
            {
                return QuickTradeResult.Fail("El tipo de cambio debe ser mayor que 0 o quedar vacío.");
            }

            // El precio ya viene resuelto desde el gráfico: no se usa el precio en vivo como respaldo
            var amount = OperationAmountCalculator.Calculate(
                request.PricePerShare, null, request.NumberOfShares, request.Commission) ?? 0m;
            var validationError = OperationAmountCalculator.Validate(
                request.Type, request.PricePerShare, null, request.NumberOfShares, amount);
            if (validationError != null)
            {
                return QuickTradeResult.Fail(validationError);
            }

            // Hoy se guarda con la hora actual; una fecha anterior, sin hora
            var date = request.Date.Date == DateTime.Today ? DateTime.Now : request.Date.Date;

            var operation = new Operation
            {
                CompanyName = company.Name,
                Type = request.Type,
                Date = date,
                CloseDate = null,
                PricePerShare = Math.Round(request.PricePerShare, 2),
                NumberOfShares = request.NumberOfShares,
                Commission = Math.Round(request.Commission, 2),
                Amount = amount,
                ExchangeRate = request.ExchangeRate,
                ExchangeRateTo = null
            };

            if (request.Type == "Compra")
            {
                try
                {
                    await AddAsync(operation, request.BankAccountName);
                }
                catch (InvalidOperationException ex)
                {
                    return QuickTradeResult.Fail(ex.Message);
                }

                return new QuickTradeResult { Success = true, Operation = operation };
            }

            var saleResult = await RegisterSaleAsync(operation, operation.PricePerShare, request.BankAccountName);
            return saleResult.Success
                ? new QuickTradeResult { Success = true, Operation = operation }
                : QuickTradeResult.Fail(saleResult.ErrorMessage);
        }

        /// <summary>
        /// Marcadores del gráfico de cotización (SPEC-05-01): compras y traspasos agrupados por lote
        /// (las filas partidas por una venta se suman en un único marcador), ventas y dividendos.
        /// Las operaciones con precio 0 no se marcan, salvo los dividendos.
        /// </summary>
        public async Task<List<ChartMarker>> GetChartMarkersAsync(string companyName)
        {
            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.CompanyName == companyName)
                .ToListAsync();

            var markers = new List<ChartMarker>();

            // Compras y traspasos: una entrada por lote original
            markers.AddRange(operations
                .Where(o => (o.Type == "Compra" || o.Type == "Traspaso") && o.PricePerShare > 0)
                .GroupBy(o => o.OriginOperationId ?? o.Id)
                .Select(g =>
                {
                    var first = g.OrderBy(o => o.Id).First();
                    return new ChartMarker
                    {
                        Kind = first.Type == "Compra" ? ChartMarkerKind.Buy : ChartMarkerKind.Transfer,
                        Date = first.Date,
                        Price = first.PricePerShare,
                        Shares = g.Sum(o => o.NumberOfShares),
                        Amount = g.Sum(o => o.Amount)
                    };
                }));

            markers.AddRange(operations
                .Where(o => o.Type == "Venta" && o.PricePerShare > 0)
                .Select(o => new ChartMarker
                {
                    Kind = ChartMarkerKind.Sell,
                    Date = o.Date,
                    Price = o.PricePerShare,
                    Shares = o.NumberOfShares,
                    Amount = o.Amount
                }));

            markers.AddRange(operations
                .Where(o => o.Type == "Dividendo")
                .Select(o => new ChartMarker
                {
                    Kind = ChartMarkerKind.Dividend,
                    Date = o.Date,
                    Price = null,
                    Shares = o.NumberOfShares,
                    Amount = o.Amount
                }));

            return markers.OrderBy(m => m.Date).ToList();
        }

        /// <summary>
        /// Calcula el total invertido en operaciones abiertas
        /// </summary>
        public async Task<decimal> GetTotalOpenAmountAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate == null)
                .SumAsync(o => o.Amount);
        }

        /// <summary>
        /// Obtiene un diccionario con el conteo de operaciones agrupadas por tipo
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationCountByTypeAsync()
        {
            return await _context.Operations
                .GroupBy(o => o.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }
    }
}
