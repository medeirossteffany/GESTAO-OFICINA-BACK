using GestaoOficina.Entities;

namespace GestaoOficina.DTOs.Users
{
    public class UserResponse
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool FullAccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<int>? UnitIds { get; set; }
    }
}
