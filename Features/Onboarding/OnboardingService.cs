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

        public async Task<(Tenant tenant, List<Unit> units, User user)> OnboardAsync(OnboardingRequest dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();


            if (!string.IsNullOrWhiteSpace(dto.TenantCnpj))
            {
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Cnpj == dto.TenantCnpj);
                
                if (existingTenant != null)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Já existe um Tenant com o CNPJ {dto.TenantCnpj}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.AdminEmail))
            {
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.AdminEmail);
                
                if (existingUserByEmail != null)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"O email {dto.AdminEmail} já está registrado no sistema.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.AdminPhoneNumber))
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == dto.AdminPhoneNumber);
                
                if (existingUserByPhone != null)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"O telefone {dto.AdminPhoneNumber} já está registrado no sistema.");
                }
            }

            var tenant = new Tenant
            {
                Name = dto.TenantName,
                Cnpj = dto.TenantCnpj,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var units = new List<Unit>();
            
            if (dto.Units != null && dto.Units.Count > 0)
            {
                foreach (var unitDto in dto.Units)
                {
            
                    if (!string.IsNullOrWhiteSpace(unitDto.Cnpj))
                    {
                        var unitInOtherTenant = await _context.Units
                            .FirstOrDefaultAsync(u => u.TenantId != tenant.Id && u.Cnpj == unitDto.Cnpj);
                        
                        if (unitInOtherTenant != null)
                        {
                            await transaction.RollbackAsync();
                            throw new InvalidOperationException($"O CNPJ {unitDto.Cnpj} já está sendo utilizado por outra Unit de outro Tenant.");
                        }
                    }

                    var unit = new Unit
                    {
                        TenantId = tenant.Id,
                        Name = unitDto.Name,
                        Cnpj = unitDto.Cnpj,
                        AddressZip = unitDto.AddressZip,
                        AddressStreet = unitDto.AddressStreet,
                        AddressNumber = unitDto.AddressNumber,
                        AddressDistrict = unitDto.AddressDistrict,
                        AddressCity = unitDto.AddressCity,
                        AddressState = unitDto.AddressState,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Units.Add(unit);
                    units.Add(unit);
                }
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

            foreach (var unit in units)
            {
                var userUnit = new UserUnit
                {
                    UserId = user.Id,
                    UnitId = unit.Id
                };
                _context.UserUnits.Add(userUnit);
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return (tenant, units, user);
        }
    }
}