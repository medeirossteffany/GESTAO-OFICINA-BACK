using System;
using System.Collections.Generic;

namespace GestaoOficina.Entities
{
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? UnitId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime PlanRenewalDate { get; set; } // Nova coluna para data de renovação

        public Unit? Unit { get; set; }
        public ICollection<Unit> Units { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<CustomerCategory> CustomerCategories { get; set; }
        public ICollection<Customer> Customers { get; set; }
        public ICollection<Vehicle> Vehicles { get; set; }
        public ICollection<ServiceOrder> ServiceOrders { get; set; }
        public ICollection<ServiceOrderPart> ServiceOrderParts { get; set; }
        public ICollection<ServiceOrderTimeline> ServiceOrderTimelines { get; set; }
        public TenantUsage? Usage { get; set; }
        public TenantPlan Plan { get; set; } = TenantPlan.Basico;
    }
}
