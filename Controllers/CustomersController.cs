using System.Security.Claims;
using GestaoOficina.DTOs.Customers;
using GestaoOficina.Entities;
using GestaoOficina.Features.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _service;

        public CustomersController(CustomerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<CustomerResponse>>> GetCustomers()
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customers = await _service.GetCustomersByTenantAndUnits(loggedTenantId, unitIds, fullAccess);
            return Ok(customers.Select(ToResponse).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customer = await _service.GetCustomerById(id);
            if (customer == null) return NotFound();

            var hasAccess = _service.HasAccessToCustomer(customer, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            return Ok(ToResponse(customer));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            if (!fullAccess && !unitIds.Contains(dto.UnitId)) return Forbid();

            var (customer, createdNew) = await _service.CreateOrLinkCustomer(dto, loggedTenantId);
            var response = ToResponse(customer);

            if (createdNew)
            {
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);
            }

            return Ok(response);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CustomerResponse>> UpdateCustomer(int id, UpdateCustomerRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customer = await _service.GetCustomerById(id);
            if (customer == null) return NotFound();

            var hasAccess = _service.HasAccessToCustomer(customer, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            var updatedCustomer = await _service.UpdateCustomer(id, dto);
            return Ok(ToResponse(updatedCustomer));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int id, [FromQuery] int? unitId, [FromQuery] string? unitIds)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var allowedUnitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var targetUnitIds = new List<int>();

            if (unitId.HasValue)
            {
                targetUnitIds.Add(unitId.Value);
            }

            if (!string.IsNullOrWhiteSpace(unitIds))
            {
                var parts = unitIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (!int.TryParse(part, out var parsed))
                    {
                        return BadRequest("ParÃ¢metro unitIds invÃ¡lido.");
                    }

                    targetUnitIds.Add(parsed);
                }
            }

            targetUnitIds = targetUnitIds.Distinct().ToList();
            if (targetUnitIds.Count == 0)
            {
                return BadRequest("Informe ao menos um unitId/unitIds para remoÃ§Ã£o.");
            }

            if (!fullAccess && targetUnitIds.Any(idToRemove => !allowedUnitIds.Contains(idToRemove))) return Forbid();

            var customer = await _service.GetCustomerById(id);
            if (customer == null) return NotFound();

            var hasAccess = _service.HasAccessToCustomer(customer, loggedTenantId, allowedUnitIds, fullAccess);
            if (!hasAccess) return Forbid();

            var success = await _service.DeleteCustomerLinksOrCustomer(id, targetUnitIds);
            if (!success) return BadRequest();

            return NoContent();
        }

        private static CustomerResponse ToResponse(Customer customer)
        {
            return new CustomerResponse
            {
                Id = customer.Id,
                TenantId = customer.TenantId,
                LegalTypeId = customer.LegalTypeId,
                Name = customer.Name,
                CpfCnpj = customer.CpfCnpj,
                Email = customer.Email,
                Phone = customer.Phone,
                AddressZip = customer.AddressZip,
                AddressStreet = customer.AddressStreet,
                AddressNumber = customer.AddressNumber,
                AddressDistrict = customer.AddressDistrict,
                AddressCity = customer.AddressCity,
                AddressState = customer.AddressState,
                Notes = customer.Notes,
                UnitIds = customer.CustomerUnits.Select(cu => cu.UnitId).ToList(),
                CreatedAt = customer.CreatedAt
            };
        }
    }
}
