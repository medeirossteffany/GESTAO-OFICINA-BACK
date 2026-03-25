using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.Vehicles
{
    public class CreateVehicleRequest
    {
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [Required]
        public string Plate { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        public string Model { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Required]
        public string Color { get; set; }

        public string? Vin { get; set; }
        public string? Renavam { get; set; }
        public string? InsuranceClaimNumber { get; set; }
        public string? Notes { get; set; }
    }
}
