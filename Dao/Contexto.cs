using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Xandernate.Dto;

namespace Xandernate.Dao
{
    class Contexto
    {
        public Contexto()
        {
            string conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            Enderecos = new DbDao<Endereco>(conn);
            Pessoas = new DbDao<Pessoa>(conn);
            Funcionarios = new DbDao<Funcionario>(conn);
        }
        public IDbDao<Endereco> Enderecos;
        public IDbDao<Pessoa> Pessoas;
        public IDbDao<Funcionario> Funcionarios;
    }
}
