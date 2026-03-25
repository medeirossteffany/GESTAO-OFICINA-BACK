using System;

namespace GestaoOficina.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int CustomerId { get; set; }
        public string Plate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int? Year { get; set; }
        public string? Color { get; set; }
        public string? Vin { get; set; }
        public string? Renavam { get; set; }
        public string? InsuranceClaimNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public Customer Customer { get; set; }
    }
}
