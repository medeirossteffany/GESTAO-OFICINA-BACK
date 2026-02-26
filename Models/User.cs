using System;

namespace GestaoOficina.Models
{
    public class User
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? UnitId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public Unit? Unit { get; set; }
    }
}