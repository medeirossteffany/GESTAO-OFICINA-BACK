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

        public async Task<Unit> CreateUnit(CreateUnitRequest dto)
        {
            var unit = new Unit
            {
                TenantId = dto.TenantId,
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

            unit.Name = dto.Name;
            unit.Cnpj = dto.Cnpj;
            unit.AddressZip = dto.AddressZip;
            unit.AddressStreet = dto.AddressStreet;
            unit.AddressNumber = dto.AddressNumber;
            unit.AddressDistrict = dto.AddressDistrict;
            unit.AddressCity = dto.AddressCity;
            unit.AddressState = dto.AddressState;

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