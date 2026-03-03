using Microsoft.AspNetCore.Mvc;
using GestaoOficina.DTOs.Tenants;
using GestaoOficina.Features.Tenants;
using GestaoOficina.Entities;

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
    }
}