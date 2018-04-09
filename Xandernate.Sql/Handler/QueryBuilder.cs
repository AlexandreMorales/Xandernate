using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            IList<object> parameters = new List<object>();
            string query = string.Empty;

            foreach (TEntity obj in objs)
            {
                switch (method)
                {
                    case GenerateScriptsEnum.GenerateInsertOrUpdate:
                        query += GenerateInsertOrUpdate(typeCache, obj, parameters, properties);
                        break;
                    case GenerateScriptsEnum.GenerateInsert:
                        query += GenerateInsert(typeCache, obj, parameters);
                        break;
                }
            }

            List<string> columns = ExecuterManager.GetInstance().ExecuteQuerySimple<string>(query, parameters.ToArray()).ToList();

            for (int i = 0; i < objs.Count; i++)
                typeCache.PrimaryKey.SetValue(objs[i], columns[i].To(typeCache.PrimaryKey.Type));
        }

        public static string GenerateCreate(ReflectionEntityCache typeCache)
        {
            string beforeCreate = string.Empty;
            string query = $"CREATE TABLE {typeCache.Name} (";

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                query += property.Name;

                if (property.IsPrimaryKey)
                {
                    query += $" {property.Type.ToStringDb()} PRIMARY KEY NOT NULL IDENTITY(1,1), ";
                }
                else if(property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                    beforeCreate += GenerateCreate(fk);
                    query = $"{query}{fk.PrimaryKey.Name} {fk.PrimaryKey.Type.ToStringDb()} FOREIGN KEY REFERENCES {property.Name}({typeCache.PrimaryKey.Name}) ON DELETE CASCADE, ";
                }
                else
                    query = $"{query} {property.Type.ToStringDb()}, ";
            }

            query = $"{query.SubstringLast()})";

            return beforeCreate +
                   $"IF  NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{typeCache.Name}')\n" +
                    "   BEGIN\n" +
                   $"       {query};\n" +
                    "   END;\n";
        }

        public static string GenerateInsertOrUpdate<TEntity>(ReflectionEntityCache typeCache, TEntity obj, IList<object> parameters, IEnumerable<ReflectionPropertyCache> properties)
        {
            string where = " WHERE ";

            if (properties != null && properties.Any())
            {
                foreach (ReflectionPropertyCache property in properties)
                {
                    object propValue = property.GetValue(obj);
                    where += property.Name;
                    if (propValue == null)
                        where += " IS NULL";
                    else
                    {
                        where += $" = @{parameters.Count()}";
                        parameters.Add(propValue);
                    }
                    where += " AND ";
                }
                where = where.Substring(0, where.Length - 5);
            }
            else
            {
                where += $"{typeCache.PrimaryKey.Name} = @{parameters.Count}";
                parameters.Add(typeCache.PrimaryKey.GetValue(obj));
            }

            return $"IF EXISTS ({GenerateSelectSimple(typeCache, string.Empty, where).SubstringLast()})\n" +
                    "  BEGIN \n" +
                   $"    {GenerateUpdate(typeCache, obj, parameters, properties, where)}" +
                    "  END \n" +
                    "ELSE\n" +
                    "  BEGIN \n" +
                   $"    {GenerateInsert(typeCache, obj, parameters)}" +
                    "  END;\n";
        }

        public static string GenerateInsert(ReflectionEntityCache typeCache, object obj, IList<object> parameters)
        {
            Type type = obj.GetType();
            ExecuterManager executer = ExecuterManager.GetInstance();
            string values = string.Empty,
                   query = $"INSERT INTO {typeCache.Name} (";

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                if (!property.IsPrimaryKey)
                {
                    object propertyVal = property.GetValue(obj);

                    if (propertyVal != null && property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                        object fkPkValue = fk.PrimaryKey.GetValue(propertyVal);
                        List<object> parametersFK = new List<object>();
                        string insertFK = GenerateInsertFK(fk, propertyVal, parametersFK);
                        if (fkPkValue.Equals(fk.PrimaryKey.Type.GetDefaultValue()))
                            fkPkValue = executer.ExecuteQuery(insertFK, parametersFK.ToArray(), x => Mapper.ConvertFromType(x.GetDecimal(0), fk.PrimaryKey.Type)).FirstOrDefault();
                        else
                            executer.ExecuteQuery(insertFK, parametersFK.ToArray());

                        query += $"{property.Name}{fk.PrimaryKey.Name}, ";
                        parameters.Add(fkPkValue);
                        fk.PrimaryKey.SetValue(propertyVal, fkPkValue);
                    }
                    else
                    {
                        query += property.Name + ", ";
                        parameters.Add(propertyVal ?? string.Empty);
                    }
                    values += $"@{(parameters.Count - 1).ToString()}, ";
                }
            }

            return $"{query.SubstringLast()}) VALUES ({values.SubstringLast()});\nSELECT IDENT_CURRENT('{typeCache.Name}');\n";
        }

        private static string GenerateInsertFK(ReflectionEntityCache typeCache, object obj, IList<object> parameters)
        {
            object pkValue = typeCache.PrimaryKey.GetValue(obj);

            if (pkValue.Equals(typeCache.PrimaryKey.Type.GetDefaultValue()))
                return GenerateInsert(typeCache, obj, parameters);
            
            string values = string.Empty,
                   beforeInsert = string.Empty;
            int idIndex = parameters.Count;
            string query = "IF NOT EXISTS (" + GenerateSelectSimple(typeCache, typeCache.PrimaryKey.Name, cont: idIndex).SubstringLast() + ")\n" +
                           "  BEGIN \n" +
                           "    SET IDENTITY_INSERT " + typeCache.Name + " ON \n" +
                           "    INSERT INTO " + typeCache.Name + " (";
            parameters.Add(pkValue);

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                object propertyVal = property.GetValue(obj);
                if (!property.IsPrimaryKey)
                {
                    if (propertyVal != null && property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                        beforeInsert += GenerateInsertFK(fk, propertyVal, parameters);

                        query += $"{property.Name}{fk.PrimaryKey.Name}, ";
                        parameters.Add(fk.PrimaryKey.GetValue(propertyVal));
                    }
                    else
                    {
                        query += $"{property.Name}, ";
                        parameters.Add(propertyVal ?? string.Empty);
                    }
                    values += "@" + (parameters.Count - 1).ToString() + ", ";
                }
                else
                {
                    query += property.Name + ", ";
                    values += "@" + idIndex.ToString() + ", ";
                }
            }

            return beforeInsert + query.SubstringLast() + ") VALUES (" + values.SubstringLast() + ");\n" +
                         "    SET IDENTITY_INSERT " + typeCache.Name + " OFF \n" +
                         "  END\n";
        }

        public static string GenerateUpdate<TEntity>(ReflectionEntityCache typeCache, TEntity obj, IList<object> parameters, IEnumerable<ReflectionPropertyCache> properties, string where = null)
        {
            string query = $"UPDATE {typeCache.Name} SET ";

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
                            query = $"{query}{fk.Name}{fk.PrimaryKey.Name}=@{parameters.Count}, ";
                            parameters.Add(propertyVal);
                        }
                    }
                    else
                    {
                        query = $"{query}{property.Name}=@{parameters.Count}, ";
                        parameters.Add(propertyVal ?? string.Empty);
                    }
            }
            return 
$@"{query.SubstringLast()}{where};
SELECT {typeCache.PrimaryKey.Name} FROM {typeCache.Name}{where};
";
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
            string query = "SELECT ";
            string joins = GenerateSelectFields(typeCache, ref query);
            return 
$@"{query.SubstringLast()} 
    FROM {typeCache.Name} 
    {joins}{where};
";
        }

        private static string GenerateSelectFields(ReflectionEntityCache typeCache, ref string query)
        {
            string joins = string.Empty;

            foreach (ReflectionPropertyCache property in typeCache.Properties)
                if (property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                    joins = $"{joins}INNER JOIN {fk.Name} {fk.Name.ToLower()} ON {typeCache.Name.ToLower()}.{property.Name}{fk.PrimaryKey.Name} = {fk.Name.ToLower()}.{fk.PrimaryKey.Name} \n";
                    joins = $"{joins}{GenerateSelectFields(fk, ref query)}";
                }
                else
                    query = $"{query}{typeCache.Name.ToLower()}.{property.Name} AS {typeCache.Name}_{property.Name}, ";

            return joins;
        }

        public static string GenerateDelete(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(typeCache, "DELETE", fieldName, where, cont);

        private static string GenerateSelectSimple(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(typeCache, "SELECT *", fieldName, where, cont);

        private static string GenerateGenericSimple(ReflectionEntityCache typeCache, string action, string fieldName, string where, int cont)
            => $"{action} FROM {typeCache.Name}{where ?? $" WHERE {fieldName}=@{cont}"};\n";


        public static string ColumnMigrations(ReflectionEntityCache typeCache, IEnumerable<InformationSchemaColumns> columns)
        {
            List<string> dbColumns = columns.Select(x => x.Name).ToList();
            string query = string.Empty;

            //ADD FIELDS
            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                if (!dbColumns.Contains(property.Name) && !dbColumns.Contains($"{property.Name}Id"))
                {
                    string fkPkName = string.Empty;
                    string valType = property.Type.ToStringDb();

                    if (property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.Type);
                        fkPkName = fk.PrimaryKey.Name;
                    }

                    query += $"ALTER TABLE {typeCache.Name} ADD {property.Name}{fkPkName} {valType};\n";

                    if (!string.IsNullOrEmpty(fkPkName))
                        query = $"ALTER TABLE {typeCache.Name} ADD FOREIGN KEY({property.Name}{fkPkName}) REFERENCES {property.Type.Name}({fkPkName});\n";
                }
            }

            //DROP FIELDS
            foreach (string column in dbColumns)
                if (typeCache.GetProperty(column) == null)
                    query += $"ALTER TABLE {typeCache.Name} DROP COLUMN {column};\n";

            return query;
        }

        public static string TypeMigrations(ReflectionEntityCache typeCache, IEnumerable<InformationSchemaColumns> columns)
        {
            string query = string.Empty;
            foreach (InformationSchemaColumns column in columns)
            {
                ReflectionPropertyCache property = typeCache.GetProperty(column.Name);
                string valType = property.Type.ToStringDb();

                if (!column.TypeString.Equals(Regex.Replace(valType, @"(\(.*\))", string.Empty)))
                    query += $"ALTER TABLE {typeCache.Name} ALTER COLUMN {property.Name} {valType};\n";
            }

            return query;
        }
    }
}
