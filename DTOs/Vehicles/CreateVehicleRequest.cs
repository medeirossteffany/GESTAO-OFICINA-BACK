namespace GestaoOficina.DTOs.Vehicles
{
    public class CreateVehicleRequest
    {
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
    }
}
