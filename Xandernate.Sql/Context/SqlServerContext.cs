using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Xandernate.Context;
using Xandernate.Handler;
using Xandernate.Sql.Connection;
using Xandernate.Sql.Handler;

namespace Xandernate.Sql.Context
{
    public abstract class SqlContext<TContext> : IContext
        where TContext : IContext
    {
        private readonly string _conn;
        private SqlDatabaseHandler _databaseHandler;

        public SqlContext(string conn, bool createDbOnInitialize = true)
        {
            _conn = conn;

            if (createDbOnInitialize)
                CreateDb();
        }

        public virtual void CreateDb()
        {
            Type contextType = typeof(TContext);

            FieldInfo[] fields = contextType.GetFields();
            Type handlerType = typeof(SqlEntityHandler<>);

            _databaseHandler = new SqlDatabaseHandler(_conn);
            ExecuterManager.CreateInstance(_conn);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.GetGenericTypeDefinition() == typeof(IEntityHandler<>))
                {
                    Type[] args = field.FieldType.GetGenericArguments();
                    Type genericHandlerType = handlerType.MakeGenericType(args);
                    object handler = Activator.CreateInstance(genericHandlerType);
                    field.SetValue(this, handler);
                }
            }
        }

        public IEnumerable<T> QuerySimple<T>(string sql, params object[] parameters)
            => ExecuterManager.GetInstance().ExecuteQuerySimple<T>(sql, parameters);

        public IEnumerable<TEntity> Query<TEntity>(string sql, object[] parameters = null, Func<IDataReader, TEntity> IdentifierExpression = null)
            where TEntity : class, new()
            => ExecuterManager.GetInstance().ExecuteQuery(sql, parameters, IdentifierExpression);

        public void Query(string sql, params object[] parameters)
            => ExecuterManager.GetInstance().ExecuteQuery(sql, parameters);
    }
}
