using System;
using System.Collections.Generic;
using System.Data;

using Xandernate.Utils;
using Xandernate.Utils.Extensions;
using Xandernate.SQL.Utils;

namespace Xandernate.SQL.DAO
{
    public class ExecuterManager
    {
        private DataManager factory;
        private static ExecuterManager Instance;

        private ExecuterManager(string _connString, string _provider)
        {
            factory = DataManager.getInstance(_connString, _provider);
        }

        public static ExecuterManager GetInstance(string _connString = null, string _provider = null)
        {
            if (Instance == null)
                Instance = new ExecuterManager(_connString, _provider);

            return Instance;
        }

        public List<TClass> ExecuteQuery<TClass>(string sql, object[] parameters = null, Func<IDataReader, TClass> IdentifierExpression = null)
            where TClass : new()
        {
            parameters = parameters ?? new object[0];
            sql = sql.Replace("\n", Environment.NewLine);
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(sql, parameters))
                    {
                        Logger.WriteLog(command.CommandText);
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            List<TClass> list = new List<TClass>();
                            Type type = typeof(TClass);
                            TClass obj;
                            do
                                while (reader.Read())
                                {
                                    if (IdentifierExpression != null)
                                        obj = IdentifierExpression.Invoke(reader);
                                    else if (type.IsNotPrimitive())
                                        obj = Mapper.MapToObjects<TClass>(reader);
                                    else
                                        obj = (TClass)Mapper.StringToProp(reader[0], type);

                                    list.Add(obj);
                                }
                            while (reader.NextResult());

                            return list;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e.Message);
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public void ExecuteQueryNoReturn(string sql, params object[] parameters)
        {
            sql = sql.Replace("\n", Environment.NewLine);
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(sql, parameters))
                    {
                        Logger.WriteLog(command.CommandText);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e.Message);
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
    }
}
