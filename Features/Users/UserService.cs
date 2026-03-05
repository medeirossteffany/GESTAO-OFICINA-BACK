using GestaoOficina.Entities;
using GestaoOficina.DTOs.Users;
using GestaoOficina.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            var existingUserByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);
            
            if (existingUserByEmail != null)
            {
                throw new InvalidOperationException($"O email {dto.Email} já está registrado no sistema.");
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
                
                if (existingUserByPhone != null)
                {
                    throw new InvalidOperationException($"O telefone {dto.PhoneNumber} já está registrado no sistema.");
                }
            }

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

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // Validar se PhoneNumber mudou e já existe em outro usuário
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && user.PhoneNumber != dto.PhoneNumber)
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.PhoneNumber == dto.PhoneNumber);
                
                if (existingUserByPhone != null)
                {
                    throw new InvalidOperationException($"O telefone {dto.PhoneNumber} já está registrado no sistema.");
                }
            }

            user.Name = dto.Name;
            user.PhoneNumber = dto.PhoneNumber;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}