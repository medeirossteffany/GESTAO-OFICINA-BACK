namespace GestaoOficina.Entities
{
    public class UserUnit
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int UnitId { get; set; }

        public User User { get; set; }
        public Unit Unit { get; set; }
    }
}
