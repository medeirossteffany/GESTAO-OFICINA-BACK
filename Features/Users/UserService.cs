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
                FullAccess = parsedRole == UserRole.Admin,
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
                    .Where(u => u.TenantId == tenantId && u.IsActive)
                    .ToListAsync();

                foreach (var unit in tenantUnits)
                {
                    _context.UserUnits.Add(new UserUnit
                    {
                        UserId = user.Id,
                        UnitId = unit.Id,
                        IsActive = true
                    });
                }
            }
            else if (dto.UnitIds != null && dto.UnitIds.Count > 0)
            {
                foreach (var unitId in dto.UnitIds)
                {
                    _context.UserUnits.Add(new UserUnit
                    {
                        UserId = user.Id,
                        UnitId = unitId,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GetUserByIdAsync(user.Id) ?? user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserUnits.Where(uu => uu.IsActive))
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<List<User>> GetTenantUsersAsync(int tenantId)
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId && u.IsActive)
                .Include(u => u.UserUnits.Where(uu => uu.IsActive))
                .ToListAsync();
        }

        public async Task<User?> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;

            if (dto.Name is not null)
            {
                user.Name = dto.Name;
            }

            if (dto.Email is not null)
            {
                var targetEmail = dto.Email.Trim();

                if (string.IsNullOrWhiteSpace(targetEmail))
                    throw new InvalidOperationException("Email inválido.");

                if (!string.Equals(user.Email, targetEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUserByEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id != userId && u.Email == targetEmail && u.IsActive);

                    if (existingUserByEmail != null)
                        throw new InvalidOperationException($"O email {targetEmail} já está registrado no sistema.");

                    user.Email = targetEmail;
                    user.UserName = targetEmail;
                    user.NormalizedEmail = _userManager.NormalizeEmail(targetEmail);
                    user.NormalizedUserName = _userManager.NormalizeName(targetEmail);
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(userId);
        }

        public async Task<User?> UpdateUserAsync(int userId, UpdateUserRequest dto)
        {
            var user = await _context.Users
                .Include(u => u.UserUnits) // importante: carregar ativos + inativos
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return null;

            var targetEmail = dto.Email ?? user.Email;
            if (user.Email != targetEmail)
            {
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.Email == targetEmail && u.IsActive);

                if (existingUserByEmail != null)
                {
                    throw new InvalidOperationException($"O email {targetEmail} já está registrado no sistema.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && user.PhoneNumber != dto.PhoneNumber)
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != userId && u.PhoneNumber == dto.PhoneNumber && u.IsActive);

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
            user.FullAccess = parsedRole == UserRole.Admin;

            var shouldUpdateUnits = !string.IsNullOrWhiteSpace(dto.Role) || dto.UnitIds is not null;
            if (shouldUpdateUnits)
            {
                foreach (var existingUnit in user.UserUnits.Where(uu => uu.IsActive))
                {
                    existingUnit.IsActive = false;
                }

                List<int> targetUnitIds = new();

                if (parsedRole == UserRole.Admin)
                {
                    targetUnitIds = await _context.Units
                        .Where(u => u.TenantId == user.TenantId && u.IsActive)
                        .Select(u => u.Id)
                        .ToListAsync();
                }
                else if (dto.UnitIds is { Count: > 0 })
                {
                    targetUnitIds = dto.UnitIds
                        .Distinct()
                        .ToList();
                }

                foreach (var unitId in targetUnitIds)
                {
                    var existingLink = user.UserUnits.FirstOrDefault(uu => uu.UnitId == unitId);
                    if (existingLink is not null)
                    {
                        existingLink.IsActive = true;
                    }
                    else
                    {
                        _context.UserUnits.Add(new UserUnit
                        {
                            UserId = user.Id,
                            UnitId = unitId,
                            IsActive = true
                        });
                    }
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(user.Id);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserUnits)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return false;

            user.IsActive = false;

            var activeUserUnits = user.UserUnits.Where(uu => uu.IsActive).ToList();
            foreach (var userUnit in activeUserUnits)
            {
                userUnit.IsActive = false;
            }

            _context.Users.Update(user);
            if (activeUserUnits.Count > 0)
            {
                _context.UserUnits.UpdateRange(activeUserUnits);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
