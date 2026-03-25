using GestaoOficina.Entities;
using GestaoOficina.DTOs.Units;
using GestaoOficina.Data;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Units
{
    public class UnitService
    {
        private readonly AppDbContext _context;
        public UnitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> CreateUnit(CreateUnitRequest dto, int tenantId)
        {
            if (!string.IsNullOrWhiteSpace(dto.Cnpj))
            {
                var unitInOtherTenant = await _context.Units
                    .FirstOrDefaultAsync(u => u.TenantId != tenantId && u.Cnpj == dto.Cnpj);
                
                if (unitInOtherTenant != null)
                {
                    throw new InvalidOperationException($"O CNPJ {dto.Cnpj} já está sendo utilizado por outra Unit de outro Tenant.");
                }
            }

            var unit = new Unit
            {
                TenantId = tenantId,
                Name = dto.Name,
                Cnpj = dto.Cnpj,
                AddressZip = dto.AddressZip,
                AddressStreet = dto.AddressStreet,
                AddressNumber = dto.AddressNumber,
                AddressDistrict = dto.AddressDistrict,
                AddressCity = dto.AddressCity,
                AddressState = dto.AddressState,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<List<Unit>> GetUnitsByTenant(int tenantId)
        {
            return await _context.Units
                .Where(u => u.TenantId == tenantId && u.IsActive)
                .ToListAsync();
        }

        public async Task<Unit?> GetUnitById(int id)
        {
            return await _context.Units
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<Unit?> UpdateUnit(int id, UpdateUnitRequest dto)
        {
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
            if (unit == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Cnpj) && unit.Cnpj != dto.Cnpj)
            {
                var unitInOtherTenant = await _context.Units
                    .FirstOrDefaultAsync(u => u.TenantId != unit.TenantId && u.Cnpj == dto.Cnpj);
                
                if (unitInOtherTenant != null)
                {
                    throw new InvalidOperationException($"O CNPJ {dto.Cnpj} já está sendo utilizado por outra Unit de outro Tenant.");
                }
            }

            if (dto.Name is not null) unit.Name = dto.Name;
            if (dto.Cnpj is not null) unit.Cnpj = dto.Cnpj;
            if (dto.AddressZip is not null) unit.AddressZip = dto.AddressZip;
            if (dto.AddressStreet is not null) unit.AddressStreet = dto.AddressStreet;
            if (dto.AddressNumber is not null) unit.AddressNumber = dto.AddressNumber;
            if (dto.AddressDistrict is not null) unit.AddressDistrict = dto.AddressDistrict;
            if (dto.AddressCity is not null) unit.AddressCity = dto.AddressCity;
            if (dto.AddressState is not null) unit.AddressState = dto.AddressState;

            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<bool> DeleteUnit(int id)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (unit == null) return false;

            unit.IsActive = false;

            var userUnits = await _context.UserUnits
                .Where(uu => uu.UnitId == id && uu.IsActive)
                .ToListAsync();

            foreach (var userUnit in userUnits)
            {
                userUnit.IsActive = false;
            }

            var customerUnits = await _context.CustomerUnits
                .Where(cu => cu.UnitId == id && cu.IsActive)
                .ToListAsync();

            foreach (var customerUnit in customerUnits)
            {
                customerUnit.IsActive = false;
            }

            var serviceOrders = await _context.ServiceOrders
                .Where(so => so.UnitId == id && so.IsActive)
                .ToListAsync();

            foreach (var serviceOrder in serviceOrders)
            {
                serviceOrder.IsActive = false;
                serviceOrder.UpdatedAt = DateTime.UtcNow;
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

            _context.Units.Update(unit);
            if (userUnits.Count > 0) _context.UserUnits.UpdateRange(userUnits);
            if (customerUnits.Count > 0) _context.CustomerUnits.UpdateRange(customerUnits);
            if (serviceOrders.Count > 0) _context.ServiceOrders.UpdateRange(serviceOrders);
            if (parts.Count > 0) _context.ServiceOrderParts.UpdateRange(parts);
            if (timelines.Count > 0) _context.ServiceOrderTimelines.UpdateRange(timelines);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
