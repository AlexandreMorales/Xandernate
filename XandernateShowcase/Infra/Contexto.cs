using Xandernate.Handler;
using Xandernate.Sql.Context;
using XandernateShowcase.Models;

namespace XandernateShowcase.Infra
{
    public class Contexto : SqlContext<Contexto>
    {
        public Contexto(string conn)
            : base(conn, false)
        {
        }

        public IEntityHandler<Endereco> Enderecos;
        public IEntityHandler<Pessoa> Pessoas;
        public IEntityHandler<Funcionario> Funcionarios;
    }
}
