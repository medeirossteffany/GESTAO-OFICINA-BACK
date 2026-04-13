using System;
using System.Collections.Generic;

namespace GestaoOficina.DTOs.ServiceOrders
{
    public class ServiceOrderResponse
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UnitId { get; set; }
        public string? UnitName { get; set; }
        public int VehicleId { get; set; }
        public string? VehiclePlate { get; set; }
        public int OwnerCustomerId { get; set; }
        public string? OwnerCustomerName { get; set; }
        public int StatusId { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? BodyworkDescription { get; set; }
        public decimal? BodyworkValue { get; set; }
        public string? PaintDescription { get; set; }
        public decimal? PaintValue { get; set; }
        public decimal? PartsValue { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ServiceOrderPartResponse> Parts { get; set; } = new();
        public string? MechanicsDescription { get; set; }
        public decimal? MechanicsValue { get; set; }
    }

    public class ServiceOrderPartResponse
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
