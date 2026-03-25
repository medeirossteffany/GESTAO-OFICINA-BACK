using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.Users
{
    public class CreateUserRequest : IValidatableObject
    {
        public List<int>? UnitIds { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string CpfCnpj { get; set; }

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

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var isAdmin = string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && (UnitIds is null || UnitIds.Count == 0))
            {
                yield return new ValidationResult(
                    "unitIds é obrigatório para usuários não-admin.",
                    [nameof(UnitIds)]);
            }
        }
    }
}
