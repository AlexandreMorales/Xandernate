using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    public class ExecuterManager
    {
        private DataManager factory;

        public ExecuterManager(string _connString, string _provider)
        {
            factory = DataManager.getInstance(_connString, _provider);
        }

        protected List<string[]> executeQuery(string sql, params Object[] parameters)
        {
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand comando = factory.getCommand(sql, conn, parameters))
                    {
                        conn.Open();
                        writeTxt(comando.CommandText);
                        using (IDataReader leitor = comando.ExecuteReader())
                        {
                            string[] values;
                            List<string[]> lista = new List<string[]>();
                            while (leitor.Read())
                            {
                                values = new string[leitor.FieldCount];
                                for (int i = 0; i < values.Length; i++)
                                    values[i] = leitor[i].ToString();
                                lista.Add(values);
                            }
                            return lista;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        protected void executeQueryNoReturn(string sql, params Object[] parameters)
        {
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand comando = factory.getCommand(sql, conn, parameters))
                    {
                        conn.Open();
                        writeTxt(comando.CommandText);
                        comando.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private static void writeTxt(string query)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Querys.txt", true))
            {
                file.WriteLine(query + "\n-----------------------------------------------------------------------------------------------------\n");
            }
        }
    }
}
