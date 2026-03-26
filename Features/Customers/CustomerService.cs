using GestaoOficina.Data;
using GestaoOficina.DTOs.Customers;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Customers
{
    public class CustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetCustomersByTenantAndUnits(int tenantId, List<int> unitIds, bool fullAccess)
        {
            return await _context.Customers
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .Where(c => fullAccess || c.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId)))
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerById(int id)
        {
            return await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<(Customer Customer, bool CreatedNew)> CreateOrLinkCustomer(CreateCustomerRequest dto, int tenantId)
        {
            var legalTypeExists = await _context.CustomerLegalTypes
                .AnyAsync(lt => lt.Id == dto.LegalTypeId);

            if (!legalTypeExists)
            {
                throw new InvalidOperationException("Tipo legal do cliente inválido.");
            }

            var unitExistsInTenant = await _context.Units
                .AnyAsync(u => u.Id == dto.UnitId && u.TenantId == tenantId && u.IsActive);

            if (!unitExistsInTenant)
            {
                throw new InvalidOperationException("Loja inválida para o tenant informado.");
            }

            if (!string.IsNullOrWhiteSpace(dto.CpfCnpj))
            {
                var existingCustomer = await _context.Customers
                    .Include(c => c.CustomerUnits)
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CpfCnpj == dto.CpfCnpj);

                if (existingCustomer != null)
                {
                    existingCustomer.IsActive = true;

                    var activeLink = existingCustomer.CustomerUnits
                        .FirstOrDefault(cu => cu.UnitId == dto.UnitId && cu.IsActive);

                    var inactiveLink = existingCustomer.CustomerUnits
                        .FirstOrDefault(cu => cu.UnitId == dto.UnitId && !cu.IsActive);

                    if (activeLink == null && inactiveLink == null)
                    {
                        _context.CustomerUnits.Add(new CustomerUnit
                        {
                            CustomerId = existingCustomer.Id,
                            UnitId = dto.UnitId,
                            IsActive = true
                        });
                    }
                    else if (inactiveLink != null)
                    {
                        inactiveLink.IsActive = true;
                        _context.CustomerUnits.Update(inactiveLink);
                    }

                    _context.Customers.Update(existingCustomer);
                    await _context.SaveChangesAsync();

                    var reloadedCustomer = await _context.Customers
                        .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                        .FirstAsync(c => c.Id == existingCustomer.Id);

                    return (reloadedCustomer, false);
                }
            }

            var customer = new Customer
            {
                TenantId = tenantId,
                LegalTypeId = dto.LegalTypeId,
                Name = dto.Name,
                CpfCnpj = dto.CpfCnpj,
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

            _context.CustomerUnits.Add(new CustomerUnit
            {
                CustomerId = customer.Id,
                UnitId = dto.UnitId,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            var reloadedNewCustomer = await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstAsync(c => c.Id == customer.Id);

            return (reloadedNewCustomer, true);
        }

        public async Task<Customer> UpdateCustomer(int id, UpdateCustomerRequest dto)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                throw new InvalidOperationException("Cliente não encontrado.");
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
            if (dto.CpfCnpj is not null) customer.CpfCnpj = dto.CpfCnpj;
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

        public async Task<bool> DeleteCustomerLinksOrCustomer(int customerId, List<int> targetUnitIds)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

            if (customer == null) return false;

            var linksToDeactivate = customer.CustomerUnits
                .Where(cu => cu.IsActive && targetUnitIds.Contains(cu.UnitId))
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
            return true;
        }

        public bool HasAccessToCustomer(Customer customer, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (customer.TenantId != tenantId || !customer.IsActive) return false;
            if (fullAccess) return true;

            return customer.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId));
        }
    }
}
