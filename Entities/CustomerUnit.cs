namespace GestaoOficina.Entities
{
    public class CustomerUnit
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int UnitId { get; set; }
        public bool IsActive { get; set; }
        public Customer Customer { get; set; }
        public Unit Unit { get; set; }
    }
}
