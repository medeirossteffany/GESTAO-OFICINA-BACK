using System;

namespace GestaoOficina.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int LegalTypeId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string? Cpf { get; set; }
        public string? Cnpj { get; set; }
        public string? StateRegistration { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public CustomerLegalType LegalType { get; set; }
        public CustomerCategory? Category { get; set; }
    }
}