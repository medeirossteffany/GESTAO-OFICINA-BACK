using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace GestaoOficina.Features.Auth
{
    public class PasswordResetService
    {
        private readonly Data.AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        public PasswordResetService(Data.AppDbContext db, IConfiguration config, UserManager<User> userManager)
        {
            _db = db;
            _config = config;
            _userManager = userManager;
        }

        public async Task GenerateAndSendCodeAsync(string email)
        {
            var code = new Random().Next(10000, 99999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(10);
            var entity = new PasswordResetCode
            {
                Email = email,
                Code = code,
                Expiration = expiration,
                Used = false
            };
            _db.PasswordResetCodes.Add(entity);
            await _db.SaveChangesAsync();
            await SendEmailAsync(email, code);
        }

        public async Task<bool> ValidateCodeAsync(string email, string code)
        {
            var entity = await _db.PasswordResetCodes
                .Where(x => x.Email == email && x.Code == code && !x.Used && x.Expiration > DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            return entity != null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var entity = await _db.PasswordResetCodes
                .Where(x => x.Email == email && x.Code == code && !x.Used && x.Expiration > DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            if (entity == null) return false;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) return false;
            entity.Used = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task SendEmailAsync(string email, string code)
        {
            var smtpHost = _config["Smtp:Host"] ?? "localhost";
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "1025");
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"] ?? "no-reply@gestaooficina.com";
            using var client = new SmtpClient(smtpHost, smtpPort);
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = _config["Smtp:EnableSsl"] == "true";
            }
            var mail = new MailMessage(from, email)
            {
                Subject = "Código de recuperação de senha",
                Body = $"Seu código de recuperação é: {code} (válido por 10 minutos)"
            };
            await client.SendMailAsync(mail);
        }
    }
}
