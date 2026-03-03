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
            var (tenant, units, user) = await _service.OnboardAsync(dto);
            return Ok(new 
            { 
                TenantId = tenant.Id, 
                UnitIds = units.Select(u => u.Id).ToList(),
                UserId = user.Id 
            });
        }
    }
}