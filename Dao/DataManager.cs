using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    public class DataManager
    {
        /*
         * Install-Package MySql.Data
         */

        private static DataManager conn;
        private static string connectionString;
        private static string provider;
        public static string Provider { get { return provider; } }

        private DataManager(string _connString, string _provider)
        {
            var conn = ConfigurationManager.ConnectionStrings["DefaultConnection"];

            if (_connString == null && conn == null)
                throw new Exception("Adicione uma ConnectionString no web/App.config ou mande uma por parametro");
            connectionString = _connString ?? conn.ConnectionString;

            provider = _provider ?? ((conn == null) ? "System.Data.SqlClient" : (conn.ProviderName ?? "System.Data.SqlClient"));
            provider = provider.Replace("System.Data.", "");
        }

        public static DataManager getInstance(string _connString, string _provider)
        {
            if (conn == null)
                conn = new DataManager(_connString, _provider);

            return conn;
        }

        public IDbConnection getConnection()
        {
            switch (Provider.ToLower())
            {
                case "odp.net": return new OracleConnection(connectionString);
            }
            return new SqlConnection(connectionString);
        }

        public IDbCommand getCommand(string query, IDbConnection conn, Object[] parameters = null)
        {
            IDbCommand comm = new SqlCommand(query, (SqlConnection)conn);

            switch (Provider.ToLower())
            {
                case "odp.net": comm = new OracleCommand(query.Replace('@', ':'), (OracleConnection)conn); break;
            }

            return (parameters == null) ? comm : AddParameters(comm, parameters);
        }

        private IDbCommand AddParameters(IDbCommand command, Object[] parameters)
        {
            IDbDataParameter data;
            Type ParameterType = typeof(SqlParameter);
            switch (Provider.ToLower())
            {
                case "odp.net": ParameterType = typeof(OracleParameter); break;
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
    }
}
