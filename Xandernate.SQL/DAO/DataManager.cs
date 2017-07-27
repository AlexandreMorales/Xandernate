using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Xandernate.SQL.DAO
{
    public class DataManager
    {
        private static DataManager instance;
        private static IDbConnection conn;
        private static string connectionString;
        private static string provider;
        public static string Provider { get { return provider; } }

        private DataManager(string _connString, string _provider)
        {
            connectionString = _connString;

            provider = _provider ?? "System.Data.SqlClient";
            provider = provider.Replace("System.Data.", "");
        }

        public static DataManager getInstance(string _connString, string _provider)
        {
            if (instance == null)
                instance = new DataManager(_connString, _provider);

            return instance;
        }

        public IDbConnection getConnection()
        {
            if (conn == null)
                switch (Provider.ToLower())
                {
                    case "sqlclient":
                    default: conn = new SqlConnection(); break;
                }

            if (conn.State != ConnectionState.Open)
            {
                conn.ConnectionString = connectionString;
                conn.Open();
            }

            return conn;
        }

        public IDbCommand getCommand(string query, object[] parameters)
        {
            IDbCommand comm = null;

            switch (Provider.ToLower())
            {
                case "sqlclient":
                default: comm = new SqlCommand(query, (SqlConnection)conn); break;
            }

            return (parameters.Length == 0) ? comm : AddParameters(comm, parameters);
        }

        private IDbCommand AddParameters(IDbCommand command, object[] parameters)
        {
            IDbDataParameter data;
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (Provider.ToLower())
                {
                    case "sqlclient":
                    default:
                        data = (parameters[i].GetType() == typeof(DateTime)) ?
                                new SqlParameter(i.ToString(), SqlDbType.DateTime) :
                                new SqlParameter(i.ToString(), parameters[i]);
                        break;
                }

                data.Value = parameters[i];
                command.Parameters.Add(data);
            }

            return command;
        }

        public static string GetDatabase()
        {
            return connectionString.Split(';').FirstOrDefault(x => x.Split('=')[0].Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)).Split('=')[1];
        }
    }
}
