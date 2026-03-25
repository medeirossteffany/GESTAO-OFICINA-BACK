using GestaoOficina.Entities;
using GestaoOficina.DTOs.Onboarding;
using GestaoOficina.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Onboarding
{
    public class OnboardingService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public OnboardingService(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<(Tenant tenant, Unit? unit, User user)> OnboardAsync(OnboardingRequest dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var existingUserByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.AdminEmail);

            if (existingUserByEmail != null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"O email {dto.AdminEmail} já está registrado no sistema.");
            }

            var existingUserByPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.AdminPhoneNumber);

            if (existingUserByPhone != null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"O telefone {dto.AdminPhoneNumber} já está registrado no sistema.");
            }

            var tenant = new Tenant
            {
                Name = dto.TenantName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            Unit? unit = null;

            if (dto.Unit != null)
            {
                var unitInOtherTenant = await _context.Units
                    .FirstOrDefaultAsync(u => u.Cnpj == dto.Unit.Cnpj);

                if (unitInOtherTenant != null)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"O CNPJ {dto.Unit.Cnpj} já está sendo utilizado por outra Unit.");
                }

                unit = new Unit
                {
                    TenantId = tenant.Id,
                    Name = dto.Unit.Name,
                    Cnpj = dto.Unit.Cnpj,
                    AddressZip = dto.Unit.AddressZip,
                    AddressStreet = dto.Unit.AddressStreet,
                    AddressNumber = dto.Unit.AddressNumber,
                    AddressDistrict = dto.Unit.AddressDistrict,
                    AddressCity = dto.Unit.AddressCity,
                    AddressState = dto.Unit.AddressState,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Units.Add(unit);
                await _context.SaveChangesAsync();

                tenant.UnitId = unit.Id;
                _context.Tenants.Update(tenant);
                await _context.SaveChangesAsync();
            }

            var user = new User
            {
                TenantId = tenant.Id,
                UserName = dto.AdminEmail,
                Name = dto.AdminName,
                Email = dto.AdminEmail,
                PhoneNumber = dto.AdminPhoneNumber,
                Role = UserRole.Admin,
                IsActive = true,
                FullAccess = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.AdminPassword);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Erro ao criar usuário: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            if (unit != null)
            {
                _context.UserUnits.Add(new UserUnit
                {
                    UserId = user.Id,
                    UnitId = unit.Id
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (tenant, unit, user);
        }
    }
}
