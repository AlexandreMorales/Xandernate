using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Xandernate.ReflectionCache;
using Xandernate.Sql.Connection;
using Xandernate.Sql.Entities;

namespace Xandernate.Sql.Handler
{
    internal static class QueryBuilder
    {
        public static void GenericAction<TEntity>(ReflectionEntityCache typeCache, IList<TEntity> objs, GenerateScriptsEnum method, IEnumerable<ReflectionPropertyCache> properties = null)
        {
            List<object> parameters = new List<object>();
            StringBuilder query = new StringBuilder();

            foreach (TEntity obj in objs)
            {
                switch (method)
                {
                    case GenerateScriptsEnum.GenerateInsertOrUpdate:
                        query.AppendLine(GenerateInsertOrUpdate(typeCache, obj, parameters, properties));
                        break;
                    case GenerateScriptsEnum.GenerateInsert:
                        query.AppendLine(GenerateInsert(typeCache, obj, parameters));
                        break;
                }
            }

            List<string> columns = ExecuterManager.GetInstance().ExecuteQuerySimple<string>(query.ToString(), parameters.ToArray()).ToList();

            for (int i = 0; i < objs.Count; i++)
                typeCache.PrimaryKey.SetValue(objs[i], columns[i].To(typeCache.PrimaryKey.Type));
        }

        public static string GenerateCreate(ReflectionEntityCache typeCache)
        {
            StringBuilder beforeCreate = new StringBuilder();
            StringBuilder query = new StringBuilder();

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                StringBuilder column = new StringBuilder(property.Name);

                if (property.IsPrimaryKey)
                    column.Append($" {property.Type.ToStringDb()} PRIMARY KEY NOT NULL IDENTITY(1,1), ");
                else if (property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                    beforeCreate.AppendLine(GenerateCreate(fk));
                    column.Append($"{fk.PrimaryKey.Name} {fk.PrimaryKey.Type.ToStringDb()} FOREIGN KEY REFERENCES {property.Name}({typeCache.PrimaryKey.Name}) ON DELETE CASCADE, ");
                }
                else
                    column.Append($" {property.Type.ToStringDb()}, ");

                query.AppendLine();
                query.Append($"\t\t{column.ToString()}");
            }

            return
$@"{beforeCreate}
IF  NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{typeCache.Name}')
    BEGIN 
        CREATE TABLE {typeCache.Name} ({query.SubstringLast()})
    END;";
        }

        public static string GenerateInsertOrUpdate<TEntity>(ReflectionEntityCache typeCache, TEntity obj, List<object> parameters, IEnumerable<ReflectionPropertyCache> properties)
        {
            StringBuilder where = new StringBuilder(" WHERE ");
            if (properties != null && properties.Any())
            {
                StringBuilder whereLine = new StringBuilder();

                foreach (ReflectionPropertyCache property in properties)
                {
                    object propValue = property.GetValue(obj);
                    whereLine.Append(property.Name);
                    if (propValue == null)
                        whereLine.Append(" IS NULL");
                    else
                    {
                        whereLine.Append($" = @{parameters.Count()}");
                        parameters.Add(propValue);
                    }
                    whereLine.Append(" AND ");
                }

                where.Append(whereLine.SubstringLast(5));
            }
            else
            {
                where.Append($"{typeCache.PrimaryKey.Name} = @{parameters.Count}");
                parameters.Add(typeCache.PrimaryKey.GetValue(obj));
            }

            string whereStr = where.ToString();
            string select = GenerateGenericSimple(typeCache.Name, $"SELECT {typeCache.PrimaryKey.Name}", whereStr);

            return
$@"IF EXISTS ({select.SubstringLast(1)})
    BEGIN 
        {GenerateUpdate(typeCache, obj, parameters, properties, whereStr)}
        {select}
    END
ELSE
    BEGIN 
        {GenerateInsert(typeCache, obj, parameters)}
    END;";
        }

        public static string GenerateInsert(ReflectionEntityCache typeCache, object obj, List<object> parameters)
        {
            StringBuilder values = new StringBuilder(),
                          columns = new StringBuilder();

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                if (!property.IsPrimaryKey)
                {
                    object propertyVal = property.GetValue(obj);
                    string propertyName = property.Name;

                    if (propertyVal != null && property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                        object fkPkValue = fk.PrimaryKey.GetValue(propertyVal);

                        if (fkPkValue.Equals(fk.PrimaryKey.Type.GetDefaultValue()))
                        {
                            List<object> parametersFK = new List<object>();
                            string insertFK = GenerateInsert(fk, propertyVal, parametersFK);

                            fkPkValue = ExecuterManager.GetInstance().ExecuteQuery(insertFK, parametersFK.ToArray(), x => Mapper.ConvertFromType(x[0], fk.PrimaryKey.Type)).FirstOrDefault();
                            fk.PrimaryKey.SetValue(propertyVal, fkPkValue);
                        }

                        propertyVal = fkPkValue;
                        propertyName = $"{property.Name}{fk.PrimaryKey.Name}";
                    }

                    columns.Append($"{propertyName}, ");
                    parameters.Add(propertyVal ?? string.Empty);
                    values.Append($"@{parameters.Count - 1}, ");
                }
            }

            return
$@"INSERT INTO {typeCache.Name} ({columns.SubstringLast()}) VALUES ({values.SubstringLast()});
SELECT IDENT_CURRENT('{typeCache.Name}');";
        }

        public static string GenerateUpdate<TEntity>(ReflectionEntityCache typeCache, TEntity obj, IList<object> parameters, IEnumerable<ReflectionPropertyCache> properties, string where = null)
        {
            StringBuilder setValues = new StringBuilder();

            where = where ?? $" WHERE {typeCache.PrimaryKey.Name}=@{parameters.Count()}";
            if (properties == null || !properties.Any())
                properties = typeCache.Properties;
            parameters.Add(typeCache.PrimaryKey.GetValue(obj));

            foreach (ReflectionPropertyCache property in properties)
            {
                object propertyVal = property.GetValue(obj);
                if (propertyVal != null && !property.IsPrimaryKey)
                    if (property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                        propertyVal = fk.PrimaryKey.GetValue(propertyVal);
                        if (propertyVal.Equals(fk.PrimaryKey.Type.GetDefaultValue()))
                        {
                            setValues.Append($"{fk.Name}{fk.PrimaryKey.Name}=@{parameters.Count}, ");
                            parameters.Add(propertyVal);
                        }
                    }
                    else
                    {
                        setValues.Append($"{property.Name}=@{parameters.Count}, ");
                        parameters.Add(propertyVal);
                    }
            }
            return $"UPDATE {typeCache.Name} SET {setValues.SubstringLast()} {where};";
        }

        public static string GenerateWhere<TEntity>(Expression<Func<TEntity, bool>> identifierExpression, IExpressionFunctions expressionFunctions)
        {
            // TODO: verificar se é um binary, se for um member concatenar com == true
            BinaryExpression body = identifierExpression.Body as BinaryExpression;
            return $" WHERE {body.ExpressionToString(expressionFunctions).SubstringLast(1)}";
        }

        public static string GenerateSelect(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
        {
            where = where ?? $"WHERE {typeCache.Name.ToLower()}.{fieldName}=@{cont}";
            StringBuilder query = new StringBuilder("SELECT ");
            string joins = GenerateSelectFields(typeCache, query);
            return
$@"{query.SubstringLast()} 
    FROM {typeCache.Name} 
        {joins}
    {where};";
        }

        private static string GenerateSelectFields(ReflectionEntityCache typeCache, StringBuilder query)
        {
            StringBuilder joins = new StringBuilder();

            foreach (ReflectionPropertyCache property in typeCache.Properties)
                if (property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                    joins.AppendLine($"INNER JOIN {fk.Name} {fk.Name.ToLower()} ON {typeCache.Name.ToLower()}.{property.Name}{fk.PrimaryKey.Name} = {fk.Name.ToLower()}.{fk.PrimaryKey.Name}");
                    joins.Append(GenerateSelectFields(fk, query));
                }
                else
                    query.Append($"{typeCache.Name.ToLower()}.{property.Name} AS {typeCache.Name}_{property.Name}, ");

            return joins.ToString();
        }

        public static string GenerateDelete(string tableName, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(tableName, "DELETE", where, cont, fieldName);

        private static string GenerateSelectSimple(string tableName, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(tableName, "SELECT *", where, cont, fieldName);

        private static string GenerateGenericSimple(string tableName, string action, string where, int cont = 0, string fieldName = null)
            => $"{action} FROM {tableName}{where ?? $" WHERE {fieldName}=@{cont}"};";


        public static string GenerateSelectColumns(string tableName)
            => $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}';";

        public static string GenerateColumnMigrations(ReflectionEntityCache typeCache, IEnumerable<InformationSchemaColumns> columns)
        {
            List<string> dbColumns = columns.Select(x => x.Name).ToList();
            StringBuilder query = new StringBuilder();

            //ADD COLUMNS
            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                string propertyName = property.Name;
                ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);

                if (property.IsForeignObj)
                    propertyName = $"{propertyName}{fk.PrimaryKey.Name}";

                if (!dbColumns.Contains(propertyName))
                {
                    string valType = property.Type.ToStringDb();

                    query.AppendLine($"ALTER TABLE {typeCache.Name} ADD {propertyName} {valType};");

                    if (property.IsForeignObj)
                        query.AppendLine($"ALTER TABLE {typeCache.Name} ADD FOREIGN KEY({propertyName}) REFERENCES {fk.Name}({fk.PrimaryKey.Name});");
                }
            }

            //DROP COLUMNS
            foreach (string column in dbColumns)
                if (typeCache.GetProperty(column) == null)
                    query.AppendLine($"ALTER TABLE {typeCache.Name} DROP COLUMN {column};");

            return query.ToString();
        }

        public static string GenerateTypeMigrations(ReflectionEntityCache typeCache, IEnumerable<InformationSchemaColumns> columns)
        {
            StringBuilder query = new StringBuilder();
            foreach (InformationSchemaColumns column in columns)
            {
                ReflectionPropertyCache property = typeCache.GetProperty(column.Name);
                string valType = property.Type.ToStringDb();

                if (!column.TypeString.Equals(Regex.Replace(valType, @"(\(.*\))", string.Empty)))
                    query.AppendLine($"ALTER TABLE {typeCache.Name} ALTER COLUMN {property.Name} {valType};");
            }

            return query.ToString();
        }
    }
}
