namespace GestaoOficina.Entities
{
    public class CustomerLegalType
    {
        public int Id { get; set; }
        public string Code { get; set; } // PF, PJ
        public string Name { get; set; } // Pessoa Física, Pessoa Jurídica
    }
}