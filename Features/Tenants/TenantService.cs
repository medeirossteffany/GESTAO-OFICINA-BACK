using GestaoOficina.Entities;
using GestaoOficina.DTOs.Tenants;
using GestaoOficina.Data;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Tenants
{
    public class TenantService
    {
        private readonly AppDbContext _context;
        public TenantService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant> CreateTenant(CreateTenantRequest dto)
        {
           
            if (!string.IsNullOrWhiteSpace(dto.Cnpj))
            {
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Cnpj == dto.Cnpj);
                
                if (existingTenant != null)
                {
                    throw new InvalidOperationException($"Já existe um Tenant com o CNPJ {dto.Cnpj}.");
                }
            }

            var tenant = new Tenant
            {
                Name = dto.Name,
                Cnpj = dto.Cnpj,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }

        public async Task<Tenant?> GetTenant(int id)
        {
            return await _context.Tenants.FindAsync(id);
        }

        public async Task<Tenant?> UpdateTenant(int id, UpdateTenantRequest dto)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Cnpj) && tenant.Cnpj != dto.Cnpj)
            {
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id != id && t.Cnpj == dto.Cnpj);
                
                if (existingTenant != null)
                {
                    throw new InvalidOperationException($"Já existe um Tenant com o CNPJ {dto.Cnpj}.");
                }
            }

            tenant.Name = dto.Name;
            tenant.Cnpj = dto.Cnpj;

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }
    }
}