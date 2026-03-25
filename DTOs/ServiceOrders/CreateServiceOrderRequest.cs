namespace GestaoOficina.DTOs.ServiceOrders
{
    public class CreateServiceOrderRequest
    {
        public int UnitId { get; set; }
        public int VehicleId { get; set; }
        public int OwnerCustomerId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? BodyworkDescription { get; set; }
        public decimal BodyworkValue { get; set; }
        public string? PaintDescription { get; set; }
        public decimal PaintValue { get; set; }
        public List<CreateServiceOrderPartItemRequest> Parts { get; set; } = [];
    }

    public class CreateServiceOrderPartItemRequest
    {
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
