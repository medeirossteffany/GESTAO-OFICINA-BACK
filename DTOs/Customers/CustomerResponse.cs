namespace GestaoOficina.DTOs.Customers
{
    public class CustomerResponse
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
        public List<int> UnitIds { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
