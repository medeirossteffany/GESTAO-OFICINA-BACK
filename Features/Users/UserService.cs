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
                CpfCnpj = dto.CpfCnpj,
                AddressZip = dto.AddressZip,
                AddressStreet = dto.AddressStreet,
                AddressNumber = dto.AddressNumber,
                AddressDistrict = dto.AddressDistrict,
                AddressCity = dto.AddressCity,
                AddressState = dto.AddressState,
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

            if (dto.Name is not null) user.Name = dto.Name;
            if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.CpfCnpj is not null) user.CpfCnpj = dto.CpfCnpj;
            if (dto.AddressZip is not null) user.AddressZip = dto.AddressZip;
            if (dto.AddressStreet is not null) user.AddressStreet = dto.AddressStreet;
            if (dto.AddressNumber is not null) user.AddressNumber = dto.AddressNumber;
            if (dto.AddressDistrict is not null) user.AddressDistrict = dto.AddressDistrict;
            if (dto.AddressCity is not null) user.AddressCity = dto.AddressCity;
            if (dto.AddressState is not null) user.AddressState = dto.AddressState;

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

            var targetEmail = dto.Email ?? user.Email;
            if (user.Email != targetEmail)
            {
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.Email == targetEmail);

                if (existingUserByEmail != null)
                {
                    throw new InvalidOperationException($"O email {targetEmail} já está registrado no sistema.");
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

            var parsedRole = user.Role;
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                parsedRole = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Comum;
            }

            if (dto.Name is not null) user.Name = dto.Name;
            user.Email = targetEmail;
            user.UserName = targetEmail;
            if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.CpfCnpj is not null) user.CpfCnpj = dto.CpfCnpj;
            if (dto.AddressZip is not null) user.AddressZip = dto.AddressZip;
            if (dto.AddressStreet is not null) user.AddressStreet = dto.AddressStreet;
            if (dto.AddressNumber is not null) user.AddressNumber = dto.AddressNumber;
            if (dto.AddressDistrict is not null) user.AddressDistrict = dto.AddressDistrict;
            if (dto.AddressCity is not null) user.AddressCity = dto.AddressCity;
            if (dto.AddressState is not null) user.AddressState = dto.AddressState;
            user.Role = parsedRole;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            user.FullAccess = parsedRole == UserRole.Admin ? true : (dto.FullAccess ?? user.FullAccess);

            var shouldUpdateUnits = !string.IsNullOrWhiteSpace(dto.Role) || dto.UnitIds is not null;
            if (shouldUpdateUnits)
            {
                var existingUnits = user.UserUnits.ToList();
                _context.UserUnits.RemoveRange(existingUnits);

                if (parsedRole == UserRole.Admin)
                {
                    var tenantUnits = await _context.Units
                        .Where(u => u.TenantId == user.TenantId)
                        .ToListAsync();

                    foreach (var unit in tenantUnits)
                    {
                        _context.UserUnits.Add(new UserUnit
                        {
                            UserId = user.Id,
                            UnitId = unit.Id
                        });
                    }
                }
                else if (dto.UnitIds is { Count: > 0 })
                {
                    foreach (var unitId in dto.UnitIds)
                    {
                        _context.UserUnits.Add(new UserUnit
                        {
                            UserId = user.Id,
                            UnitId = unitId
                        });
                    }
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
