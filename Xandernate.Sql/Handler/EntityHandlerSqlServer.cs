using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Xandernate.Handler;
using Xandernate.ReflectionCache;
using Xandernate.Sql.Connection;
using Xandernate.Sql.Entities;

namespace Xandernate.Sql.Handler
{
    public class EntityHandlerSqlServer<TEntity> : IEntityHandler<TEntity>
        where TEntity : new()
    {
        private readonly ExecuterManager _executer;
        private readonly QueryBuilder<TEntity> _queryBuilder;
        private readonly ReflectionEntityCache _reflectionCache;

        public EntityHandlerSqlServer(string conn)
        {
            _executer = ExecuterManager.GetInstance(conn);
            _reflectionCache = ReflectionEntityCache.GetOrCreateEntity(typeof(TEntity));
            _queryBuilder = new QueryBuilder<TEntity>();
            Init();
        }

        private void Init()
        {
            string createTable = _queryBuilder.GenerateCreate(_reflectionCache);

            _executer.ExecuteQuery(createTable);

            IEnumerable<INFORMATION_SCHEMA_COLUMNS> fields = GetFieldsDB();
            string migrationsSql = ColumnMigrations(fields) + TypeMigrations(fields);
            if (!String.IsNullOrEmpty(migrationsSql))
                _executer.ExecuteQuery(migrationsSql);
        }


        public void Add(params TEntity[] objs)
        {
            _queryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsert);
        }


        public void AddOrUpdate(params TEntity[] objs)
        {
            _queryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsertOrUpdate);
        }

        public void AddOrUpdate(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs)
        {
            IEnumerable<ReflectionPropertyCache> properties = identifierExpression.GetProperties().Select(p => _reflectionCache.Properties.FirstOrDefault(rp => rp.Name == p.Name));
            _queryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateInsertOrUpdate, properties);
        }


        public void Update(params TEntity[] objs)
        {
            _queryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateUpdate);
        }

        public void Update(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs)
        {
            IEnumerable<ReflectionPropertyCache> properties = identifierExpression.GetProperties().Select(p => _reflectionCache.Properties.FirstOrDefault(rp => rp.Name == p.Name));
            _queryBuilder.GenericAction(_reflectionCache, objs, GenerateScriptsEnum.GenerateUpdate, properties);
        }


        public TEntity Find<Att>(Att pk)
        {
            if (!typeof(Att).Equals(_reflectionCache.PrimaryKey.PropertyType))
                throw new Exception("The parameter type is not the Id type of the class " + _reflectionCache.PrimaryKey.Name);

            string query = _queryBuilder.GenerateSelect(_reflectionCache, _reflectionCache.PrimaryKey.Name);

            return _executer.ExecuteQuery<TEntity>(query, new object[] { pk }).FirstOrDefault();
        }

        public TEntity Find<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = _queryBuilder.GenerateSelect(_reflectionCache, fieldName);

            return _executer.ExecuteQuery<TEntity>(query, new object[] { value }).FirstOrDefault();
        }

        public IEnumerable<TEntity> FindAll()
        {
            string query = _queryBuilder.GenerateSelect(_reflectionCache, string.Empty, where: string.Empty);

            return _executer.ExecuteQuery<TEntity>(query);
        }

        public IEnumerable<TEntity> WhereEquals<Att>(Expression<Func<Att>> identifierExpression)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            Att Value = identifierExpression.Compile()();

            string query = _queryBuilder.GenerateSelect(_reflectionCache, fieldName);

            return _executer.ExecuteQuery<TEntity>(query, new object[] { Value });
        }

        public IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> identifierExpression)
        {
            BinaryExpression body = identifierExpression.Body as BinaryExpression;
            string validation = $" WHERE {body.ExpressionToString<SqlExpressionFunctions>().SubstringLast(1)}";
            string query = _queryBuilder.GenerateSelect(_reflectionCache, string.Empty, where: validation);

            return _executer.ExecuteQuery<TEntity>(query);
        }


        public void Remove(TEntity obj)
        {
            string query = _queryBuilder.GenerateDelete(_reflectionCache, _reflectionCache.PrimaryKey.Name);

            _executer.ExecuteQuery(query, _reflectionCache.PrimaryKey.GetValue(obj));
        }

        public void Remove<Att>(Att pk)
        {
            string query = _queryBuilder.GenerateDelete(_reflectionCache, _reflectionCache.PrimaryKey.Name);

            _executer.ExecuteQuery(query, pk);
        }

        public void Remove(Expression<Func<TEntity, bool>> identifierExpression)
        {
            BinaryExpression body = identifierExpression.Body as BinaryExpression;
            string validation = $" WHERE {body.ExpressionToString<SqlExpressionFunctions>().SubstringLast(1)}";
            string query = _queryBuilder.GenerateDelete(_reflectionCache, string.Empty, where: validation);

            _executer.ExecuteQuery(query);
        }

        public void Remove<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = _queryBuilder.GenerateDelete(_reflectionCache, fieldName);

            _executer.ExecuteQuery(query, value);
        }


        private string ColumnMigrations(IEnumerable<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            List<string> fieldsNameDB = fields.Select(x => x.Name).ToList();
            string query = string.Empty;

            //ADD FIELDS
            foreach (ReflectionPropertyCache property in _reflectionCache.Properties)
            {
                if (!fieldsNameDB.Contains(property.Name) && !fieldsNameDB.Contains($"{property.Name}Id"))
                {
                    string fkPkName = string.Empty;
                    string valType = property.PropertyType.ToStringDb();

                    if (property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
                        fkPkName = fk.PrimaryKey.Name;
                    }

                    query += $"ALTER TABLE {_reflectionCache.Name} ADD {property.Name}{fkPkName} {valType};\n";

                    if (!string.IsNullOrEmpty(fkPkName))
                        query = $"ALTER TABLE {_reflectionCache.Name} ADD FOREIGN KEY({property.Name}{fkPkName}) REFERENCES {property.PropertyType.Name}({fkPkName});\n";
                }
            }

            //DROP FIELDS
            foreach (string column in fieldsNameDB)
                if (_reflectionCache.Properties.FirstOrDefault(p => column == p.Name || column == $"{p.Name}Id") == null)
                    query += $"ALTER TABLE {_reflectionCache.Name} DROP COLUMN {column};\n";

            return query;
        }

        private string TypeMigrations(IEnumerable<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            string query = string.Empty;
            foreach (INFORMATION_SCHEMA_COLUMNS field in fields)
            {
                ReflectionPropertyCache property = _reflectionCache.Properties.FirstOrDefault(p => field.Name == p.Name || field.Name == $"{p.Name}Id");
                string valType = property.PropertyType.ToStringDb();

                if (!field.TypeString.Equals(Regex.Replace(valType, @"(\(.*\))", string.Empty)))
                    query += $"ALTER TABLE {_reflectionCache.Name} ALTER COLUMN {property.Name} {valType};\n";
            }

            return query;
        }

        private IEnumerable<INFORMATION_SCHEMA_COLUMNS> GetFieldsDB()
        {
            string sql = $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{_reflectionCache.Name}';\n";
            return _executer.ExecuteQuery(sql, null, x => new INFORMATION_SCHEMA_COLUMNS
            {
                TableName = _reflectionCache.Name,
                Name = x.GetString(0),
                TypeString = x.GetString(1),
                Type = Mapper.StringDBToType(x.GetString(1))
            });
        }

    }
}
