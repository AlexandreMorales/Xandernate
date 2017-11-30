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

        private ExecuterManager(string conn, DBTypes type)
        {
            factory = DataManager.getInstance(conn, type);
        }

        public static ExecuterManager GetInstance(string conn = null, DBTypes type = DBTypes.Sql)
        {
            if (Instance == null)
                Instance = new ExecuterManager(conn, type);

            return Instance;
        }

        public List<T> ExecuteQuerySimple<T>(string sql, params object[] parameters)
        {
            List<T> list = new List<T>();
            Type type = typeof(T);

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
                            do
                                while (reader.Read())
                                {
                                    list.Add((T)Mapper.StringToProp(reader[0], type));
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

        public List<TDao> ExecuteQuery<TDao>(string sql, object[] parameters = null, Func<IDataReader, TDao> IdentifierExpression = null)
            where TDao : new()
        {
            List<TDao> list = new List<TDao>();
            Type type = typeof(TDao);
            TDao obj;

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
                            do
                                while (reader.Read())
                                {
                                    if (IdentifierExpression != null)
                                        obj = IdentifierExpression.Invoke(reader);
                                    else if (type.IsNotPrimitive())
                                        obj = Mapper.MapToObjects<TDao>(reader);
                                    else
                                        obj = (TDao)Mapper.StringToProp(reader[0], type);

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

        public void ExecuteQuery(string sql, params object[] parameters)
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
