using System;
using System.Collections.Generic;

namespace GestaoOficina.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int LegalTypeId { get; set; }
        public string Name { get; set; }
        public string? CpfCnpj { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public CustomerLegalType LegalType { get; set; }
        public ICollection<CustomerUnit> CustomerUnits { get; set; }
    }
}
