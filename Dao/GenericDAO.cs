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
    class GenericDAO
    {
        string database;

        public GenericDAO(string _database)
        {
            database = _database;
        }

        protected List<string[]> executeQuery(string sql, params Object[] parameters)
        {
            using (SqlConnection conn = Connection.getInstance().getConnection(this.database))
            {
                try
                {
                    using (SqlCommand comando = new SqlCommand(sql, conn))
                    {
                        conn.Open();

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].GetType() == typeof(DateTime))
                            {
                                SqlParameter data = new SqlParameter(i.ToString(), SqlDbType.DateTime);
                                data.Value = parameters[i];
                                comando.Parameters.Add(data);
                            }
                            else
                                comando.Parameters.Add(new SqlParameter(i.ToString(), parameters[i]));
                        }
                        writeTxt(comando.CommandText);

                        using (SqlDataReader leitor = comando.ExecuteReader())
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
                    Connection.close();
                    conn.Close();
                }
            }
        }

        protected void executeQueryNoReturn(string sql, params Object[] parameters)
        {
            using (SqlConnection conn = Connection.getInstance().getConnection(this.database))
            {
                try
                {
                    using (SqlCommand comando = new SqlCommand(sql, conn))
                    {
                        conn.Open();

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].GetType() == typeof(DateTime))
                            {
                                SqlParameter data = new SqlParameter(i.ToString(), SqlDbType.DateTime);
                                data.Value = parameters[i];
                                comando.Parameters.Add(data);
                            }
                            else
                                comando.Parameters.Add(new SqlParameter(i.ToString(), parameters[i]));
                        }
                        writeTxt(comando.CommandText);

                        comando.ExecuteReader();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    Connection.close();
                    conn.Close();
                }
            }
        }

        protected string[] getFieldsNames(string table)
        {
            string sql = "select c.name from sys.columns c " +
                            "inner join sys.tables t " +
                            "on t.object_id = c.object_id " +
                            "and t.name = '" + table + "' " +
                            "and t.type = 'U'";
            List<string> listacolumnas = new List<string>();
            using (SqlConnection conn = Connection.getInstance().getConnection(this.database))
            {
                try
                {
                    using (SqlCommand comando = new SqlCommand(sql, conn))
                    {
                        conn.Open();
                        writeTxt(comando.CommandText);


                        using (SqlDataReader leitor = comando.ExecuteReader())
                        {
                            while (leitor.Read())
                            {
                                listacolumnas.Add(leitor.GetString(0));
                            }
                            return listacolumnas.ToArray();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    Connection.close();
                    conn.Close();
                }
            }
        }

        protected string[] getFieldsTypes(string table)
        {
            string sql = "select ty.name from sys.columns c " +
                            "inner join sys.tables t " +
                            "on t.object_id = c.object_id " +
                            "inner join sys.types ty " +
                            "on c.user_type_id = ty.user_type_id " +
                            "and t.name = '" + table + "' " +
                            "and t.type = 'U'";
            List<string> listacolumnas = new List<string>();
            using (SqlConnection conn = Connection.getInstance().getConnection(this.database))
            {
                try
                {
                    using (SqlCommand comando = new SqlCommand(sql, conn))
                    {
                        conn.Open();
                        writeTxt(comando.CommandText);

                        using (SqlDataReader leitor = comando.ExecuteReader())
                        {
                            while (leitor.Read())
                            {
                                listacolumnas.Add(leitor.GetString(0));
                            }
                            return listacolumnas.ToArray();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    Connection.close();
                    conn.Close();
                }
            }
        }

        private static void writeTxt(string query)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Querys.txt", true))
            {
                file.WriteLine(query + "\n");
            }
        }
    }
}
