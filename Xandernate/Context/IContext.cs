using System;
using System.Collections.Generic;
using System.Data;

namespace Xandernate.Context
{
    public interface IContext
    {
        IEnumerable<T> QuerySimple<T>(string sql, params object[] parameters);

        IEnumerable<TEntity> Query<TEntity>(string sql, object[] parameters = null, Func<IDataReader, TEntity> IdentifierExpression = null)
            where TEntity : new();

        void Query(string sql, params object[] parameters);
    }
}
