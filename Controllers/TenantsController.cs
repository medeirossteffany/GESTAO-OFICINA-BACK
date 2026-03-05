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
        public ActionResult<Tenant> CreateTenant(CreateTenantRequest dto)
        {
            var tenant = _service.CreateTenant(dto);
            return CreatedAtAction(nameof(CreateTenant), new { id = tenant.Id }, tenant);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Tenant>> GetTenant(int id)
        {
            var tenant = await _service.GetTenant(id);
            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Tenant>> UpdateTenant(int id, UpdateTenantRequest dto)
        {
           
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            if (loggedTenantId != id)
                return Forbid();

            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            if (!fullAccess)
                return Forbid();

            var tenant = await _service.UpdateTenant(id, dto);
            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }
    }
}