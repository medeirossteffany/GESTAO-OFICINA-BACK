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

        [HttpGet]
        public async Task<ActionResult<List<UserResponse>>> GetAllUsers()
        {
            var loggedUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(loggedUserIdStr, out var loggedUserId))
                return Unauthorized();

            var loggedUser = await _service.GetUserByIdAsync(loggedUserId);
            if (loggedUser == null || !loggedUser.FullAccess)
                return Forbid();

            var users = await _service.GetTenantUsersAsync(loggedUser.TenantId);
            var response = users.Select(u => new UserResponse
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Name = u.Name,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsActive = u.IsActive,
                FullAccess = u.FullAccess,
                CreatedAt = u.CreatedAt,
                UnitIds = u.UserUnits?.Select(uu => uu.UnitId).ToList()
            }).ToList();

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest dto)
        {
            var loggedUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(loggedUserIdStr, out var loggedUserId))
                return Unauthorized();

            var tenantIdStr = User.FindFirstValue("TenantId");
            if (!int.TryParse(tenantIdStr, out var tenantId))
                return Unauthorized();

            var loggedUser = await _service.GetUserByIdAsync(loggedUserId);
            if (loggedUser == null || !loggedUser.FullAccess)
                return Forbid();

            // Validação: Admins são atribuídos a todas as units automaticamente
            var isAdmin = Enum.TryParse<UserRole>(dto.Role, true, out var role) && role == UserRole.Admin;
            if (isAdmin && dto.UnitIds != null && dto.UnitIds.Count > 0)
                return BadRequest("Usuários admin são atribuidos a todas as units automaticamente. Não é possível especificar unidades individuais.");

            var user = await _service.CreateUserAsync(dto, tenantId);
            var response = new UserResponse
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                FullAccess = user.FullAccess,
                CreatedAt = user.CreatedAt,
                UnitIds = user.UserUnits?.Select(uu => uu.UnitId).ToList()
            };

            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(int id, UpdateUserRequest dto)
        {
            var loggedUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(loggedUserIdStr, out var loggedUserId))
                return Unauthorized();

            var loggedUser = await _service.GetUserByIdAsync(loggedUserId);
            if (loggedUser == null || !loggedUser.FullAccess)
                return Forbid();

            var userToUpdate = await _service.GetUserByIdAsync(id);
            if (userToUpdate == null)
                return NotFound();

            if (loggedUser.TenantId != userToUpdate.TenantId)
                return Forbid();

            // Validação: Admins são atribuídos a todas as units automaticamente
            var isAdmin = Enum.TryParse<UserRole>(dto.Role, true, out var role) && role == UserRole.Admin;
            if (isAdmin && dto.UnitIds != null && dto.UnitIds.Count > 0)
                return BadRequest("Usuários admin são atribuidos a todas as units automaticamente. Não é possível especificar unidades individuais.");

            var updatedUser = await _service.UpdateUserAsync(id, dto);
            if (updatedUser == null)
                return NotFound();

            var response = new UserResponse
            {
                Id = updatedUser.Id,
                TenantId = updatedUser.TenantId,
                Name = updatedUser.Name,
                Email = updatedUser.Email,
                PhoneNumber = updatedUser.PhoneNumber,
                Role = updatedUser.Role,
                IsActive = updatedUser.IsActive,
                FullAccess = updatedUser.FullAccess,
                CreatedAt = updatedUser.CreatedAt,
                UnitIds = updatedUser.UserUnits?.Select(uu => uu.UnitId).ToList()
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var loggedUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(loggedUserIdStr, out var loggedUserId))
                return Unauthorized();

            var loggedUser = await _service.GetUserByIdAsync(loggedUserId);
            if (loggedUser == null || !loggedUser.FullAccess)
                return Forbid();

            var userToDelete = await _service.GetUserByIdAsync(id);
            if (userToDelete == null)
                return NotFound();

            if (loggedUser.TenantId != userToDelete.TenantId)
                return Forbid();

            var success = await _service.DeleteUserAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
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