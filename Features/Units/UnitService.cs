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
                CreatedAt = DateTime.UtcNow
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<List<Unit>> GetUnitsByTenant(int tenantId)
        {
            return await _context.Units
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();
        }

        public async Task<Unit?> GetUnitById(int id)
        {
            return await _context.Units.FindAsync(id);
        }

        public async Task<Unit?> UpdateUnit(int id, UpdateUnitRequest dto)
        {
            var unit = await _context.Units.FindAsync(id);
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
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return false;

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}