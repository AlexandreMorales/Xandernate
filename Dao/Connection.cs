using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    class Connection
    {
        private static Connection conn;

        private Connection()
        {
        }

        public static Connection getInstance()
        {
            if (conn == null)
                conn = new Connection();

            return conn;
        }

        public SqlConnection getConnection(string database = @"C:\Users\afraga\Documents\testeReflection.mdf")
        {
            return new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=" + database + ";Integrated Security=True;Connect Timeout=30");
        }

        public static void close()
        {
            if (conn.getConnection() != null)
                conn.getConnection().Close();
        }
    }
}
