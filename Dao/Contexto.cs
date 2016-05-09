using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xandernate.Dto;

namespace Xandernate.Dao
{
    public class Contexto
    {
        /*
         * ORDER 
         */

        public Contexto()
        {
            Enderecos = new DbDao<Endereco>();
            Pessoas = new DbDao<Pessoa>();
            Funcionarios = new DbDao<Funcionario>();
        }
        public IDbDao<Endereco> Enderecos;
        public IDbDao<Pessoa> Pessoas;
        public IDbDao<Funcionario> Funcionarios;
    }
}
