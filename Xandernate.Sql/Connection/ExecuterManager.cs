using System;
using System.Collections.Generic;
using System.Data;

namespace Xandernate.Sql.Connection
{
    internal class ExecuterManager
    {
        private DataManager factory;
        private static ExecuterManager Instance;

        private ExecuterManager(string conn, DbTypes type)
        {
            factory = DataManager.GetInstance(conn, type);
        }

        public static ExecuterManager GetInstance(string conn = null, DbTypes type = DbTypes.SqlServer)
        {
            if (Instance == null)
                Instance = new ExecuterManager(conn, type);

            return Instance;
        }

        public IEnumerable<T> ExecuteQuerySimple<T>(string query, params object[] parameters)
        {
            List<T> list = new List<T>();
            Type type = typeof(T);

            parameters = parameters ?? new object[0];
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(query, parameters))
                    {
                        Logger.WriteLog(command.CommandText);
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            do
                                while (reader.Read())
                                {
                                    list.Add((T)Mapper.ConvertFromType(reader[0], type));
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

        public IEnumerable<TEntity> ExecuteQuery<TEntity>(string query, params object[] parameters)
            where TEntity : class, new()
            => ExecuteQuery<TEntity>(query, parameters, null);

        public IEnumerable<TEntity> ExecuteQuery<TEntity>(string query, object[] parameters = null, Func<IDataReader, TEntity> IdentifierExpression = null)
            where TEntity : class, new()
        {
            List<TEntity> list = new List<TEntity>();
            Type type = typeof(TEntity);

            parameters = parameters ?? new object[] { };
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(query, parameters))
                    {
                        Logger.WriteLog(command.CommandText);
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            do
                                while (reader.Read())
                                {
                                    TEntity obj;

                                    if (IdentifierExpression != null)
                                        obj = IdentifierExpression(reader);
                                    else if (type.IsNotPrimitive())
                                        obj = Mapper.MapToEntity<TEntity>(reader, type);
                                    else
                                        obj = (TEntity)Mapper.ConvertFromType(reader[0], type);

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

        public void ExecuteQuery(string query, params object[] parameters)
        {
            using (IDbConnection conn = factory.getConnection())
            {
                try
                {
                    using (IDbCommand command = factory.getCommand(query, parameters))
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
