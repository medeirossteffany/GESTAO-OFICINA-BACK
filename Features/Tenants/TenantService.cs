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
            var tenant = new Tenant
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }

        public async Task<Tenant?> GetTenant(int id)
        {
            return await _context.Tenants
                .Include(t => t.Unit)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tenant?> UpdateTenant(int id, UpdateTenantRequest dto)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Unit)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return null;

            if (dto.Name is not null)
                tenant.Name = dto.Name;

            if (dto.Cnpj is not null)
            {
                if (tenant.Unit is null)
                    throw new InvalidOperationException("Tenant não possui unidade vinculada para atualizar CNPJ.");

                if (!string.Equals(tenant.Unit.Cnpj, dto.Cnpj, StringComparison.Ordinal))
                {
                    var unitInOtherTenant = await _context.Units
                        .FirstOrDefaultAsync(u => u.TenantId != tenant.Id && u.Cnpj == dto.Cnpj);

                    if (unitInOtherTenant is not null)
                        throw new InvalidOperationException($"O CNPJ {dto.Cnpj} já está sendo utilizado por outra Unit de outro Tenant.");

                    tenant.Unit.Cnpj = dto.Cnpj;
                    _context.Units.Update(tenant.Unit);
                }
            }

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return tenant;
        }
    }
}
