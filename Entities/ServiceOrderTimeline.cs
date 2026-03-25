using System;

namespace GestaoOficina.Entities
{
    public class ServiceOrderTimeline
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int ServiceOrderId { get; set; }
        public int? UserId { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public int? OldStatusId { get; set; }
        public int? NewStatusId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Tenant Tenant { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
        public User? User { get; set; }
        public ServiceOrderStatus? OldStatus { get; set; }
        public ServiceOrderStatus? NewStatus { get; set; }
    }
}
