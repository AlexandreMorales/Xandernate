using Xandernate.Annotations;

namespace XandernateShowcase.Models
{
    public class Funcionario
    {
        [PrimaryKey]
        public int Id { get; set; }
        public double Salario { get; set; }
        [ForeignKey]
        public Pessoa pessoa { get; set; }
    }
}
