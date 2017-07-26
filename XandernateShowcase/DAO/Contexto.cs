using Xandernate.DAO;
using XandernateShowcase.Models;

namespace XandernateShowcase.DAO
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
