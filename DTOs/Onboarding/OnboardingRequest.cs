using System.ComponentModel.DataAnnotations;
using GestaoOficina.Entities;

namespace GestaoOficina.DTOs.Onboarding
{
    public class OnboardingRequest
    {
        [Required]
        public string TenantName { get; set; }

        public TenantPlan? Plan { get; set; }

        public CreateUnitDto? Unit { get; set; }

        [Required]
        public string AdminName { get; set; }

        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; }

        [Required]
        public string AdminPhoneNumber { get; set; }

        [Required]
        public string AdminPassword { get; set; }
    }
}
