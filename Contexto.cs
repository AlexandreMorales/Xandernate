using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using XandernateShowcase.Models;

using Xandernate.Dao;


namespace XandernateShowcase.Dao
{
    public class Contexto
    {
        public Contexto()
        {
            Funcionarios = new DbDao<Funcionario>();
            Pessoas = new DbDao<Pessoa>();
            Enderecos = new DbDao<Endereco>();
        }
        public IDbDao<Endereco> Enderecos;
        public IDbDao<Pessoa> Pessoas;
        public IDbDao<Funcionario> Funcionarios;
    }
}
