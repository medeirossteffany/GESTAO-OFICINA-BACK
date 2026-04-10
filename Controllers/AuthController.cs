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
using GestaoOficina.Features.Auth;

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
        private readonly PasswordResetService _passwordResetService;

        public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IConfiguration config, AppDbContext context, PasswordResetService passwordResetService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config;
            _context = context;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized();

            var userUnits = await _context.UserUnits
                .Where(uu => uu.UserId == user.Id && uu.IsActive && uu.Unit.IsActive)
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

            foreach (var unitId in userUnits)
            {
                claims.Add(new Claim("UnitId", unitId));
            }

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                return StatusCode(500, "Configuração JWT inválida: JWT_KEY deve ter no mínimo 32 caracteres.");
            }

            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _config["Jwt:Issuer"] ?? "GestaoOficina";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound("Usuário com este email não encontrado.");
            await _passwordResetService.GenerateAndSendCodeAsync(dto.Email);
            return Ok();
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest dto)
        {
            var valid = await _passwordResetService.ValidateCodeAsync(dto.Email, dto.Code);
            if (!valid) return BadRequest("Código inválido ou expirado.");
            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("As senhas não coincidem.");
            var success = await _passwordResetService.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword);
            if (!success) return BadRequest("Código inválido, expirado ou usuário não encontrado.");
            return Ok();
        }
    }
}
