namespace GestaoOficina.DTOs.Users
{
    public class CreateUserRequest
    {
        public List<int>? UnitIds { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CpfCnpj { get; set; }
        public string? AddressZip { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressDistrict { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
