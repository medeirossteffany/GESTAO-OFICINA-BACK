namespace GestaoOficina.DTOs.Users
{
    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public bool? FullAccess { get; set; }
        public List<int>? UnitIds { get; set; }
    }
}
