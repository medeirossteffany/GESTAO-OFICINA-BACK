using GestaoOficina.Entities;

namespace GestaoOficina.DTOs.Tenants
{
    public class CreateTenantRequest
    {
        public string Name { get; set; }
        public TenantPlan? Plan { get; set; }
    }
}
