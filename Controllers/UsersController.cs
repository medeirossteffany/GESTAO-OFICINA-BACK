using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestaoOficina.DTOs.Users;
using GestaoOficina.Features.Users;
using GestaoOficina.Entities;
using System.Security.Claims;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _service;
        public UsersController(UserService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(CreateUserRequest dto)
        {
            var loggedTenantId = int.Parse(User.FindFirstValue("TenantId"));
            if (loggedTenantId != dto.TenantId)
                return Forbid();
            var user = await _service.CreateUserAsync(dto);
            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
        }

        [HttpGet("me")]
        [AllowAnonymous]
        [Authorize]
        public async Task<ActionResult<User>> GetMyProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _service.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("me")]
        [AllowAnonymous]
        [Authorize]
        public async Task<ActionResult<User>> UpdateMyProfile(UpdateUserProfileRequest dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _service.UpdateUserProfileAsync(userId, dto);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }
}