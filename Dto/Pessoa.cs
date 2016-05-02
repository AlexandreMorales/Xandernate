using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dto
{
    class Pessoa
    {
        public int Id { get; set; }
        public int Idade { get; set; }
        public double Altura { get; set; }
        public string Nome { get; set; }
        public double Peso { get; set; }
        public Endereco endereco { get; set; }
        
    }

    class Endereco
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Rua { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Cep { get; set; }

    }
}
