using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Xandernate.Handler;
using Xandernate.ReflectionCache;
using Xandernate.Sql.Connection;
using Xandernate.Sql.Entities;

namespace Xandernate.Sql.Handler
{
    public class EntityHandlerSql<TEntity> : IEntityHandler<TEntity>
        where TEntity : class, new()
    {
        private readonly ExecuterManager _executer;
        private readonly ReflectionEntityCache _reflectionCache;
        private readonly SqlExpressionFunctions _expressionFunctions;

        private readonly string _query_delete;

        public EntityHandlerSql(string conn)
        {
            _executer = ExecuterManager.GetInstance(conn);
            _reflectionCache = ReflectionEntityCache.GetOrCreateEntity<TEntity>();
            _expressionFunctions = new SqlExpressionFunctions();

            _query_delete = QueryBuilder.GenerateDelete(_reflectionCache.Name, _reflectionCache.PrimaryKey.Name);

            Init();
        }

        private void Init()
        {
            string createTable = QueryBuilder.GenerateCreate(_reflectionCache);

            _executer.ExecuteQuery(createTable);

            IEnumerable<InformationSchemaColumns> columns = GetDbColumns();
            string migrationsSql = QueryBuilder.GenerateColumnMigrations(_reflectionCache, columns) + QueryBuilder.GenerateTypeMigrations(_reflectionCache, columns);

            if (!string.IsNullOrEmpty(migrationsSql))
                _executer.ExecuteQuery(migrationsSql);
        }


        public void Add(params TEntity[] objs)
            => QueryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsert);


        public void AddOrUpdate(params TEntity[] objs)
            => QueryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsertOrUpdate);

        public void AddOrUpdate(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs)
        {
            IEnumerable<ReflectionPropertyCache> properties = identifierExpression.GetProperties().Select(p => _reflectionCache.GetProperty(p.Name));
            QueryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsertOrUpdate, properties);
        }


        public void Update(params TEntity[] objs)
            => Update(null, objs);

        public void Update(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs)
        {
            IEnumerable<ReflectionPropertyCache> properties = identifierExpression?.GetProperties().Select(p => _reflectionCache.GetProperty(p.Name));

            StringBuilder query = new StringBuilder();
            IList<object> parameters = new List<object>();

            foreach (TEntity obj in objs)
                query.AppendLine(QueryBuilder.GenerateUpdate(_reflectionCache, obj, parameters, properties));

            _executer.ExecuteQuery(query.ToString(), parameters);
        }


        public TEntity Find<Att>(Att pk)
        {
            if (!typeof(Att).Equals(_reflectionCache.PrimaryKey.Type))
                throw new Exception($"The parameter type is not the Primary Key type of the class {_reflectionCache.PrimaryKey.Name}");

            string query = QueryBuilder.GenerateSelect(_reflectionCache, _reflectionCache.PrimaryKey.Name);

            return _executer.ExecuteQuery<TEntity>(query, pk).FirstOrDefault();
        }

        public TEntity Find<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value)
        {
            string query = QueryBuilder.GenerateSelect(_reflectionCache, GetMemberName(identifierExpression));

            return _executer.ExecuteQuery<TEntity>(query, value).FirstOrDefault();
        }

        public IEnumerable<TEntity> FindAll()
        {
            string query = QueryBuilder.GenerateSelect(_reflectionCache, string.Empty, where: string.Empty);

            return _executer.ExecuteQuery<TEntity>(query);
        }

        public IEnumerable<TEntity> WhereEquals<Att>(Expression<Func<Att>> identifierExpression)
        {
            string query = QueryBuilder.GenerateSelect(_reflectionCache, GetMemberName(identifierExpression));

            Att value = identifierExpression.Compile()();

            return _executer.ExecuteQuery<TEntity>(query, value);
        }

        public IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> identifierExpression)
        {
            string where = QueryBuilder.GenerateWhere(identifierExpression, _expressionFunctions);
            string query = QueryBuilder.GenerateSelect(_reflectionCache, string.Empty, where: where);

            return _executer.ExecuteQuery<TEntity>(query);
        }


        public void Remove(TEntity obj)
            => _executer.ExecuteQuery(_query_delete, _reflectionCache.PrimaryKey.GetValue(obj));

        public void Remove<Att>(Att pk)
            => _executer.ExecuteQuery(_query_delete, pk);

        public void Remove(Expression<Func<TEntity, bool>> identifierExpression)
        {
            string where = QueryBuilder.GenerateWhere(identifierExpression, _expressionFunctions);
            string query = QueryBuilder.GenerateDelete(_reflectionCache.Name, string.Empty, where: where);

            _executer.ExecuteQuery(query);
        }

        public void Remove<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value)
        {
            string query = QueryBuilder.GenerateDelete(_reflectionCache.Name, GetMemberName(identifierExpression));

            _executer.ExecuteQuery(query, value);
        }


        private static string GetMemberName(LambdaExpression identifierExpression)
        {
            if (identifierExpression.Body is MemberExpression member)
                return member.Member.Name;

            return string.Empty;
        }
        
        private IEnumerable<InformationSchemaColumns> GetDbColumns()
        {
            return _executer.ExecuteQuery(QueryBuilder.GenerateSelectColumns(_reflectionCache.Name), null, 
                x => new InformationSchemaColumns
                {
                    TableName = _reflectionCache.Name,
                    Name = x.GetString(0),
                    TypeString = x.GetString(1),
                    Type = Mapper.ColumnNameToType(x.GetString(1))
                });
        }

    }
}
