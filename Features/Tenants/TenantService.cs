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
            // Validar se CNPJ já existe
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
    }
}