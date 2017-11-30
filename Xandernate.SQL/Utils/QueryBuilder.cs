using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xandernate.SQL.DAO;
using Xandernate.SQL.Utils.Extensions;
using Xandernate.Utils.Extensions;

namespace Xandernate.SQL.Utils
{
    public static class QueryBuilder
    {
        public static List<T> ExecuteQuerySimple<T>(string query, params object[] parameters)
            => ExecuterManager.GetInstance().ExecuteQuerySimple<T>(query, parameters);

        public static void GenericAction<TDao>(TDao[] objs, GenerateScriptsEnum method)
        {
            GenericAction(objs, method, null);
        }

        public static void GenericAction<TDao>(TDao[] objs, GenerateScriptsEnum method, PropertyInfo[] properties)
        {
            Type type = typeof(TDao);
            List<object> parameters = new List<object>();
            string query = "";
            List<string> fields = null;
            ExecuterManager executer = ExecuterManager.GetInstance();
            PropertyInfo idProperty = type.GetIdField();

            foreach (TDao obj in objs)
            {
                switch (method)
                {
                    case GenerateScriptsEnum.GenerateInsertOrUpdate:
                        query += GenerateInsertOrUpdate(obj, parameters, properties);
                        break;
                    case GenerateScriptsEnum.GenerateInsert:
                        query += GenerateInsert(obj, parameters);
                        break;
                    case GenerateScriptsEnum.GenerateUpdate:
                        query += GenerateUpdate(obj, parameters, null, properties);
                        break;
                }
            }
            fields = executer.ExecuteQuerySimple<string>(query, parameters.ToArray());

            for (int i = 0; i < objs.Length; i++)
                idProperty.SetValue(objs[i], idProperty.PropertyType.TypeToObject(fields[i]));
        }

        public static string GenerateCreate(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            string beforeCreate = "";
            string valType = "";
            string query = "CREATE TABLE " + type.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (property.IsPrimaryKey())
                {
                    valType = " int primary key not null";

                    switch (DataManager.DbType)
                    {
                        case DBTypes.Sql:
                            valType += " IDENTITY(1,1)";
                            break;
                        case DBTypes.Oracle:
                            beforeCreate +=
                        "    BEGIN\n" +
                        "        EXECUTE IMMEDIATE 'CREATE SEQUENCE seq_" + type.Name + "_" + property.Name + " MINVALUE 1 START WITH 1 INCREMENT BY 1';\n" +
                        "    EXCEPTION\n" +
                        "        WHEN OTHERS THEN\n" +
                        "        IF SQLCODE != -955 THEN\n" +
                        "            RAISE;\n" +
                        "        END IF;\n" +
                        "    END;\n";
                            break;
                    }

                    valType += ", ";
                }
                else if (property.IsForeignKey())
                {
                    beforeCreate += GenerateCreate(property.PropertyType);
                    valType = "Id int " + (DataManager.DbType == DBTypes.Oracle ? "" : "FOREIGN KEY")
                        + " REFERENCES " + property.PropertyType.Name + "(" + property.PropertyType.GetIdField().Name + ") ON DELETE CASCADE, ";
                }
                else
                    valType = " " + property.PropertyType.TypeToStringDB() + ", ";

                query += property.Name + valType;
            }

            query = query.SubstringLast() + ")";


            switch (DataManager.DbType)
            {
                case DBTypes.Oracle:
                    return beforeCreate +
                        "    BEGIN\n" +
                        "        EXECUTE IMMEDIATE '" + query + "';\n" +
                        "    EXCEPTION\n" +
                        "        WHEN OTHERS THEN\n" +
                        "        IF SQLCODE != -955 THEN\n" +
                        "            RAISE;\n" +
                        "        END IF;\n" +
                        "    END;\n";
                case DBTypes.Sql:
                default:
                    return beforeCreate +
                        "IF  NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + type.Name + "')\n" +
                        "   BEGIN\n" +
                        "       " + query + ";\n" +
                        "   END;\n";
            }
        }

        public static string GenerateInsertOrUpdate(object obj, List<object> parameters, PropertyInfo[] properties)
        {
            Type type = obj.GetType();
            PropertyInfo idProperty = type.GetIdField();
            PropertyInfo[] typeProperties = type.GetProperties();
            string where = " WHERE ";
            object propValue;

            if (properties != null && properties.Any())
            {
                foreach (PropertyInfo property in properties)
                {
                    propValue = property.GetValue(obj);
                    where += property.Name;
                    if (propValue == null) where += " IS NULL";
                    else
                    {
                        where += " = @" + parameters.Count;
                        parameters.Add(propValue ?? "");
                    }
                    where += " AND ";
                }
                where = where.Substring(0, where.Length - 5);
            }
            else
            {
                where += type.GetIdField().Name + " = @" + parameters.Count;
                parameters.Add(type.GetIdField().GetValue(obj));
            }

            return "IF EXISTS (" + GenerateSelectSimple("", type, where).SubstringLast() + ")\n" +
                    "  BEGIN \n" +
                    "    " + GenerateUpdate(obj, parameters, where) +
                    "  END \n" +
                    "ELSE\n" +
                    "  BEGIN \n" +
                    "    " + GenerateInsert(obj, parameters) +
                    "  END;\n";
        }

        public static string GenerateInsert(object obj, List<object> parameters)
        {
            ExecuterManager executer = ExecuterManager.GetInstance();
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();
            object idFKValue;
            List<object> parametersFK;
            object propertyVal;
            string insertFK = "",
                   currId = "",
                   values = "",
                   query = "INSERT INTO " + type.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                propertyVal = property.GetValue(obj);
                if (!property.IsPrimaryKey())
                {
                    if (propertyVal != null && property.IsForeignKey())
                    {
                        idFKValue = property.PropertyType.GetIdField().GetValue(propertyVal);
                        parametersFK = new List<object>();
                        insertFK = GenerateInsertFK(propertyVal, parametersFK);
                        if (idFKValue == null || idFKValue.Equals(0))
                            idFKValue = executer.ExecuteQuery(insertFK, parametersFK.ToArray(), x => Mapper.StringToProp(x.GetDecimal(0), property.PropertyType.GetIdField().PropertyType))[0];
                        else
                            executer.ExecuteQuery(insertFK, parametersFK.ToArray());

                        query += property.Name + "Id, ";
                        parameters.Add(idFKValue);
                        property.PropertyType.GetIdField().SetValue(propertyVal, idFKValue);
                    }
                    else
                    {
                        query += property.Name + ", ";
                        parameters.Add(propertyVal ?? "");
                    }
                    values += "@" + (parameters.Count - 1).ToString() + ", ";
                }
                else if (DataManager.DbType == DBTypes.Oracle)
                {
                    query += property.Name + ", ";
                    values += "seq_" + type.Name + "_" + property.Name + ".nextval, ";
                }
            }

            switch (DataManager.DbType)
            {
                case DBTypes.Oracle: currId = "SELECT dual.seq_" + type.Name + "_" + type.GetIdField().Name + ".currval from dual"; break;
                default: currId = "SELECT IDENT_CURRENT('" + type.Name + "')"; break;
            }

            return query.SubstringLast() + ") VALUES (" + values.SubstringLast() + ");\n" + currId + ";\n";
        }

        private static string GenerateInsertFK(object obj, List<object> parameters)
        {
            Type type = obj.GetType();
            object propertyVal;
            object idValue = type.GetIdField().GetValue(obj);
            if (idValue == null || idValue.Equals(0))
                return GenerateInsert(obj, parameters);
            PropertyInfo[] properties = type.GetProperties();
            string values = "",
                   beforeInsert = "";
            int idIndex = parameters.Count;
            string query = "IF NOT EXISTS (" + GenerateSelectSimple(type.GetIdField().Name, type, cont: idIndex).SubstringLast() + ")\n" +
                           "  BEGIN \n" +
                            (DataManager.DbType == DBTypes.Sql ?
                           "    SET IDENTITY_INSERT " + type.Name + " ON \n" : "") +
                           "    INSERT INTO " + type.Name + " (";
            parameters.Add(idValue);

            foreach (PropertyInfo property in properties)
            {
                propertyVal = property.GetValue(obj);
                if (!property.IsPrimaryKey())
                {
                    if (propertyVal != null && property.IsForeignKey())
                    {
                        beforeInsert += GenerateInsertFK(propertyVal, parameters);

                        query += property.Name + "Id, ";
                        parameters.Add(property.PropertyType.GetIdField().GetValue(propertyVal));
                    }
                    else
                    {
                        query += property.Name + ", ";
                        parameters.Add(propertyVal ?? "");
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
                         (DataManager.DbType == DBTypes.Sql ?
                         "    SET IDENTITY_INSERT " + type.Name + " OFF \n" : "") +
                         "  END\n";
        }

        public static string GenerateUpdate(object obj, List<object> parameters, string where = null, params PropertyInfo[] properties)
        {
            Type type = obj.GetType();
            object propertyVal;
            string IdField = type.GetIdField().Name;
            object IdValue = type.GetIdField().GetValue(obj);
            string query = "UPDATE " + type.Name + " SET ";

            where = where ?? " WHERE " + IdField + "=@" + parameters.Count;
            if (properties == null || !properties.Any())
                properties = type.GetProperties();
            parameters.Add(IdValue);

            foreach (PropertyInfo property in properties)
            {
                propertyVal = property.GetValue(obj);
                if (propertyVal != null && !property.IsPrimaryKey())
                    if (property.IsForeignKey())
                    {
                        propertyVal = property.PropertyType.GetIdField().GetValue(propertyVal);
                        if (!propertyVal.Equals(0))
                        {
                            query += property.Name + "Id=@" + parameters.Count + ", ";
                            parameters.Add(propertyVal);
                        }
                    }
                    else
                    {
                        query += property.Name + "=@" + parameters.Count + ", ";
                        parameters.Add(propertyVal ?? "");
                    }
            }
            return query.SubstringLast() + where + ";\n"
                 + "SELECT " + IdField + " FROM " + type.Name + where + ";\n";
        }

        public static string GenerateSelect(string fieldName, Type type, string where = null, int cont = 0)
        {
            where = where ?? ("WHERE " + type.Name.ToLower() + "." + fieldName + "=@" + cont);
            string query = "SELECT ";
            string joins = "";
            joins = GenerateSelectFields(type, ref query);
            return query.SubstringLast() + " \n" +
                    "FROM " + type.Name + " \n" + joins + where + ";\n";
        }

        private static string GenerateSelectFields(Type type, ref string query)
        {
            Type fk;
            string joins = "";
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
                if (property.IsForeignKey())
                {
                    fk = property.PropertyType;
                    joins += "inner join " + fk.Name + " " + fk.Name.ToLower() + " on  " + type.Name.ToLower() + "." + property.Name + "Id = " + fk.Name.ToLower() + "." + fk.GetIdField().Name + " \n"
                          + GenerateSelectFields(fk, ref query);
                }
                else
                    query += type.Name.ToLower() + "." + property.Name + " as " + type.Name + "_" + property.Name + ", ";

            return joins;
        }

        public static string GenerateDelete(string fieldName, Type type, string where = null, int cont = 0)
        {
            return GenerateGenericSimple("DELETE", fieldName, type, where, cont);
        }

        private static string GenerateSelectSimple(string fieldName, Type type, string where = null, int cont = 0)
        {
            return GenerateGenericSimple("SELECT *", fieldName, type, where, cont);
        }

        private static string GenerateGenericSimple(string action, string fieldName, Type type, string where, int cont)
        {
            return action + " FROM " + type.Name + (where ?? (" WHERE " + fieldName + "=@" + cont)) + ";\n";
        }
    }
}
