using System;
using System.Collections.Generic;

namespace GestaoOficina.Entities
{
    public class Unit
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
        public string? Cnpj { get; set; }
        public string AddressZip { get; set; }
        public string AddressStreet { get; set; }
        public string AddressNumber { get; set; }
        public string AddressDistrict { get; set; }
        public string AddressCity { get; set; }
        public string AddressState { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<UserUnit> UserUnits { get; set; }
        public ICollection<CustomerUnit> CustomerUnits { get; set; }
        public ICollection<ServiceOrder> ServiceOrders { get; set; }
    }
}
