using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.Units
{
    public class CreateUnitRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Cnpj { get; set; }

        [Required]
        public string AddressZip { get; set; }

        [Required]
        public string AddressStreet { get; set; }

        [Required]
        public string AddressNumber { get; set; }

        [Required]
        public string AddressDistrict { get; set; }

        [Required]
        public string AddressCity { get; set; }

        [Required]
        public string AddressState { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}
