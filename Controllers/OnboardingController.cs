using Microsoft.AspNetCore.Mvc;
using GestaoOficina.DTOs.Onboarding;
using GestaoOficina.Features.Onboarding;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly OnboardingService _service;
        public OnboardingController(OnboardingService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Onboard(OnboardingRequest dto)
        {
            var (tenant, unit, user) = await _service.OnboardAsync(dto);
            return Ok(new
            {
                TenantId = tenant.Id,
                UnitId = unit.Id,
                UserId = user.Id
            });
        }
    }
}
