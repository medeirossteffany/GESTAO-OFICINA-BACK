namespace GestaoOficina.Features.Tenants
{
    public sealed record TenantPlanLimits(
        int MaxUnits,
        int MaxUsers,
        int MaxCustomers,
        int MaxVehicles,
        int MaxServicesPerMonth);

    public static class TenantPlanCatalog
    {
        public static TenantPlanLimits Get(Entities.TenantPlan plan) => plan switch
        {
            Entities.TenantPlan.Basico => new(1, 3, 500, 700, 80),
            Entities.TenantPlan.Profissional => new(2, 10, 3000, 4000, 300),
            Entities.TenantPlan.Premium => new(5, 30, 10000, 15000, 1000),
            _ => throw new InvalidOperationException("Plano inválido.")
        };
    }
}