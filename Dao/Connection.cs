using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    class Connection
    {
        private static Connection conn;
        private static string connectionString;

        private Connection()
        {
        }

        public static Connection getInstance()
        {
            if (conn == null)
                conn = new Connection();

            return conn;
        }

        public SqlConnection getConnection(string connString)
        {
            connectionString = connString;
            return new SqlConnection(connString);
        }

        public static void close()
        {
            if (conn.getConnection(connectionString) != null)
                conn.getConnection(connectionString).Close();
        }
    }
}
