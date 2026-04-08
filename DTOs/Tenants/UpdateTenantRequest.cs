using GestaoOficina.Entities;

namespace GestaoOficina.DTOs.Tenants
{
    public class UpdateTenantRequest
    {
        public string? Name { get; set; }
        public string? Cnpj { get; set; }
        public TenantPlan? Plan { get; set; }
    }
}
