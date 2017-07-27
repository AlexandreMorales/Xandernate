using Xandernate.DAO;
using XandernateShowcase.Models;

namespace XandernateShowcase.DAO
{
    public class Contexto
    {
        public Contexto(string conn)
        {
            Funcionarios = new DbDao<Funcionario>(conn);
            Pessoas = new DbDao<Pessoa>(conn);
            Enderecos = new DbDao<Endereco>(conn);
        }
        public IDbDao<Endereco> Enderecos;
        public IDbDao<Pessoa> Pessoas;
        public IDbDao<Funcionario> Funcionarios;
    }
}
