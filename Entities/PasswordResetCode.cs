using System;

namespace GestaoOficina.Entities
{
    public class PasswordResetCode
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; } 
        public DateTime Expiration { get; set; }
        public bool Used { get; set; }
    }
}