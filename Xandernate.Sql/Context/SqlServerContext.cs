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
    public abstract class SqlServerContext<TContext> : IContext
        where TContext : IContext
    {
        private string _conn { get; set; }

        public SqlServerContext(string conn, bool createDbOnInitialize = true)
        {
            _conn = conn;

            if (createDbOnInitialize)
                CreateDb();
        }

        public virtual void CreateDb()
        {
            Type contextType = typeof(TContext);

            FieldInfo[] fields = contextType.GetFields();
            Type type = typeof(EntityHandlerSql<>);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.GetGenericTypeDefinition() == typeof(IEntityHandler<>))
                {
                    Type[] args = field.FieldType.GetGenericArguments();
                    Type makeme = type.MakeGenericType(args);
                    object o = Activator.CreateInstance(makeme, _conn);
                    field.SetValue(this, o);
                }
            }
        }

        public IEnumerable<T> QuerySimple<T>(string sql, params object[] parameters)
            => ExecuterManager.GetInstance(_conn).ExecuteQuerySimple<T>(sql, parameters);

        public IEnumerable<TEntity> Query<TEntity>(string sql, object[] parameters = null, Func<IDataReader, TEntity> IdentifierExpression = null)
            where TEntity : class, new()
            => ExecuterManager.GetInstance(_conn).ExecuteQuery(sql, parameters, IdentifierExpression);

        public void Query(string sql, params object[] parameters)
            => ExecuterManager.GetInstance(_conn).ExecuteQuery(sql, parameters);
    }
}
