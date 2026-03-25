using System.Security.Claims;
using GestaoOficina.DTOs.ServiceOrders;
using GestaoOficina.Entities;
using GestaoOficina.Features.ServiceOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceOrdersController : ControllerBase
    {
        private readonly ServiceOrderService _service;

        public ServiceOrdersController(ServiceOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceOrders()
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var serviceOrders = await _service.GetServiceOrdersByTenantAndUnits(loggedTenantId, unitIds, fullAccess);
            return Ok(serviceOrders.Select(ToResponse));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateServiceOrder([FromBody] CreateServiceOrderRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            if (!fullAccess && !unitIds.Contains(dto.UnitId))
                return Forbid();

            var created = await _service.CreateServiceOrder(dto, loggedTenantId, unitIds, fullAccess);

            return CreatedAtAction(nameof(GetServiceOrderById), new { id = created.Id }, ToResponse(created));
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateServiceOrder(int id, [FromBody] UpdateServiceOrderRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var existing = await _service.GetServiceOrderById(id);
            if (existing == null) return NotFound();
            if (!_service.HasAccess(existing, loggedTenantId, unitIds, fullAccess)) return Forbid();
            if (dto.UnitId.HasValue && !fullAccess && !unitIds.Contains(dto.UnitId.Value)) return Forbid();

            var updated = await _service.UpdateServiceOrder(id, dto, loggedTenantId, unitIds, fullAccess);
            if (updated == null) return NotFound();

            return Ok(ToResponse(updated));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceOrderById(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var serviceOrder = await _service.GetServiceOrderById(id);
            if (serviceOrder == null) return NotFound();
            if (!_service.HasAccess(serviceOrder, loggedTenantId, unitIds, fullAccess)) return Forbid();

            return Ok(ToResponse(serviceOrder));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteServiceOrder(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var serviceOrder = await _service.GetServiceOrderById(id);
            if (serviceOrder == null) return NotFound();
            if (!_service.HasAccess(serviceOrder, loggedTenantId, unitIds, fullAccess)) return Forbid();

            var deleted = await _service.DeleteServiceOrder(id, loggedTenantId, unitIds, fullAccess);
            if (!deleted) return NotFound();

            return NoContent();
        }

        private static object ToResponse(ServiceOrder so)
        {
            return new
            {
                so.Id,
                so.TenantId,
                so.UnitId,
                UnitName = so.Unit?.Name,
                so.VehicleId,
                VehiclePlate = so.Vehicle?.Plate,
                so.OwnerCustomerId,
                OwnerCustomerName = so.OwnerCustomer?.Name,
                so.StatusId,
                StatusCode = so.Status?.Code,
                StatusName = so.Status?.Name,
                so.EntryDate,
                so.EstimatedDeliveryDate,
                so.DeliveryDate,
                so.BodyworkDescription,
                so.BodyworkValue,
                so.PaintDescription,
                so.PaintValue,
                so.PartsValue,
                so.TotalDiscount,
                so.TotalAmount,
                so.CreatedAt,
                so.UpdatedAt
            };
        }
    }
}
