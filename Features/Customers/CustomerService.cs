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
                .Where(c => fullAccess || c.CustomerUnits.Any(cu => unitIds.Contains(cu.UnitId)))
                .Include(c => c.CustomerUnits)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerById(int id)
        {
            return await _context.Customers
                .Include(c => c.CustomerUnits)
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
                .AnyAsync(u => u.Id == dto.UnitId && u.TenantId == tenantId);

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

                    var alreadyLinked = existingCustomer.CustomerUnits.Any(cu => cu.UnitId == dto.UnitId);
                    if (!alreadyLinked)
                    {
                        _context.CustomerUnits.Add(new CustomerUnit
                        {
                            CustomerId = existingCustomer.Id,
                            UnitId = dto.UnitId
                        });
                    }

                    _context.Customers.Update(existingCustomer);
                    await _context.SaveChangesAsync();
                    await _context.Entry(existingCustomer).Collection(c => c.CustomerUnits).LoadAsync();

                    return (existingCustomer, false);
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
                UnitId = dto.UnitId
            });
            await _context.SaveChangesAsync();

            await _context.Entry(customer).Collection(c => c.CustomerUnits).LoadAsync();
            return (customer, true);
        }

        public async Task<Customer?> UpdateCustomer(int id, UpdateCustomerRequest dto)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null) return null;

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
            return customer;
        }

        public async Task<bool> DeleteCustomerLinksOrCustomer(int id, List<int> unitIds)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null) return false;

            var linksToRemove = customer.CustomerUnits
                .Where(cu => unitIds.Contains(cu.UnitId))
                .ToList();

            if (linksToRemove.Count == 0) return false;

            _context.CustomerUnits.RemoveRange(linksToRemove);
            await _context.SaveChangesAsync();

            var hasOtherLinks = await _context.CustomerUnits.AnyAsync(cu => cu.CustomerId == id);
            if (!hasOtherLinks)
            {
                customer.IsActive = false;
                _context.Customers.Update(customer);

                var vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == id && v.IsActive)
                    .ToListAsync();

                foreach (var vehicle in vehicles)
                {
                    vehicle.IsActive = false;
                }

                var serviceOrders = await _context.ServiceOrders
                    .Where(so => so.IsActive && (so.OwnerCustomerId == id || so.Vehicle.CustomerId == id))
                    .ToListAsync();

                foreach (var serviceOrder in serviceOrders)
                {
                    serviceOrder.IsActive = false;
                }

                if (vehicles.Count > 0)
                {
                    _context.Vehicles.UpdateRange(vehicles);
                }

                if (serviceOrders.Count > 0)
                {
                    _context.ServiceOrders.UpdateRange(serviceOrders);
                }

                await _context.SaveChangesAsync();
            }

            return true;
        }

        public bool HasAccessToCustomer(Customer customer, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (customer.TenantId != tenantId || !customer.IsActive) return false;
            if (fullAccess) return true;

            return customer.CustomerUnits.Any(cu => unitIds.Contains(cu.UnitId));
        }
    }
}
