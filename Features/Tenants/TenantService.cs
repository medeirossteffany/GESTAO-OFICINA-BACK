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

        public Tenant CreateTenant(CreateTenantRequest dto)
        {
            var tenant = new Tenant
            {
                Name = dto.Name,
                Cnpj = dto.Cnpj,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            _context.SaveChanges();
            return tenant;
        }
    }
}