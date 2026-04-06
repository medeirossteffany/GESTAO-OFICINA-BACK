using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.Users
{
    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CpfCnpj { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }

        [Required(ErrorMessage = "Role é obrigatório.")]
        public string Role { get; set; } = default!;

        public bool? IsActive { get; set; }
        public bool? FullAccess { get; set; }
        public List<int>? UnitIds { get; set; }
    }
}
