using System.ComponentModel.DataAnnotations;
using GestaoOficina.Entities;

namespace GestaoOficina.DTOs.Tenants
{
    public class UpgradeTenantPlanRequest
    {
        [Required]
        public TenantPlan Plan { get; set; }
    }
}
