using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GestaoOficina.Entities;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using GestaoOficina.DTOs.Auth;
using GestaoOficina.Data;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IConfiguration config, AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized();

            // Carregar as units do usuário
            var userUnits = await _context.UserUnits
                .Where(uu => uu.UserId == user.Id)
                .Select(uu => uu.UnitId.ToString())
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("TenantId", user.TenantId.ToString()),
                new Claim("FullAccess", user.FullAccess.ToString())
            };

            // Adicionar units ao token
            foreach (var unitId in userUnits)
            {
                claims.Add(new Claim("UnitId", unitId));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "sua-chave-secreta-bem-grande"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "GestaoOficina",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}