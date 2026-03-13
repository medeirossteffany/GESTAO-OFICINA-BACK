namespace GestaoOficina.DTOs.Units
{
    public class UpdateUnitRequest
    {
        public string? Name { get; set; }
        public string? Cnpj { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
    }
}
