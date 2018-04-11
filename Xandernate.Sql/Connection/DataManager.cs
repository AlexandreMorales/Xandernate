using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Xandernate.Sql.Connection
{
    internal class DataManager
    {
        private static DataManager _instance;
        private static IDbConnection _conn;
        private static string _connectionString;
        public static DbTypes DbType;

        private DataManager(string conn, DbTypes type)
        {
            _connectionString = conn;
            DbType = type;
        }

        public static DataManager GetInstance(string conn, DbTypes type)
        {
            if (_instance == null)
                _instance = new DataManager(conn, type);

            return _instance;
        }

        public IDbConnection GetConnection()
        {
            if (_conn == null)
                switch (DbType)
                {
                    case DbTypes.SqlServer:
                    default: _conn = new SqlConnection(); break;
                }

            if (_conn.State != ConnectionState.Open)
            {
                _conn.ConnectionString = _connectionString;
                _conn.Open();
            }

            return _conn;
        }

        public IDbCommand GetCommand(string query, params object[] parameters)
        {
            IDbCommand comm = null;

            switch (DbType)
            {
                case DbTypes.SqlServer:
                default: comm = new SqlCommand(query, (SqlConnection)_conn); break;
            }

            return (parameters.Length == 0) ? comm : AddParameters(comm, parameters);
        }

        private IDbCommand AddParameters(IDbCommand command, params object[] parameters)
        {
            IDbDataParameter data;
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (DbType)
                {
                    case DbTypes.SqlServer:
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
            => _connectionString
                .Split(';')
                .FirstOrDefault(x => x.Split('=')[0].Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                .Split('=')[1];
    }
}