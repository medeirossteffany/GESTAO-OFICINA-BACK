namespace GestaoOficina.DTOs.ServiceOrders
{
    public class CreateServiceOrderRequest
    {
        public int UnitId { get; set; } // Loja
        public int VehicleId { get; set; } // VeÃ­culo
        public int OwnerCustomerId { get; set; } // Cliente responsÃ¡vel

        public DateTime EntryDate { get; set; } // Data de entrada
        public DateTime? EstimatedDeliveryDate { get; set; } // PrevisÃ£o saÃ­da

        public string? BodyworkDescription { get; set; } // Funilaria
        public decimal BodyworkValue { get; set; }

        public string? PaintDescription { get; set; } // Pintura
        public decimal PaintValue { get; set; }

        public decimal TotalDiscount { get; set; } // Opcional
        public List<CreateServiceOrderPartItemRequest> Parts { get; set; } = [];
    }

    public class CreateServiceOrderPartItemRequest
    {
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
