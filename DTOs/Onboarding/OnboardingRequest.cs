using System.Collections.Generic;

namespace GestaoOficina.DTOs.Onboarding
{
    public class OnboardingRequest
    {
        public string TenantName { get; set; }
        public string? TenantCnpj { get; set; }
        public List<CreateUnitDto>? Units { get; set; }
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminPassword { get; set; }
    }
}