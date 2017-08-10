using Xandernate.DAO;
using Xandernate.SQL.DAO;
using XandernateShowcase.Models;

namespace XandernateShowcase.DAO
{
    public class Contexto : SqlContext<Contexto>
    {
        public Contexto(string conn)
            : base(conn)
        {
        }

        public IDaoHandler<Endereco> Enderecos;
        public IDaoHandler<Pessoa> Pessoas;
        public IDaoHandler<Funcionario> Funcionarios;
    }
}
