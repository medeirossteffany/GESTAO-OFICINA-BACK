namespace GestaoOficina.Entities
{
    public class TenantUsage
    {
        public int TenantId { get; set; }
        public int CurrentUnits { get; set; }
        public int CurrentUsers { get; set; }
        public int CurrentCustomers { get; set; }
        public int CurrentVehicles { get; set; }
        public int CurrentServicesInMonth { get; set; }
        public DateTime ServicesMonthReference { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Tenant Tenant { get; set; }
    }
}
