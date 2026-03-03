using GestaoOficina.Entities;
using GestaoOficina.DTOs.Users;
using GestaoOficina.Data;
using Microsoft.AspNetCore.Identity;

namespace GestaoOficina.Features.Users
{
    public class UserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public User CreateUser(CreateUserRequest dto)
        {
            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                TenantId = dto.TenantId,
                Name = dto.Name,
                Email = dto.Email,
                Role = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Comum,
                IsActive = true,
                FullAccess = false,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);
            _context.SaveChanges();

            // Se UnitId foi fornecido no DTO, vincular à unit
            if (dto.UnitId.HasValue)
            {
                var userUnit = new UserUnit
                {
                    UserId = user.Id,
                    UnitId = dto.UnitId.Value
                };
                _context.UserUnits.Add(userUnit);
                _context.SaveChanges();
            }

            return user;
        }
    }
}