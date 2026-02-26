using System;

namespace GestaoOficina.Models
{
    public class CustomerCategory
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
    }
}