namespace GestaoOficina.DTOs.ServiceOrders
{
    public class UpdateServiceOrderRequest
    {
        public int UnitId { get; set; }
        public int VehicleId { get; set; }
        public int OwnerCustomerId { get; set; }
        public int? StatusId { get; set; }

        public DateTime EntryDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public string? BodyworkDescription { get; set; }
        public decimal BodyworkValue { get; set; }

        public string? PaintDescription { get; set; }
        public decimal PaintValue { get; set; }

        public decimal TotalDiscount { get; set; }
        public List<CreateServiceOrderPartItemRequest> Parts { get; set; } = [];
    }
}