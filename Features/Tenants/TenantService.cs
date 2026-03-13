using GestaoOficina.Entities;
using GestaoOficina.DTOs.Tenants;
using GestaoOficina.Data;

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
            return await _context.Tenants.FindAsync(id);
        }

        public async Task<Tenant?> UpdateTenant(int id, UpdateTenantRequest dto)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return null;

            tenant.Name = dto.Name;

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }
    }
}