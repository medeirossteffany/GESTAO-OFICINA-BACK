using System;
using Microsoft.AspNetCore.Identity;

namespace GestaoOficina.Entities
{
    public class User : IdentityUser<int>
    {
        public int TenantId { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool FullAccess { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public ICollection<UserUnit> UserUnits { get; set; }
    }
}