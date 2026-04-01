namespace GestaoOficina.DTOs.ServiceOrders
{
    public class ServiceOrderExcelSummaryResponse
    {
        public int TotalLinhasProcessadas { get; set; }
        public decimal TotalGeral { get; set; }
        public List<ServiceOrderExcelStoreSummaryResponse> Lojas { get; set; } = [];
    }

    public class ServiceOrderExcelStoreSummaryResponse
    {
        public string Loja { get; set; } = string.Empty;
        public int QuantidadePlacas { get; set; }
        public decimal TotalLoja { get; set; }
        public List<ServiceOrderExcelPlateSummaryResponse> Placas { get; set; } = [];
    }

    public class ServiceOrderExcelPlateSummaryResponse
    {
        public string Placa { get; set; } = string.Empty;
        public int QuantidadeServicos { get; set; }
        public decimal TotalPlaca { get; set; }
        public List<ServiceOrderExcelServiceItemResponse> Servicos { get; set; } = [];
    }

    public class ServiceOrderExcelServiceItemResponse
    {
        public string TpServico { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Fornecedor { get; set; } = string.Empty;
        public string? ObsConsultor { get; set; }
    }
}