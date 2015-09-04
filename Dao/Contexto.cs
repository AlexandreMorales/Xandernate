using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xandernate.Dto;

namespace Xandernate.Dao
{
    class Contexto
    {
        public Contexto()
        {
            Pessoas = new DbDao<Pessoa>(@"C:\Users\afraga\Documents\testeReflection.mdf");
            Funcionarios = new DbDao<Funcionario>(@"C:\Users\afraga\Documents\testeReflection.mdf");
        }
        public IDbDao<Pessoa> Pessoas;
        public IDbDao<Funcionario> Funcionarios;
    }
}
