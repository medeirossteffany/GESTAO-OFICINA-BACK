using System.Security.Claims;
using GestaoOficina.DTOs.ServiceOrders;
using GestaoOficina.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestaoOficina.Features.ServiceOrders;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceOrdersController : ControllerBase
    {
        private readonly ServiceOrderService _service;
        private readonly ServiceOrderPdfService _pdfService;
        private readonly ServiceOrderExcelService _excelService;

        public ServiceOrdersController(
            ServiceOrderService service,
            ServiceOrderPdfService pdfService,
            ServiceOrderExcelService excelService)
        {
            _service = service;
            _pdfService = pdfService;
            _excelService = excelService;
        }

        [HttpPost("import/excel/resumo")]
        public async Task<IActionResult> ImportExcelResumoPorLoja([FromForm(Name = "file")] IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Arquivo não enviado." });

            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            try
            {
                var result = await _excelService.ParseExcelSummaryByStore(file, loggedTenantId, unitIds, fullAccess);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceOrders([FromQuery] int? unitId)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            if (unitId.HasValue && unitId.Value <= 0)
                return BadRequest(new { message = "unitId inválido." });

            if (unitId.HasValue && !fullAccess && !unitIds.Contains(unitId.Value))
                return Forbid();

            var serviceOrders = await _service.GetServiceOrdersByTenantAndUnits(
                loggedTenantId,
                unitIds,
                fullAccess,
                unitId);

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

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DownloadServiceOrderPdf(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var serviceOrder = await _service.GetServiceOrderById(id);
            if (serviceOrder == null) return NotFound();
            if (!_service.HasAccess(serviceOrder, loggedTenantId, unitIds, fullAccess)) return Forbid();

            var pdfBytes = await _pdfService.GenerateAsync(serviceOrder);
            return File(pdfBytes, "application/pdf", $"os-{serviceOrder.Id}.pdf");
        }

        [HttpPatch("{id}/status/enviado")]
        [Authorize]
        public async Task<IActionResult> MarkAsEnviado(int id)
        {
            return await ChangeStatus(id, "ENVIADO");
        }

        [HttpPatch("{id}/status/feito")]
        [Authorize]
        public async Task<IActionResult> MarkAsFeito(int id)
        {
            return await ChangeStatus(id, "FEITO");
        }

        [HttpPatch("{id}/status/finalizado")]
        [Authorize]
        public async Task<IActionResult> MarkAsFinalizado(int id)
        {
            return await ChangeStatus(id, "FINALIZADO");
        }

        private async Task<IActionResult> ChangeStatus(int id, string statusCode)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            try
            {
                var updated = await _service.ChangeStatus(id, statusCode, loggedTenantId, unitIds, fullAccess);
                if (updated == null) return NotFound();

                return Ok(ToResponse(updated));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                so.TotalAmount,
                so.CreatedAt,
                so.UpdatedAt,
                Parts = so.Parts
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        p.Id,
                        p.Description,
                        p.Quantity,
                        p.UnitPrice,
                        p.TotalPrice
                    })
                    .ToList(),
                so.MechanicsDescription,
                so.MechanicsValue,
            };
        }
    }
}
