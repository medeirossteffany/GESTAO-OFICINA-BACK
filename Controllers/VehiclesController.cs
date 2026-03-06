using System.Security.Claims;
using GestaoOficina.DTOs.Vehicles;
using GestaoOficina.Entities;
using GestaoOficina.Features.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly VehicleService _service;

        public VehiclesController(VehicleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<VehicleResponse>>> GetVehicles()
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var vehicles = await _service.GetVehiclesByTenantAndUnits(loggedTenantId, unitIds, fullAccess);
            return Ok(vehicles.Select(ToResponse).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleResponse>> GetVehicle(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var vehicle = await _service.GetVehicleById(id);
            if (vehicle == null) return NotFound();

            var hasAccess = _service.HasAccessToVehicle(vehicle, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            return Ok(ToResponse(vehicle));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VehicleResponse>> CreateVehicle(CreateVehicleRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customer = await _service.GetCustomerById(dto.CustomerId, loggedTenantId);
            if (customer == null) return BadRequest("Cliente inválido para o tenant informado.");

            var hasAccessToCustomer = _service.HasAccessToCustomer(customer, loggedTenantId, unitIds, fullAccess);
            if (!hasAccessToCustomer) return Forbid();

            var vehicle = await _service.CreateVehicle(dto, loggedTenantId);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, ToResponse(vehicle));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VehicleResponse>> UpdateVehicle(int id, UpdateVehicleRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var vehicle = await _service.GetVehicleById(id);
            if (vehicle == null) return NotFound();

            var hasAccess = _service.HasAccessToVehicle(vehicle, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            var targetCustomer = await _service.GetCustomerById(dto.CustomerId, loggedTenantId);
            if (targetCustomer == null) return BadRequest("Cliente inválido para o tenant informado.");

            var hasAccessToTargetCustomer = _service.HasAccessToCustomer(targetCustomer, loggedTenantId, unitIds, fullAccess);
            if (!hasAccessToTargetCustomer) return Forbid();

            var updatedVehicle = await _service.UpdateVehicle(id, dto, loggedTenantId);
            if (updatedVehicle == null) return NotFound();

            return Ok(ToResponse(updatedVehicle));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var vehicle = await _service.GetVehicleById(id);
            if (vehicle == null) return NotFound();

            var hasAccess = _service.HasAccessToVehicle(vehicle, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            var success = await _service.DeleteVehicle(id);
            if (!success) return BadRequest();

            return NoContent();
        }

        private static VehicleResponse ToResponse(Vehicle vehicle)
        {
            return new VehicleResponse
            {
                Id = vehicle.Id,
                TenantId = vehicle.TenantId,
                CustomerId = vehicle.CustomerId,
                Plate = vehicle.Plate,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color,
                Vin = vehicle.Vin,
                Renavam = vehicle.Renavam,
                InsuranceClaimNumber = vehicle.InsuranceClaimNumber,
                Notes = vehicle.Notes,
                CreatedAt = vehicle.CreatedAt
            };
        }
    }
}
