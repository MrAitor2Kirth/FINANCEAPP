using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="ICountryService"/> sobre la tabla Countries
    /// </summary>
    public class CountryService : ICountryService
    {
        private readonly AppDbContext _context;
        private readonly IAppSettingsService _settings;

        public CountryService(AppDbContext context, IAppSettingsService settings)
        {
            _context = context;
            _settings = settings;
        }

        public async Task<List<Country>> GetAllAsync()
        {
            return await _context.Countries
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Country?> GetByCodeAsync(string code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == normalized);
        }

        public async Task<CountryResult> CreateAsync(Country country)
        {
            var error = Validate(country, requireCode: true);
            if (error != null)
            {
                return CountryResult.Fail(error);
            }

            var code = country.Code.Trim().ToUpperInvariant();
            var name = country.Name.Trim();

            try
            {
                if (await _context.Countries.AnyAsync(c => c.Code == code))
                {
                    return CountryResult.Fail("Ya existe un país con ese código.");
                }

                if (await NameExistsAsync(name, null))
                {
                    return CountryResult.Fail("Ya existe un país con ese nombre.");
                }

                _context.Countries.Add(new Country
                {
                    Code = code,
                    Name = name,
                    Region = country.Region.Trim().ToUpperInvariant(),
                    CurrencyCode = country.CurrencyCode.Trim().ToUpperInvariant(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return CountryResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear el país: {ex.Message}");
                _context.ChangeTracker.Clear();
                return CountryResult.Fail("No se pudo guardar el país. No se ha modificado ningún dato.");
            }
        }

        public async Task<CountryResult> UpdateAsync(Country country)
        {
            var error = Validate(country, requireCode: false);
            if (error != null)
            {
                return CountryResult.Fail(error);
            }

            var code = country.Code.Trim().ToUpperInvariant();
            var name = country.Name.Trim();
            var currency = country.CurrencyCode.Trim().ToUpperInvariant();

            try
            {
                var existing = await _context.Countries.FirstOrDefaultAsync(c => c.Code == code);
                if (existing == null)
                {
                    return CountryResult.Fail("El país ya no existe.");
                }

                if (await NameExistsAsync(name, code))
                {
                    return CountryResult.Fail("Ya existe un país con ese nombre.");
                }

                if (!string.Equals(existing.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
                {
                    var inUse = await CountCompaniesAsync(code);
                    if (inUse > 0)
                    {
                        return CountryResult.Fail(
                            $"No se puede cambiar la divisa: {Companies(inUse)} usan este país y sus operaciones están en {existing.CurrencyCode}.");
                    }
                }

                existing.Name = name;
                existing.Region = country.Region.Trim().ToUpperInvariant();
                existing.CurrencyCode = currency;
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return CountryResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar el país: {ex.Message}");
                _context.ChangeTracker.Clear();
                return CountryResult.Fail("No se pudo guardar el país. No se ha modificado ningún dato.");
            }
        }

        public async Task<CountryResult> DeleteAsync(string code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

            try
            {
                var existing = await _context.Countries.FirstOrDefaultAsync(c => c.Code == normalized);
                if (existing == null)
                {
                    return CountryResult.Fail("El país ya no existe.");
                }

                var baseCode = await _settings.GetBaseCountryCodeAsync();
                if (string.Equals(baseCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return CountryResult.Fail("No se puede eliminar el país base. Elige otro país base en Configuración y vuelve a intentarlo.");
                }

                var inUse = await CountCompaniesAsync(normalized);
                if (inUse > 0)
                {
                    return CountryResult.Fail($"No se puede eliminar: {Companies(inUse)} usan este país.");
                }

                _context.Countries.Remove(existing);
                await _context.SaveChangesAsync();
                return CountryResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar el país: {ex.Message}");
                _context.ChangeTracker.Clear();
                return CountryResult.Fail("No se pudo eliminar el país. No se ha modificado ningún dato.");
            }
        }

        public async Task<int> CountCompaniesAsync(string code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return await _context.CompanyDetails.CountAsync(c => c.CountryCode == normalized);
        }

        public async Task<BaseCurrencyInfo> GetBaseCurrencyAsync()
        {
            var baseCode = await _settings.GetBaseCountryCodeAsync();
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == baseCode);

            return new BaseCurrencyInfo
            {
                CountryCode = baseCode,
                CountryName = country?.Name,
                CurrencyCode = country != null
                    ? CurrencyFormatter.Normalize(country.CurrencyCode)
                    : CurrencyFormatter.DefaultCurrency,
                IsFallback = country == null
            };
        }

        private async Task<bool> NameExistsAsync(string name, string? excludeCode)
        {
            var lower = name.ToLowerInvariant();
            var names = await _context.Countries
                .AsNoTracking()
                .Where(c => excludeCode == null || c.Code != excludeCode)
                .Select(c => c.Name)
                .ToListAsync();
            return names.Any(n => string.Equals(n, lower, StringComparison.OrdinalIgnoreCase));
        }

        private static string Companies(int count) =>
            count == 1 ? "1 empresa" : $"{count} empresas";

        private static string? Validate(Country country, bool requireCode)
        {
            if (country == null)
            {
                return "Faltan los datos del país.";
            }

            var code = (country.Code ?? string.Empty).Trim();
            if (code.Length < 2 || code.Length > 3 || !code.All(char.IsLetter))
            {
                return "El código debe tener 2 o 3 letras.";
            }

            var name = (country.Name ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                return "El nombre es obligatorio.";
            }

            if (name.Length > 100)
            {
                return "El nombre no puede exceder 100 caracteres.";
            }

            var region = (country.Region ?? string.Empty).Trim().ToUpperInvariant();
            if (!Country.Regions.Contains(region))
            {
                return "Elige una región.";
            }

            var currency = (country.CurrencyCode ?? string.Empty).Trim();
            if (currency.Length != 3 || !currency.All(char.IsLetter))
            {
                return "La divisa debe tener 3 letras (por ejemplo EUR o USD).";
            }

            return null;
        }
    }
}
