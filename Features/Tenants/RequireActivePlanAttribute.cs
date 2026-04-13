using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GestaoOficina.Features.Tenants;
using System.Security.Claims;

namespace GestaoOficina.Features.Tenants
{
    public class RequireActivePlanAttribute : TypeFilterAttribute
    {
        public RequireActivePlanAttribute() : base(typeof(RequireActivePlanFilter)) { }
    }

    public class RequireActivePlanFilter : IAsyncActionFilter
    {
        private readonly TenantService _tenantService;
        public RequireActivePlanFilter(TenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var tenantIdClaim = user.FindFirstValue("TenantId");
            if (!int.TryParse(tenantIdClaim, out var tenantId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var tenant = await _tenantService.GetTenant(tenantId);
            if (tenant == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var now = DateTime.UtcNow;
            if (tenant.PlanRenewalDate < now)
            {
                context.Result = new ObjectResult(new { message = "Plano expirado. Renove para continuar usando o sistema." })
                {
                    StatusCode = 402 // Payment Required
                };
                return;
            }

            await next();
        }
    }
}
