using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.Customers
{
    public class CreateCustomerRequest
    {
        [Range(1, int.MaxValue)]
        public int UnitId { get; set; }

        [Range(1, int.MaxValue)]
        public int LegalTypeId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string CpfCnpj { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

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

        public string? Notes { get; set; }
    }
}
