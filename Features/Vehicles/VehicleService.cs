using GestaoOficina.Data;
using GestaoOficina.DTOs.Vehicles;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Vehicles
{
    public class VehicleService
    {
        private readonly AppDbContext _context;

        public VehicleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetVehiclesByTenantAndUnits(int tenantId, List<int> unitIds, bool fullAccess)
        {
            return await _context.Vehicles
                .Where(v => v.TenantId == tenantId)
                .Where(v => v.Customer.IsActive)
                .Where(v => fullAccess || v.Customer.CustomerUnits.Any(cu => unitIds.Contains(cu.UnitId)))
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits)
                .OrderBy(v => v.Plate)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleById(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Customer?> GetCustomerById(int customerId, int tenantId)
        {
            return await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId && c.IsActive);
        }

        public async Task<Vehicle> CreateVehicle(CreateVehicleRequest dto, int tenantId)
        {
            var customer = await GetCustomerById(dto.CustomerId, tenantId);
            if (customer == null)
            {
                throw new InvalidOperationException("Cliente invÃ¡lido para o tenant informado.");
            }

            var duplicatePlate = await _context.Vehicles
                .AnyAsync(v => v.TenantId == tenantId && v.Plate == dto.Plate);

            if (duplicatePlate)
            {
                throw new InvalidOperationException($"A placa {dto.Plate} jÃ¡ estÃ¡ cadastrada para este tenant.");
            }

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
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return await GetVehicleById(vehicle.Id) ?? vehicle;
        }

        public async Task<Vehicle?> UpdateVehicle(int id, UpdateVehicleRequest dto, int tenantId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Customer)
                    .ThenInclude(c => c.CustomerUnits)
                .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);

            if (vehicle == null) return null;

            var targetCustomerId = dto.CustomerId ?? vehicle.CustomerId;
            var customer = await GetCustomerById(targetCustomerId, tenantId);
            if (customer == null)
            {
                throw new InvalidOperationException("Cliente invÃ¡lido para o tenant informado.");
            }

            var targetPlate = dto.Plate ?? vehicle.Plate;
            if (!string.Equals(vehicle.Plate, targetPlate, StringComparison.OrdinalIgnoreCase))
            {
                var duplicatePlate = await _context.Vehicles
                    .AnyAsync(v => v.Id != id && v.TenantId == tenantId && v.Plate == targetPlate);

                if (duplicatePlate)
                {
                    throw new InvalidOperationException($"A placa {targetPlate} jÃ¡ estÃ¡ cadastrada para este tenant.");
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
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return false;

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }

        public bool HasAccessToVehicle(Vehicle vehicle, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (vehicle.TenantId != tenantId) return false;
            if (!vehicle.Customer.IsActive) return false;
            if (fullAccess) return true;

            return vehicle.Customer.CustomerUnits.Any(cu => unitIds.Contains(cu.UnitId));
        }

        public bool HasAccessToCustomer(Customer customer, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (customer.TenantId != tenantId || !customer.IsActive) return false;
            if (fullAccess) return true;

            return customer.CustomerUnits.Any(cu => unitIds.Contains(cu.UnitId));
        }
    }
}
