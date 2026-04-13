using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestaoOficina.DTOs.Tenants;
using GestaoOficina.Features.Tenants;
using GestaoOficina.Entities;
using System.Security.Claims;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly TenantService _service;
        public TenantsController(TenantService service)
        {
            _service = service;
        }

        [HttpPost]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<ActionResult<Tenant>> CreateTenant(CreateTenantRequest dto)
        {
            var tenant = await _service.CreateTenant(dto);
            return CreatedAtAction(nameof(GetMyTenant), tenant);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetMyTenant()
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!int.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized();

            var tenant = await _service.GetTenant(tenantId);
            if (tenant == null)
                return NotFound();

            return Ok(new
            {
                tenant.Name,
                tenant.Plan,
                PlanRenewalDate = tenant.PlanRenewalDate, // Agora retorna da entidade Tenant
                Cnpj = tenant.Unit?.Cnpj,
                tenant.CreatedAt
            });
        }

        [HttpPatch]
        [Authorize(Roles = "Admin")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<ActionResult> UpdateTenant(UpdateTenantRequest dto)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!int.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized();

            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            if (!fullAccess)
                return Forbid();

            var tenant = await _service.UpdateTenant(tenantId, dto);
            if (tenant == null)
                return NotFound();

            return Ok(new
            {
                tenant.Name,
                tenant.Plan,
                Cnpj = tenant.Unit?.Cnpj,
                tenant.CreatedAt
            });
        }

        [HttpPatch("upgrade-plan")]
        [Authorize(Roles = "Admin")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<ActionResult> UpgradeTenantPlan(UpgradeTenantPlanRequest dto)
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (!int.TryParse(tenantIdClaim, out var tenantId))
                return Unauthorized();

            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            if (!fullAccess)
                return Forbid();

            try
            {
                var tenant = await _service.UpgradeTenantPlan(tenantId, dto.Plan);
                if (tenant == null)
                    return NotFound();

                return Ok(new
                {
                    Message = "Plano atualizado com sucesso.",
                    tenant.Name,
                    tenant.Plan,
                    Cnpj = tenant.Unit?.Cnpj,
                    tenant.CreatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
