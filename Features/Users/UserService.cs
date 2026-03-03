using GestaoOficina.Entities;
using GestaoOficina.DTOs.Users;
using GestaoOficina.Data;
using Microsoft.AspNetCore.Identity;

namespace GestaoOficina.Features.Users
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        
        public UserService(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<User> CreateUserAsync(CreateUserRequest dto)
        {
            var user = new User
            {
                TenantId = dto.TenantId,
                UserName = dto.Email,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Comum,
                IsActive = true,
                FullAccess = false,
                CreatedAt = DateTime.UtcNow
            };
            
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                throw new Exception($"Erro ao criar usuário: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            if (dto.UnitIds != null && dto.UnitIds.Count > 0)
            {
                foreach (var unitId in dto.UnitIds)
                {
                    var userUnit = new UserUnit
                    {
                        UserId = user.Id,
                        UnitId = unitId
                    };
                    _context.UserUnits.Add(userUnit);
                }
                await _context.SaveChangesAsync();
            }

            return user;
        }
    }
}