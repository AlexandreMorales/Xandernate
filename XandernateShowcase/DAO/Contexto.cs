using Xandernate.DAO;
using Xandernate.SQL.DAO;
using XandernateShowcase.Models;

namespace XandernateShowcase.DAO
{
    public class Contexto
    {
        public Contexto(string conn)
        {
            Funcionarios = new DaoHandlerSQL<Funcionario>(conn);
            Pessoas = new DaoHandlerSQL<Pessoa>(conn);
            Enderecos = new DaoHandlerSQL<Endereco>(conn);
        }
        public IDaoHandler<Endereco> Enderecos;
        public IDaoHandler<Pessoa> Pessoas;
        public IDaoHandler<Funcionario> Funcionarios;
    }
}
