namespace GestaoOficina.DTOs.Users
{
    public class CreateUserRequest
    {
        public int TenantId { get; set; }
        public int? UnitId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}