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
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _service;
        public UsersController(UserService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
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
                CpfCnpj = u.CpfCnpj,
                AddressZip = u.AddressZip,
                AddressStreet = u.AddressStreet,
                AddressNumber = u.AddressNumber,
                AddressDistrict = u.AddressDistrict,
                AddressCity = u.AddressCity,
                AddressState = u.AddressState,
                Role = u.Role,
                IsActive = u.IsActive,
                FullAccess = u.FullAccess,
                CreatedAt = u.CreatedAt,
                UnitIds = u.UserUnits?.Select(uu => uu.UnitId).ToList()
            }).ToList();

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
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
                CpfCnpj = user.CpfCnpj,
                AddressZip = user.AddressZip,
                AddressStreet = user.AddressStreet,
                AddressNumber = user.AddressNumber,
                AddressDistrict = user.AddressDistrict,
                AddressCity = user.AddressCity,
                AddressState = user.AddressState,
                Role = user.Role,
                IsActive = user.IsActive,
                FullAccess = user.FullAccess,
                CreatedAt = user.CreatedAt,
                UnitIds = user.UserUnits?.Select(uu => uu.UnitId).ToList()
            };

            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, response);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
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

            var roleToValidate = dto.Role ?? userToUpdate.Role.ToString();
            var isAdmin = Enum.TryParse<UserRole>(roleToValidate, true, out var role) && role == UserRole.Admin;
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
                CpfCnpj = updatedUser.CpfCnpj,
                AddressZip = updatedUser.AddressZip,
                AddressStreet = updatedUser.AddressStreet,
                AddressNumber = updatedUser.AddressNumber,
                AddressDistrict = updatedUser.AddressDistrict,
                AddressCity = updatedUser.AddressCity,
                AddressState = updatedUser.AddressState,
                Role = updatedUser.Role,
                IsActive = updatedUser.IsActive,
                FullAccess = updatedUser.FullAccess,
                CreatedAt = updatedUser.CreatedAt,
                UnitIds = updatedUser.UserUnits?.Select(uu => uu.UnitId).ToList()
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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
        public async Task<ActionResult<UserResponse>> GetMyProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _service.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            var response = new UserResponse
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CpfCnpj = user.CpfCnpj,
                AddressZip = user.AddressZip,
                AddressStreet = user.AddressStreet,
                AddressNumber = user.AddressNumber,
                AddressDistrict = user.AddressDistrict,
                AddressCity = user.AddressCity,
                AddressState = user.AddressState,
                Role = user.Role,
                IsActive = user.IsActive,
                FullAccess = user.FullAccess,
                CreatedAt = user.CreatedAt,
                UnitIds = user.UserUnits?.Where(uu => uu.IsActive).Select(uu => uu.UnitId).ToList()
            };

            return Ok(response);
        }

        [HttpPatch("me")]
        public async Task<ActionResult<UserResponse>> UpdateMyProfile(UpdateUserProfileRequest dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _service.UpdateUserProfileAsync(userId, dto);
            if (user == null)
                return NotFound();

            var response = new UserResponse
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CpfCnpj = user.CpfCnpj,
                AddressZip = user.AddressZip,
                AddressStreet = user.AddressStreet,
                AddressNumber = user.AddressNumber,
                AddressDistrict = user.AddressDistrict,
                AddressCity = user.AddressCity,
                AddressState = user.AddressState,
                Role = user.Role,
                IsActive = user.IsActive,
                FullAccess = user.FullAccess,
                CreatedAt = user.CreatedAt,
                UnitIds = user.UserUnits?.Where(uu => uu.IsActive).Select(uu => uu.UnitId).ToList()
            };

            return Ok(response);
        }
    }
}
