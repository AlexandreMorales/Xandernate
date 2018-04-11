using Xandernate.Sql.Connection;

namespace Xandernate.Sql.Handler
{
    internal class SqlDatabaseHandler
    {
        public SqlDatabaseHandler(string conn)
        { 
            string createTable = QueryBuilder.GenerateCreateDb(ref conn);

            ExecuterManager.CreateInstance(conn).ExecuteQuery(createTable);
        }
    }
}
