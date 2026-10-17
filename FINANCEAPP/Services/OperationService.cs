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

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        /// <param name="companyService">Servicio de empresas para recalcular importes</param>
        public OperationService(AppDbContext context, ICompanyService companyService, ICountryService countryService)
        {
            _context = context;
            _companyService = companyService;
            _countryService = countryService;
        }

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
        public async Task<Operation> AddAsync(Operation operation)
        {
            operation.CreatedAt = DateTime.Now;
            operation.CurrencyCode = await GetCompanyCurrencyAsync(operation.CompanyName);
            await ApplyExchangeRateAsync(operation);
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(operation.CompanyName))
            {
                await _companyService.RecalculateTotalInvestedAsync(operation.CompanyName);
            }

            return operation;
        }

        /// <summary>
        /// Registra una Venta de acciones cerrando por FIFO las compras abiertas y guardando el histórico
        /// </summary>
        public async Task<SaleRegistrationResult> RegisterSaleAsync(Operation sale, decimal sellPrice)
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
                        $"No hay acciones suficientes: se venden {sale.NumberOfShares} y solo hay {Math.Max(availableShares, 0)} abiertas de «{sale.CompanyName}».");
                }

                // 5. Consumir lotes hasta cubrir las acciones vendidas
                var remaining = sale.NumberOfShares;
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
                        remaining -= lot.NumberOfShares;
                        lot.CloseDate = sale.Date;
                        closedLots.Add(lot);
                    }
                    else
                    {
                        // Lote parcial: se divide en una fila cerrada y otra abierta duplicada.
                        // La parte pendiente se queda con el resto para no perder céntimos.
                        var closedShares = remaining;
                        var pendingShares = lot.NumberOfShares - closedShares;
                        var ratio = (decimal)closedShares / lot.NumberOfShares;

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

                // 9. Recalcular la empresa dentro de la transacción y confirmar
                await _companyService.RecalculateTotalInvestedAsync(sale.CompanyName);
                await transaction.CommitAsync();

                return new SaleRegistrationResult { Success = true, SaleHistoryId = record.Id };
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
                            survivor.NumberOfShares += other.NumberOfShares;
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
                ? $"Esta venta está en el histórico (nº {recordId.Value}). Si cambias acciones o precio, el beneficio guardado no cambia; si la reabres, volverá a restar acciones como una venta antigua."
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
        public async Task<bool> UpdateAsync(Operation operation)
        {
            // Las operaciones ligadas a una venta del histórico se pueden editar (con aviso en pantalla)
            try
            {
                var existing = await _context.Operations.FindAsync(operation.Id);
                if (existing == null)
                {
                    return false;
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

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Operations.AnyAsync(o => o.Id == operation.Id))
                {
                    return false;
                }
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
            _context.Operations.Remove(operation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                await _companyService.RecalculateTotalInvestedAsync(companyName);
            }

            return true;
        }

        /// <summary>
        /// Acciones disponibles para vender a partir de las operaciones abiertas de una empresa:
        /// lotes de Compra y Traspaso menos las Ventas antiguas que siguen abiertas. Sin redondear a 0.
        /// Es la misma cuenta que usa <see cref="RegisterSaleAsync"/>.
        /// </summary>
        private static int ComputeAvailableShares(IEnumerable<Operation> openOperations)
        {
            var list = openOperations.ToList();
            var lotShares = list
                .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                .Sum(o => o.NumberOfShares);
            var legacyOpenSoldShares = list
                .Where(o => o.Type == "Venta")
                .Sum(o => o.NumberOfShares);
            return lotShares - legacyOpenSoldShares;
        }

        /// <summary>
        /// Devuelve las acciones disponibles para vender de una empresa (mínimo 0)
        /// </summary>
        public async Task<int> GetAvailableSharesAsync(string companyName)
        {
            var openOperations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.CompanyName == companyName && o.CloseDate == null)
                .ToListAsync();

            return Math.Max(ComputeAvailableShares(openOperations), 0);
        }

        /// <summary>
        /// Motivo por el que no se puede registrar una operación rápida desde el gráfico, o null si se puede
        /// (SPEC-05-01, RF-6). Con fecha de hoy y mercado Cerrado se bloquea; con fecha anterior no se exige
        /// mercado abierto; con mercado Desconocido se permite.
        /// </summary>
        public string? GetQuickTradeBlockReason(string type, DateTime date, MarketState marketState, int availableShares)
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
            var blockReason = GetQuickTradeBlockReason(request.Type, request.Date, request.MarketState, availableShares);
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
                await AddAsync(operation);
                return new QuickTradeResult { Success = true, Operation = operation };
            }

            var saleResult = await RegisterSaleAsync(operation, operation.PricePerShare);
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
