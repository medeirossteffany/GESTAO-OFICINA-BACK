using GestaoOficina.Entities;
using GestaoOficina.DTOs.Onboarding;
using GestaoOficina.Data;
using Microsoft.AspNetCore.Identity;

namespace GestaoOficina.Features.Onboarding
{
    public class OnboardingService
    {
        private readonly AppDbContext _context;
        public OnboardingService(AppDbContext context)
        {
            _context = context;
        }

        public (Tenant tenant, List<Unit> units, User user) Onboard(OnboardingRequest dto)
        {
            using var transaction = _context.Database.BeginTransaction();

            var tenant = new Tenant
            {
                Name = dto.TenantName,
                Cnpj = dto.TenantCnpj,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            _context.SaveChanges();

            var units = new List<Unit>();
            
            // Criar units apenas se fornecidas
            if (dto.Units != null && dto.Units.Count > 0)
            {
                foreach (var unitDto in dto.Units)
                {
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
                _context.SaveChanges();
            }

            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                TenantId = tenant.Id,
                Name = dto.AdminName,
                Email = dto.AdminEmail,
                Role = UserRole.Admin,
                IsActive = true,
                FullAccess = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, dto.AdminPassword);
            _context.Users.Add(user);
            _context.SaveChanges();

            // Vincular admin a todas as units (se houver)
            foreach (var unit in units)
            {
                var userUnit = new UserUnit
                {
                    UserId = user.Id,
                    UnitId = unit.Id
                };
                _context.UserUnits.Add(userUnit);
            }
            _context.SaveChanges();

            return (tenant, units, user);
        }
    }
}