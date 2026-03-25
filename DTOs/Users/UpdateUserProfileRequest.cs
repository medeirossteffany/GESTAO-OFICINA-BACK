namespace GestaoOficina.DTOs.Users
{
    public class UpdateUserProfileRequest
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CpfCnpj { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
    }
}
