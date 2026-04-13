using System.Security.Claims;
using GestaoOficina.DTOs.Customers;
using GestaoOficina.Entities;
using GestaoOficina.Features.Customers;
using GestaoOficina.Features.Tenants;
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
        public async Task<ActionResult<List<CustomerResponse>>> GetCustomers([FromQuery] int? unitId)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            if (unitId.HasValue && !fullAccess && !unitIds.Contains(unitId.Value))
                return Forbid();

            var customers = await _service.GetCustomersByTenantAndUnits(loggedTenantId, unitIds, fullAccess, unitId);
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

        [HttpGet("by-document/{cpfCnpj}")]
        public async Task<ActionResult<CustomerResponse>> GetCustomerByCpfCnpj(string cpfCnpj)
        {
            if (string.IsNullOrWhiteSpace(cpfCnpj))
                return BadRequest("CPF/CNPJ é obrigatório.");

            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));

            var customer = await _service.GetCustomerByCpfCnpj(loggedTenantId, cpfCnpj);
            if (customer == null) return NotFound();

            return Ok(ToResponse(customer));
        }

        [HttpPost]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            if ((dto.UnitId.HasValue && dto.UnitId.Value <= 0) || (dto.UnitIds?.Any(id => id <= 0) ?? false))
                return BadRequest("unitId/unitIds inválido(s).");

            var targetUnitIds = new List<int>();
            if (dto.UnitId.HasValue) targetUnitIds.Add(dto.UnitId.Value);
            if (dto.UnitIds is { Count: > 0 }) targetUnitIds.AddRange(dto.UnitIds);
            targetUnitIds = targetUnitIds.Distinct().ToList();

            if (targetUnitIds.Count == 0)
                return BadRequest("Informe ao menos uma loja em unitId ou unitIds.");

            if (!fullAccess && targetUnitIds.Any(id => !unitIds.Contains(id)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Usuário não possui acesso às lojas informadas.",
                    allowedUnitIds = unitIds,
                    requestedUnitIds = targetUnitIds
                });
            }

            var (customer, createdNew) = await _service.CreateOrLinkCustomer(dto, loggedTenantId, targetUnitIds);
            var response = ToResponse(customer);

            if (createdNew)
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);

            return Ok(response);
        }

        [HttpPatch("{id}")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<ActionResult<CustomerResponse>> UpdateCustomer(int id, UpdateCustomerRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var unitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customer = await _service.GetCustomerById(id);
            if (customer == null) return NotFound();

            var hasAccess = _service.HasAccessToCustomer(customer, loggedTenantId, unitIds, fullAccess);
            if (!hasAccess) return Forbid();

            List<int>? targetUnitIds = null;
            var hasUnitInput = dto.UnitId.HasValue || (dto.UnitIds is { Count: > 0 });

            if (hasUnitInput)
            {
                if ((dto.UnitId.HasValue && dto.UnitId.Value <= 0) || (dto.UnitIds?.Any(x => x <= 0) ?? false))
                    return BadRequest("unitId/unitIds inválido(s).");

                targetUnitIds = new List<int>();
                if (dto.UnitId.HasValue) targetUnitIds.Add(dto.UnitId.Value);
                if (dto.UnitIds is { Count: > 0 }) targetUnitIds.AddRange(dto.UnitIds);

                targetUnitIds = targetUnitIds.Distinct().ToList();

                if (!fullAccess && targetUnitIds.Any(x => !unitIds.Contains(x)))
                    return Forbid();
            }

            var updatedCustomer = await _service.UpdateCustomer(id, dto, loggedTenantId, targetUnitIds);
            return Ok(ToResponse(updatedCustomer));
        }

        [HttpDelete("{id}")]
        [ServiceFilter(typeof(RequireActivePlanAttribute))]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            var fullAccess = bool.Parse(User.FindFirstValue("FullAccess") ?? "false");
            var allowedUnitIds = User.FindAll("UnitId").Select(c => int.Parse(c.Value)).ToList();

            var customer = await _service.GetCustomerById(id);
            if (customer == null) return NotFound();

            var hasAccess = _service.HasAccessToCustomer(customer, loggedTenantId, allowedUnitIds, fullAccess);
            if (!hasAccess) return Forbid();

            var success = await _service.DeleteCustomerLinksOrCustomer(id, loggedTenantId, allowedUnitIds, fullAccess);
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
