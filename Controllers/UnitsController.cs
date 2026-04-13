using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestaoOficina.DTOs.Units;
using GestaoOficina.Features.Units;
using GestaoOficina.Features.Tenants;
using System.Security.Claims;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitsController : ControllerBase
    {
        private readonly UnitService _service;
        public UnitsController(UnitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<UnitResponse>>> GetUnits()
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var units = await _service.GetUnitsByTenant(loggedTenantId);
            var response = units.Select(u => new UnitResponse
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Name = u.Name,
                Cnpj = u.Cnpj,
                AddressZip = u.AddressZip,
                AddressStreet = u.AddressStreet,
                AddressNumber = u.AddressNumber,
                AddressDistrict = u.AddressDistrict,
                AddressCity = u.AddressCity,
                AddressState = u.AddressState,
                CreatedAt = u.CreatedAt,
                Email = u.Email,
                Phone = u.Phone
            }).ToList();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UnitResponse>> GetUnit(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var unit = await _service.GetUnitById(id);
            if (unit == null) return NotFound();
            if (unit.TenantId != loggedTenantId) return Forbid();

            var response = new UnitResponse
            {
                Id = unit.Id,
                TenantId = unit.TenantId,
                Name = unit.Name,
                Cnpj = unit.Cnpj,
                AddressZip = unit.AddressZip,
                AddressStreet = unit.AddressStreet,
                AddressNumber = unit.AddressNumber,
                AddressDistrict = unit.AddressDistrict,
                AddressCity = unit.AddressCity,
                AddressState = unit.AddressState,
                CreatedAt = unit.CreatedAt,
                Email = unit.Email,
                Phone = unit.Phone
            };
            return Ok(response);
        }

        [HttpPost]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UnitResponse>> CreateUnit(CreateUnitRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");

            if (!fullAccess) return Forbid();

            var unit = await _service.CreateUnit(dto, loggedTenantId);
            var response = new UnitResponse
            {
                Id = unit.Id,
                TenantId = unit.TenantId,
                Name = unit.Name,
                Cnpj = unit.Cnpj,
                AddressZip = unit.AddressZip,
                AddressStreet = unit.AddressStreet,
                AddressNumber = unit.AddressNumber,
                AddressDistrict = unit.AddressDistrict,
                AddressCity = unit.AddressCity,
                AddressState = unit.AddressState,
                CreatedAt = unit.CreatedAt,
                Email = unit.Email,
                Phone = unit.Phone
            };
            return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, response);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UnitResponse>> UpdateUnit(int id, UpdateUnitRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");

            var unit = await _service.GetUnitById(id);
            if (unit == null) return NotFound();
            if (unit.TenantId != loggedTenantId) return Forbid();
            if (!fullAccess) return Forbid();

            var updatedUnit = await _service.UpdateUnit(id, dto);
            var response = new UnitResponse
            {
                Id = updatedUnit.Id,
                TenantId = updatedUnit.TenantId,
                Name = updatedUnit.Name,
                Cnpj = updatedUnit.Cnpj,
                AddressZip = updatedUnit.AddressZip,
                AddressStreet = updatedUnit.AddressStreet,
                AddressNumber = updatedUnit.AddressNumber,
                AddressDistrict = updatedUnit.AddressDistrict,
                AddressCity = updatedUnit.AddressCity,
                AddressState = updatedUnit.AddressState,
                CreatedAt = updatedUnit.CreatedAt,
                Email = updatedUnit.Email,
                Phone = updatedUnit.Phone
            };
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");

            var unit = await _service.GetUnitById(id);
            if (unit == null) return NotFound();
            if (unit.TenantId != loggedTenantId) return Forbid();
            if (!fullAccess) return Forbid();

            var success = await _service.DeleteUnit(id);
            if (!success) return BadRequest();
            return NoContent();
        }
    }
}
