using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Xandernate.DAO
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
            ConnectionStringSettings connSettings = ConfigurationManager.ConnectionStrings["DefaultConnection"];

            if (_connString == null && connSettings == null)
                throw new Exception("Adicione uma ConnectionString no web/App.config ou mande uma por parametro");
            connectionString = _connString ?? connSettings.ConnectionString;

            provider = _provider ?? ((connSettings == null) ? "System.Data.SqlClient" : (connSettings.ProviderName ?? "System.Data.SqlClient"));
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
                    case "odp.net": //conn = new OracleConnection(); break;
                    case "mysql": conn = new MySqlConnection(); break;
                    default:
                    case "sqlclient": conn = new SqlConnection(); break;
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
                case "odp.net": //comm = new OracleCommand(query.Replace('@', ':'), (OracleConnection)conn); break;
                case "mysql": comm = new MySqlCommand(query, (MySqlConnection)conn); break;
                default:
                case "sqlclient": comm = new SqlCommand(query, (SqlConnection)conn); break;
            }

            return (parameters.Length == 0) ? comm : AddParameters(comm, parameters);
        }

        private IDbCommand AddParameters(IDbCommand command, object[] parameters)
        {
            IDbDataParameter data;
            Type ParameterType = null;
            switch (Provider.ToLower())
            {
                case "odp.net": //ParameterType = typeof(OracleParameter); break;
                case "mysql": ParameterType = typeof(MySqlParameter); break;
                default:
                case "sqlclient": ParameterType = typeof(SqlParameter); break;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                data = (parameters[i].GetType() == typeof(DateTime)) ?
                    (IDbDataParameter)ParameterType.GetConstructor(new[] { typeof(string), typeof(SqlDbType) })
                        .Invoke(new object[] { i.ToString(), SqlDbType.DateTime }) :
                    (IDbDataParameter)ParameterType.GetConstructor(new[] { typeof(string), typeof(object) })
                        .Invoke(new object[] { i.ToString(), parameters[i] });

                data.Value = parameters[i];
                command.Parameters.Add(data);
            }

            return command;
        }

        public static string GetDatabase()
        {
            return connectionString.Split(';').FirstOrDefault(x => x.Split('=')[0].Equals("Initial Catalog", StringComparison.InvariantCultureIgnoreCase)).Split('=')[1];
        }
    }
}
