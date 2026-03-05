using System;

namespace GestaoOficina.Entities
{
    public class ServiceOrderPart
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int ServiceOrderId { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public Tenant Tenant { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
    }
}