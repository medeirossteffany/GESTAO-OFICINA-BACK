using GestaoOficina.Data;
using GestaoOficina.DTOs.Vehicles;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using GestaoOficina.Features.Tenants;
using GestaoOficina.Features.Customers;

namespace GestaoOficina.Features.Vehicles
{
    public class VehicleService
    {
        private readonly AppDbContext _context;
        private readonly TenantPlanValidator _planValidator;
        private readonly CustomerService _customerService;

        public VehicleService(AppDbContext context, TenantPlanValidator planValidator, CustomerService customerService)
        {
            _context = context;
            _planValidator = planValidator;
            _customerService = customerService;
        }

        public async Task<List<Vehicle>> GetVehiclesByTenantAndUnits(
            int tenantId,
            List<int> unitIds,
            bool fullAccess,
            int? selectedUnitId = null)
        {
            var query = _context.Vehicles
                .Where(v => v.TenantId == tenantId)
                .Where(v => v.IsActive)
                .Where(v => v.Customer.IsActive)
                .Where(v => fullAccess || v.Customer.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId)))
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .AsQueryable();

            if (selectedUnitId.HasValue)
            {
                query = query.Where(v => v.Customer.CustomerUnits.Any(cu => cu.IsActive && cu.UnitId == selectedUnitId.Value));
            }

            return await query
                .OrderBy(v => v.Plate)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleById(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);
        }

        public async Task<Customer?> GetCustomerById(int customerId, int tenantId)
        {
            return await _context.Customers
                .Include(c => c.CustomerUnits.Where(cu => cu.IsActive))
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId && c.IsActive);
        }

        public async Task<Vehicle> CreateVehicle(CreateVehicleRequest dto, int tenantId, List<int> unitIds, bool fullAccess)
        {
            var customer = await _customerService.GetCustomerById(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException("Cliente não encontrado.");

            if (!_customerService.HasAccessToCustomer(customer, tenantId, unitIds, fullAccess))
                throw new InvalidOperationException("Você não tem permissão para cadastrar veículo para este cliente.");

            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Plate == dto.Plate);

            if (existingVehicle != null)
            {
                var wasInactive = !existingVehicle.IsActive;

                if (!existingVehicle.IsActive)
                {
                    await _planValidator.EnsureCanCreateVehicleAsync(tenantId);
                }

                existingVehicle.CustomerId = dto.CustomerId;
                existingVehicle.Brand = dto.Brand;
                existingVehicle.Model = dto.Model;
                existingVehicle.Year = dto.Year;
                existingVehicle.Color = dto.Color;
                existingVehicle.Vin = dto.Vin;
                existingVehicle.Renavam = dto.Renavam;
                existingVehicle.InsuranceClaimNumber = dto.InsuranceClaimNumber;
                existingVehicle.Notes = dto.Notes;
                existingVehicle.IsActive = true;

                _context.Vehicles.Update(existingVehicle);
                await _context.SaveChangesAsync();

                if (wasInactive)
                {
                    await _planValidator.RegisterVehicleCreatedAsync(tenantId);
                }

                return await GetVehicleById(existingVehicle.Id) ?? existingVehicle;
            }

            await _planValidator.EnsureCanCreateVehicleAsync(tenantId);

            var vehicle = new Vehicle
            {
                TenantId = tenantId,
                CustomerId = dto.CustomerId,
                Plate = dto.Plate,
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Color = dto.Color,
                Vin = dto.Vin,
                Renavam = dto.Renavam,
                InsuranceClaimNumber = dto.InsuranceClaimNumber,
                Notes = dto.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            await _planValidator.RegisterVehicleCreatedAsync(tenantId);

            return await GetVehicleById(vehicle.Id) ?? vehicle;
        }

        public async Task<Vehicle?> UpdateVehicle(int id, UpdateVehicleRequest dto, int tenantId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits)
                .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId && v.IsActive);

            if (vehicle == null) return null;

            var targetCustomerId = dto.CustomerId ?? vehicle.CustomerId;
            var customer = await GetCustomerById(targetCustomerId, tenantId);
            if (customer == null)
            {
                throw new InvalidOperationException("Cliente inválido para o tenant informado.");
            }

            var targetPlate = dto.Plate ?? vehicle.Plate;
            if (!string.Equals(vehicle.Plate, targetPlate, StringComparison.OrdinalIgnoreCase))
            {
                var duplicatePlate = await _context.Vehicles
                    .AnyAsync(v => v.Id != id && v.TenantId == tenantId && v.Plate == targetPlate && v.IsActive);

                if (duplicatePlate)
                {
                    throw new InvalidOperationException($"A placa {targetPlate} já está cadastrada para este tenant.");
                }
            }

            vehicle.CustomerId = targetCustomerId;
            vehicle.Plate = targetPlate;
            if (dto.Brand is not null) vehicle.Brand = dto.Brand;
            if (dto.Model is not null) vehicle.Model = dto.Model;
            if (dto.Year.HasValue) vehicle.Year = dto.Year;
            if (dto.Color is not null) vehicle.Color = dto.Color;
            if (dto.Vin is not null) vehicle.Vin = dto.Vin;
            if (dto.Renavam is not null) vehicle.Renavam = dto.Renavam;
            if (dto.InsuranceClaimNumber is not null) vehicle.InsuranceClaimNumber = dto.InsuranceClaimNumber;
            if (dto.Notes is not null) vehicle.Notes = dto.Notes;

            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();

            return await GetVehicleById(vehicle.Id);
        }

        public async Task<bool> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

            if (vehicle == null) return false;

            vehicle.IsActive = false;

            var serviceOrders = await _context.ServiceOrders
                .Where(so => so.VehicleId == id && so.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);
            var currentMonthServiceOrdersDeleted = serviceOrders.Count(so => so.CreatedAt >= monthStart && so.CreatedAt < nextMonthStart);

            foreach (var serviceOrder in serviceOrders)
            {
                serviceOrder.IsActive = false;
            }

            var serviceOrderIds = serviceOrders.Select(so => so.Id).ToList();

            var parts = await _context.ServiceOrderParts
                .Where(p => serviceOrderIds.Contains(p.ServiceOrderId) && p.IsActive)
                .ToListAsync();

            foreach (var part in parts)
            {
                part.IsActive = false;
            }

            var timelines = await _context.ServiceOrderTimelines
                .Where(t => serviceOrderIds.Contains(t.ServiceOrderId) && t.IsActive)
                .ToListAsync();

            foreach (var timeline in timelines)
            {
                timeline.IsActive = false;
            }

            _context.Vehicles.Update(vehicle);
            if (serviceOrders.Count > 0) _context.ServiceOrders.UpdateRange(serviceOrders);
            if (parts.Count > 0) _context.ServiceOrderParts.UpdateRange(parts);
            if (timelines.Count > 0) _context.ServiceOrderTimelines.UpdateRange(timelines);

            await _context.SaveChangesAsync();
            await _planValidator.RegisterVehicleDeletedAsync(vehicle.TenantId);
            await _planValidator.RegisterServiceOrdersDeletedInCurrentMonthAsync(vehicle.TenantId, currentMonthServiceOrdersDeleted);
            return true;
        }

        public bool HasAccessToVehicle(Vehicle vehicle, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (vehicle.TenantId != tenantId) return false;
            if (!vehicle.IsActive) return false;
            if (!vehicle.Customer.IsActive) return false;
            if (fullAccess) return true;

            return vehicle.Customer.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId));
        }

        public bool HasAccessToCustomer(Customer customer, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (customer.TenantId != tenantId || !customer.IsActive) return false;
            if (fullAccess) return true;

            return customer.CustomerUnits.Any(cu => cu.IsActive && unitIds.Contains(cu.UnitId));
        }
    }
}
