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

        public async Task<User> CreateUserAsync(CreateUserRequest dto, int tenantId)
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

            var parsedRole = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Comum;

            var user = new User
            {
                TenantId = tenantId,
                UserName = dto.Email,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = parsedRole,
                IsActive = true,
                FullAccess = parsedRole == UserRole.Admin ? true : false,
                CreatedAt = DateTime.UtcNow
            };
            
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                throw new Exception($"Erro ao criar usuário: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Se for Admin, atribui todas as units do tenant
            if (parsedRole == UserRole.Admin)
            {
                var tenantUnits = await _context.Units
                    .Where(u => u.TenantId == tenantId)
                    .ToListAsync();

                foreach (var unit in tenantUnits)
                {
                    var userUnit = new UserUnit
                    {
                        UserId = user.Id,
                        UnitId = unit.Id
                    };
                    _context.UserUnits.Add(userUnit);
                }
            }
            // Se for Comum, atribui apenas as units especificadas
            else if (dto.UnitIds != null && dto.UnitIds.Count > 0)
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
            }

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetTenantUsersAsync(int tenantId)
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId)
                .Include(u => u.UserUnits)
                .ToListAsync();
        }

        public async Task<User?> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

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

        public async Task<User?> UpdateUserAsync(int userId, UpdateUserRequest dto)
        {
            var user = await _context.Users
                .Include(u => u.UserUnits)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return null;

            if (user.Email != dto.Email)
            {
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.Email == dto.Email);
                
                if (existingUserByEmail != null)
                {
                    throw new InvalidOperationException($"O email {dto.Email} já está registrado no sistema.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && user.PhoneNumber != dto.PhoneNumber)
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.PhoneNumber == dto.PhoneNumber);
                
                if (existingUserByPhone != null)
                {
                    throw new InvalidOperationException($"O telefone {dto.PhoneNumber} já está registrado no sistema.");
                }
            }

            var parsedRole = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Comum;

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Role = parsedRole;
            user.IsActive = dto.IsActive;
            user.FullAccess = parsedRole == UserRole.Admin ? true : dto.FullAccess;

            var existingUnits = user.UserUnits.ToList();
            _context.UserUnits.RemoveRange(existingUnits);

            // Se for Admin, atribui todas as units do tenant
            if (parsedRole == UserRole.Admin)
            {
                var tenantUnits = await _context.Units
                    .Where(u => u.TenantId == user.TenantId)
                    .ToListAsync();

                foreach (var unit in tenantUnits)
                {
                    var userUnit = new UserUnit
                    {
                        UserId = user.Id,
                        UnitId = unit.Id
                    };
                    _context.UserUnits.Add(userUnit);
                }
            }
            // Se for Comum, atribui apenas as units especificadas
            else if (dto.UnitIds != null && dto.UnitIds.Count > 0)
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
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}