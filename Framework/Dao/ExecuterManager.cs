using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    public static class ExecuterManager
    {
        private static DataManager factory;
        public static string breakLine = System.Environment.NewLine;

        public static void Config(string _connString, string _provider)
        {
            factory = DataManager.getInstance(_connString, _provider);
        }

        public static List<Classe> ExecuteQuery<Classe>(string sql, object[] parameters = null, Func<IDataReader, Classe> IdentifierExpression = null)
        {
            parameters = parameters ?? new object[0];
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(sql, parameters))
                    {
                        writeTxt(command.CommandText);
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            List<Classe> list = new List<Classe>();
                            Type type = typeof(Classe);
                            Classe obj;
                            do
                                while (reader.Read())
                                {
                                    if (IdentifierExpression != null)
                                        obj = IdentifierExpression.Invoke(reader);
                                    else if (type.IsNotPrimitive())
                                        obj = MappingUtils.MapToObjects<Classe>(reader);
                                    else
                                        obj = (Classe)MappingUtils.StringToProp(reader[0], type);

                                    list.Add(obj);
                                }
                            while (reader.NextResult());

                            return list;
                        }
                    }
                }
                catch (Exception e)
                {
                    writeTxt(e.Message);
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public static void ExecuteQueryNoReturn(string sql, params object[] parameters)
        {
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(sql, parameters))
                    {
                        writeTxt(command.CommandText);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    writeTxt(e.Message);
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
            string dirLog = ConfigurationManager.AppSettings["DirLogs"];
            if (dirLog != null)
                dirLog += @"\";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(dirLog + @"XandernateLog.txt", true))
            {
                file.WriteLine(query + breakLine +
                    "-----------------------------------------------------------------------------------------------------" +
                    breakLine);
            }
        }
    }
}
