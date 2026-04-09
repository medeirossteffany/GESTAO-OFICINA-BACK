using GestaoOficina.Data;
using GestaoOficina.DTOs.Customers;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using GestaoOficina.Features.Tenants;

namespace GestaoOficina.Features.Customers
{
    public class CustomerService
    {
        private readonly AppDbContext _context;
        private readonly TenantPlanValidator _planValidator;

        public CustomerService(AppDbContext context, TenantPlanValidator planValidator)
        {
            _context = context;
            _planValidator = planValidator;
        }

        public async Task<List<Customer>> GetCustomersByTenantAndUnits(
            int tenantId,
            List<int> unitIds,
            bool fullAccess,
            int? selectedUnitId = null)
        {
            var query = _context.Customers
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .Where(c => c.CustomerUnits.Any(cu => cu.IsActive))
                .Where(c => fullAccess || c.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId)))
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .AsQueryable();

            if (selectedUnitId.HasValue)
            {
                query = query.Where(c => c.CustomerUnits.Any(cu => cu.IsActive && cu.UnitId == selectedUnitId.Value));
            }

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerById(int id)
        {
            return await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive && c.CustomerUnits.Any(cu => cu.IsActive));
        }

        public async Task<Customer?> GetCustomerByCpfCnpj(int tenantId, string cpfCnpj)
        {
            var rawDocument = cpfCnpj.Trim();
            var normalizedDocument = new string(rawDocument.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(rawDocument))
                return null;

            return await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c =>
                    c.TenantId == tenantId &&
                    c.CpfCnpj != null &&
                    (
                        c.CpfCnpj == rawDocument ||
                        c.CpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "") == normalizedDocument
                    ));
        }

        public async Task<(Customer Customer, bool CreatedNew)> CreateOrLinkCustomer(
            CreateCustomerRequest dto,
            int tenantId,
            List<int> targetUnitIds)
        {
            targetUnitIds = targetUnitIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (targetUnitIds.Count == 0)
                throw new InvalidOperationException("Informe ao menos uma loja.");

            var legalTypeExists = await _context.CustomerLegalTypes
                .AnyAsync(lt => lt.Id == dto.LegalTypeId);

            if (!legalTypeExists)
            {
                throw new InvalidOperationException("Tipo legal do cliente inválido.");
            }

            var validUnitIds = await _context.Units
                .Where(u => targetUnitIds.Contains(u.Id) && u.TenantId == tenantId && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (validUnitIds.Count != targetUnitIds.Count)
            {
                throw new InvalidOperationException("Uma ou mais lojas são inválidas para o tenant informado.");
            }

            var rawDocument = dto.CpfCnpj?.Trim();

            if (string.IsNullOrWhiteSpace(rawDocument))
                throw new InvalidOperationException("CPF/CNPJ inválido.");

            var existingCustomer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c =>
                    c.TenantId == tenantId &&
                    c.CpfCnpj != null &&
                    c.CpfCnpj == rawDocument);

            if (existingCustomer != null)
            {
                var wasInactive = !existingCustomer.IsActive;

                if (!existingCustomer.IsActive)
                    await _planValidator.EnsureCanCreateCustomerAsync(tenantId);

                existingCustomer.IsActive = true;
                existingCustomer.LegalTypeId = dto.LegalTypeId;
                existingCustomer.Name = dto.Name;
                existingCustomer.CpfCnpj = rawDocument;
                existingCustomer.Email = dto.Email;
                existingCustomer.Phone = dto.Phone;
                existingCustomer.AddressZip = dto.AddressZip;
                existingCustomer.AddressStreet = dto.AddressStreet;
                existingCustomer.AddressNumber = dto.AddressNumber;
                existingCustomer.AddressDistrict = dto.AddressDistrict;
                existingCustomer.AddressCity = dto.AddressCity;
                existingCustomer.AddressState = dto.AddressState;
                existingCustomer.Notes = dto.Notes;

                foreach (var unitId in targetUnitIds)
                {
                    var activeLink = existingCustomer.CustomerUnits
                        .FirstOrDefault(cu => cu.UnitId == unitId && cu.IsActive);

                    var inactiveLink = existingCustomer.CustomerUnits
                        .FirstOrDefault(cu => cu.UnitId == unitId && !cu.IsActive);

                    if (activeLink == null && inactiveLink == null)
                    {
                        _context.CustomerUnits.Add(new CustomerUnit
                        {
                            CustomerId = existingCustomer.Id,
                            UnitId = unitId,
                            IsActive = true
                        });
                    }
                    else if (inactiveLink != null)
                    {
                        inactiveLink.IsActive = true;
                        _context.CustomerUnits.Update(inactiveLink);
                    }
                }

                var inactiveVehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == existingCustomer.Id && !v.IsActive)
                    .ToListAsync();

                foreach (var vehicle in inactiveVehicles)
                {
                    vehicle.IsActive = true;
                    _context.Vehicles.Update(vehicle);
                }

                _context.Customers.Update(existingCustomer);
                await _context.SaveChangesAsync();

                if (wasInactive)
                {
                    await _planValidator.RegisterCustomerCreatedAsync(tenantId);
                }

                var reloadedCustomer = await _context.Customers
                    .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                    .FirstAsync(c => c.Id == existingCustomer.Id);

                return (reloadedCustomer, false);
            }

            await _planValidator.EnsureCanCreateCustomerAsync(tenantId);

            var customer = new Customer
            {
                TenantId = tenantId,
                LegalTypeId = dto.LegalTypeId,
                Name = dto.Name,
                CpfCnpj = rawDocument,
                Email = dto.Email,
                Phone = dto.Phone,
                AddressZip = dto.AddressZip,
                AddressStreet = dto.AddressStreet,
                AddressNumber = dto.AddressNumber,
                AddressDistrict = dto.AddressDistrict,
                AddressCity = dto.AddressCity,
                AddressState = dto.AddressState,
                Notes = dto.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var links = targetUnitIds.Select(unitId => new CustomerUnit
            {
                CustomerId = customer.Id,
                UnitId = unitId,
                IsActive = true
            }).ToList();

            _context.CustomerUnits.AddRange(links);
            await _context.SaveChangesAsync();
            await _planValidator.RegisterCustomerCreatedAsync(tenantId);

            var reloadedNewCustomer = await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstAsync(c => c.Id == customer.Id);

            return (reloadedNewCustomer, true);
        }

        public async Task<Customer> UpdateCustomer(int id, UpdateCustomerRequest dto, int tenantId, List<int>? targetUnitIds = null)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.IsActive);

            if (customer == null)
            {
                throw new InvalidOperationException("Cliente não encontrado.");
            }

            if (targetUnitIds is { Count: > 0 })
            {
                targetUnitIds = targetUnitIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                var validUnitIds = await _context.Units
                    .Where(u => targetUnitIds.Contains(u.Id) && u.TenantId == tenantId && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (validUnitIds.Count != targetUnitIds.Count)
                {
                    throw new InvalidOperationException("Uma ou mais lojas são inválidas para o tenant informado.");
                }

                var targetSet = targetUnitIds.ToHashSet();

                foreach (var link in customer.CustomerUnits.Where(cu => cu.IsActive && !targetSet.Contains(cu.UnitId)))
                {
                    link.IsActive = false;
                    _context.CustomerUnits.Update(link);
                }

                foreach (var unitId in targetUnitIds)
                {
                    var activeLink = customer.CustomerUnits.FirstOrDefault(cu => cu.UnitId == unitId && cu.IsActive);
                    var inactiveLink = customer.CustomerUnits.FirstOrDefault(cu => cu.UnitId == unitId && !cu.IsActive);

                    if (activeLink is null && inactiveLink is null)
                    {
                        _context.CustomerUnits.Add(new CustomerUnit
                        {
                            CustomerId = customer.Id,
                            UnitId = unitId,
                            IsActive = true
                        });
                    }
                    else if (inactiveLink is not null)
                    {
                        inactiveLink.IsActive = true;
                        _context.CustomerUnits.Update(inactiveLink);
                    }
                }

                if (!customer.CustomerUnits.Any(cu => cu.IsActive || targetSet.Contains(cu.UnitId)))
                {
                    throw new InvalidOperationException("O cliente deve permanecer vinculado a pelo menos uma loja.");
                }
            }

            if (dto.LegalTypeId.HasValue)
            {
                var legalTypeExists = await _context.CustomerLegalTypes
                    .AnyAsync(lt => lt.Id == dto.LegalTypeId.Value);

                if (!legalTypeExists)
                {
                    throw new InvalidOperationException("Tipo legal do cliente inválido.");
                }

                customer.LegalTypeId = dto.LegalTypeId.Value;
            }

            if (dto.Name is not null) customer.Name = dto.Name;

            if (dto.CpfCnpj is not null)
            {
                var rawDocument = dto.CpfCnpj.Trim();

                if (string.IsNullOrWhiteSpace(rawDocument))
                    throw new InvalidOperationException("CPF/CNPJ inválido.");

                customer.CpfCnpj = rawDocument;
            }

            if (dto.Email is not null) customer.Email = dto.Email;
            if (dto.Phone is not null) customer.Phone = dto.Phone;
            if (dto.AddressZip is not null) customer.AddressZip = dto.AddressZip;
            if (dto.AddressStreet is not null) customer.AddressStreet = dto.AddressStreet;
            if (dto.AddressNumber is not null) customer.AddressNumber = dto.AddressNumber;
            if (dto.AddressDistrict is not null) customer.AddressDistrict = dto.AddressDistrict;
            if (dto.AddressCity is not null) customer.AddressCity = dto.AddressCity;
            if (dto.AddressState is not null) customer.AddressState = dto.AddressState;
            if (dto.Notes is not null) customer.Notes = dto.Notes;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            return await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstAsync(c => c.Id == id);
        }

        public async Task<bool> DeleteCustomerLinksOrCustomer(int customerId, int tenantId, List<int> allowedUnitIds, bool fullAccess)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId && c.IsActive);

            if (customer == null) return false;

            var linksToDeactivate = customer.CustomerUnits
                .Where(cu => cu.IsActive && (fullAccess || allowedUnitIds.Contains(cu.UnitId)))
                .ToList();

            if (linksToDeactivate.Count == 0) return false;

            foreach (var link in linksToDeactivate)
            {
                link.IsActive = false;
            }

            _context.CustomerUnits.UpdateRange(linksToDeactivate);

            if (!customer.CustomerUnits.Any(cu => cu.IsActive))
            {
                customer.IsActive = false;
                _context.Customers.Update(customer);
            }

            await _context.SaveChangesAsync();

            if (!customer.IsActive)
            {
                await _planValidator.RegisterCustomerDeletedAsync(customer.TenantId);
            }

            return true;
        }

        public bool HasAccessToCustomer(Customer customer, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (customer.TenantId != tenantId || !customer.IsActive) return false;
            if (!customer.CustomerUnits.Any(cu => cu.IsActive)) return false;
            if (fullAccess) return true;

            return customer.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId));
        }
    }
}
