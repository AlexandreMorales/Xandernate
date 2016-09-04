using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xandernate.Annotations;

namespace XandernateShowcase.Models
{
    public class Pessoa
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Idade { get; set; }
        public double Altura { get; set; }
        public string Nome { get; set; }
        public double Peso { get; set; }
        [ForeignKey]
        public Endereco endereco { get; set; }
    }
}
