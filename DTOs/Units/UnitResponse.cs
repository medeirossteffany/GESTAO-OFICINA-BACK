namespace GestaoOficina.DTOs.Units
{
    public class UnitResponse
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
    }
}
