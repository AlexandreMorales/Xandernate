using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dto
{
    class Funcionario
    {
        public int Id { get; set; }
        public double Salario { get; set; }
        public Pessoa pessoa { get; set; }
    }
}
