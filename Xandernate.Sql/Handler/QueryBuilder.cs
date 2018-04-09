using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xandernate.ReflectionCache;
using Xandernate.Sql.Connection;

namespace Xandernate.Sql.Handler
{
    internal class QueryBuilder<TEntity>
        where TEntity : new()
    {
        public IEnumerable<T> ExecuteQuerySimple<T>(string query, params object[] parameters)
            => ExecuterManager.GetInstance().ExecuteQuerySimple<T>(query, parameters);

        public void GenericAction(ReflectionEntityCache typeCache, IList<TEntity> objs, GenerateScriptsEnum method, IEnumerable<ReflectionPropertyCache> properties = null)
        {
            IList<object> parameters = new List<object>();
            string query = string.Empty;
            ExecuterManager executer = ExecuterManager.GetInstance();

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
                    case GenerateScriptsEnum.GenerateUpdate:
                        query += GenerateUpdate(typeCache, obj, parameters, properties);
                        break;
                }
            }

            List<string> fields = executer.ExecuteQuerySimple<string>(query, parameters.ToArray()).ToList();

            for (int i = 0; i < objs.Count; i++)
                typeCache.PrimaryKey.SetValue(objs[i], fields[i].To(typeCache.PrimaryKey.PropertyType));
        }
        
        public string GenerateCreate(ReflectionEntityCache typeCache)
        {
            string beforeCreate = string.Empty;
            string query = $"CREATE TABLE {typeCache.Name} (";

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                query += property.Name;

                if (property.IsPrimaryKey)
                {
                    query += $" {property.PropertyType.ToStringDb()} PRIMARY KEY NOT NULL IDENTITY(1,1), ";
                }
                else if(property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
                    beforeCreate += GenerateCreate(fk);
                    query = $"{query}{fk.PrimaryKey.Name} {fk.PrimaryKey.PropertyType.ToStringDb()} FOREIGN KEY REFERENCES {property.Name}({typeCache.PrimaryKey.Name}) ON DELETE CASCADE, ";
                }
                else
                    query = $"{query} {property.PropertyType.ToStringDb()}, ";
            }

            query = $"{query.SubstringLast()})";

            return beforeCreate +
                   $"IF  NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{typeCache.Name}')\n" +
                    "   BEGIN\n" +
                   $"       {query};\n" +
                    "   END;\n";
        }

        public string GenerateInsertOrUpdate(ReflectionEntityCache typeCache, TEntity obj, IList<object> parameters, IEnumerable<ReflectionPropertyCache> properties)
        {
            string where = " WHERE ";

            if (properties != null && properties.Any())
            {
                foreach (ReflectionPropertyCache property in properties)
                {
                    object propValue = property.GetValue(obj);
                    where += property.Name;
                    if (propValue == null) where += " IS NULL";
                    else
                    {
                        where += $" = @{parameters.Count()}";
                        parameters.Add(propValue ?? string.Empty);
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

        public string GenerateInsert(ReflectionEntityCache typeCache, object obj, IList<object> parameters)
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
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
                        object idFKValue = fk.PrimaryKey.GetValue(propertyVal);
                        List<object> parametersFK = new List<object>();
                        string insertFK = GenerateInsertFK(fk, propertyVal, parametersFK);
                        if (idFKValue == null || idFKValue.Equals(0))
                            idFKValue = executer.ExecuteQuery(insertFK, parametersFK.ToArray(), x => Mapper.StringToProp(x.GetDecimal(0), fk.PrimaryKey.PropertyType)).FirstOrDefault();
                        else
                            executer.ExecuteQuery(insertFK, parametersFK.ToArray());

                        query += $"{property.Name}{fk.PrimaryKey.Name}, ";
                        parameters.Add(idFKValue);
                        fk.PrimaryKey.SetValue(propertyVal, idFKValue);
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

        private string GenerateInsertFK(ReflectionEntityCache typeCache, object obj, IList<object> parameters)
        {
            object pkValue = typeCache.PrimaryKey.GetValue(obj);

            if (pkValue == null || pkValue.Equals(0))
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
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
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

        public string GenerateUpdate(ReflectionEntityCache typeCache, TEntity obj, IList<object> parameters, IEnumerable<ReflectionPropertyCache> properties, string where = null)
        {
            string pkField = typeCache.PrimaryKey.Name;
            string query = $"UPDATE {typeCache.Name} SET ";

            where = where ?? $" WHERE {pkField}=@{parameters.Count()}";
            if (properties == null || !properties.Any())
                properties = typeCache.Properties;
            parameters.Add(typeCache.PrimaryKey.GetValue(obj));

            foreach (ReflectionPropertyCache property in properties)
            {
                object propertyVal = property.GetValue(obj);
                if (propertyVal != null && !property.IsPrimaryKey)
                    if (property.IsForeignObj)
                    {
                        ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
                        propertyVal = fk.PrimaryKey.GetValue(propertyVal);
                        if (!propertyVal.Equals(0))
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
SELECT {pkField} FROM {typeCache.Name}{where};
";
        }

        public string GenerateSelect(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
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

        private string GenerateSelectFields(ReflectionEntityCache typeCache, ref string query)
        {
            string joins = string.Empty;

            foreach (ReflectionPropertyCache property in typeCache.Properties)
                if (property.IsForeignObj)
                {
                    ReflectionEntityCache fk = ReflectionEntityCache.GetOrCreateEntity(property.PropertyType);
                    joins = $"{joins}INNER JOIN {fk.Name} {fk.Name.ToLower()} ON {typeCache.Name.ToLower()}.{property.Name}{fk.PrimaryKey.Name} = {fk.Name.ToLower()}.{fk.PrimaryKey.Name} \n";
                    joins = $"{joins}{GenerateSelectFields(fk, ref query)}";
                }
                else
                    query = $"{query}{typeCache.Name.ToLower()}.{property.Name} AS {typeCache.Name}_{property.Name}, ";

            return joins;
        }

        public string GenerateDelete(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(typeCache, "DELETE", fieldName, where, cont);

        private string GenerateSelectSimple(ReflectionEntityCache typeCache, string fieldName, string where = null, int cont = 0)
            => GenerateGenericSimple(typeCache, "SELECT *", fieldName, where, cont);

        private string GenerateGenericSimple(ReflectionEntityCache typeCache, string action, string fieldName, string where, int cont)
            => $"{action} FROM {typeCache.Name}{where ?? $" WHERE {fieldName}=@{cont}"};\n";
    }
}
