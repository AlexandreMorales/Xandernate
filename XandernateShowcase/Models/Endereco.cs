using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xandernate.Annotations;

namespace XandernateShowcase.Models
{
    public class Endereco
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Rua { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Cep { get; set; }
    }
}
