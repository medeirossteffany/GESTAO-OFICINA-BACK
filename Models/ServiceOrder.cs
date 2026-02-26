using System;

namespace GestaoOficina.Models
{
    public class ServiceOrder
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UnitId { get; set; }
        public int VehicleId { get; set; }
        public int OwnerCustomerId { get; set; }
        public int PayerCustomerId { get; set; }
        public int StatusId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? BodyworkDescription { get; set; }
        public decimal BodyworkValue { get; set; }
        public string? PaintDescription { get; set; }
        public decimal PaintValue { get; set; }
        public decimal PartsValue { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public Unit Unit { get; set; }
        public Vehicle Vehicle { get; set; }
        public Customer OwnerCustomer { get; set; }
        public Customer PayerCustomer { get; set; }
        public ServiceOrderStatus Status { get; set; }
    }
}