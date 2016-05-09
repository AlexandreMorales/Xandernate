using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    public class DbDao<Classe> : ExecuterManager, IDbDao<Classe>
    {
        /*         
         * FOREIGN KEY | NUMERIC         
         */

        private Type TypeReflection;

        public DbDao(string _database = null, string _provider = null)
            : base(_database, _provider)
        {
            TypeReflection = typeof(Classe);
            CreateTable();
            Migrations();
        }

        public Classe Add(Classe obj)
        {
            List<Object> parameters = new List<Object>();

            string query = CreateInsert(obj, ref parameters);

            switch (DataManager.Provider.ToLower())
            {
                case "odp.net":
                    query += "SELECT seq_" + TypeReflection.Name + "_" + GetIdField() + ".currval FROM dual; ";
                    break;
                default: query += "SELECT CAST(SCOPE_IDENTITY() as int); "; break;
            }


            List<string[]> fields = executeQuery(query, parameters.ToArray());
            PropertyInfo idProperty = TypeReflection.GetProperty(GetIdField());

            idProperty.SetValue(obj, ConvertToType(idProperty, fields[0][0]));
            return obj;
        }

        public void AddRange(params Classe[] objs)
        {
            List<Object> parameters = new List<Object>();
            string query = "";
            foreach (Classe obj in objs)
            {
                query += CreateInsert(obj, ref parameters);
            }
            executeQueryNoReturn(query, parameters.ToArray());
        }

        public Classe Find(Object id)
        {
            string query = "";

            string IdField = GetIdField();
            query = CreateSelect(IdField);

            return MapToObjects(executeQuery(query, id)).OfType<Classe>().FirstOrDefault();
        }

        public Classe Find<Att>(Expression<Func<Classe, Att>> expression, Att value)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            var query = CreateSelect(fieldName);

            return MapToObjects(executeQuery(query, value)).OfType<Classe>().FirstOrDefault();
        }

        public Classe[] FindAll()
        {
            string query = CreateSelect("", where: "");

            return MapToObjects(executeQuery(query)).OfType<Classe>().ToArray();
        }

        public void Remove(Classe obj)
        {
            string IdField = GetIdField();

            string query = CreateDelete(IdField);

            executeQueryNoReturn(query, TypeReflection.GetProperty(IdField).GetValue(obj, null));
        }

        public void Remove(Object id)
        {
            string query = CreateDelete(GetIdField());

            executeQueryNoReturn(query, id);
        }

        public void Remove(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = body.ExpressionToString();
            string query = CreateDelete("", where: validation);

            executeQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<Classe, Att>> expression, Att value)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            string query = CreateDelete(fieldName);

            executeQueryNoReturn(query, value);
        }

        public void AddOrUpdate(params Classe[] objs)
        {
            string IdField = GetIdField();
            object idValue;

            foreach (var obj in objs)
            {
                idValue = TypeReflection.GetProperty(IdField).GetValue(obj, null);

                if (Find(idValue) == null)
                    Add(obj);
                else
                    Update(obj);
            }
        }

        public void AddOrUpdate<Att>(Expression<Func<Classe, Att>> expression, params Classe[] objs)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            Att value;

            foreach (var obj in objs)
            {
                value = (Att)TypeReflection.GetProperty(fieldName).GetValue(obj, null);

                if (Find(expression, value) == null)
                    Add(obj);
                else
                    Update(obj);
            }
        }

        public void InsertOrUpdate(Classe obj, Expression<Func<Classe, object>> IdentifierExpression = null)
        {
            List<Object> parameters = new List<Object>();
            string where = string.Empty;
            object propValue;

            if (IdentifierExpression != null)
            {
                var expression = IdentifierExpression.Body as NewExpression;
                foreach (var member in expression.Members)
                {
                    propValue = TypeReflection.GetProperty(member.Name).GetValue(obj, null);
                    where += member.Name;
                    if (propValue == null) where += " IS NULL";
                    else
                    {
                        where += " = @" + parameters.Count;
                        parameters.Add(propValue);
                    }
                    where += " AND ";
                }
                where = where.Substring(0, where.Length - 5);
            }
            else
            {
                where = "Id = @0";
                parameters.Add(TypeReflection.GetProperty(GetIdField()).GetValue(obj, null));
            }

            string sql = "IF EXIST (" + CreateSelect("", where: where) + ")" +
                            "BEGIN " + CreateUpdate(obj, ref parameters, where: where) + " END " +
                         "ELSE BEGIN " + CreateInsert(obj, ref parameters) + " END; ";

            executeQueryNoReturn(sql, parameters.ToArray());
        }

        public Classe Update(Classe obj)
        {
            List<Object> parameters = new List<Object>();

            string query = CreateUpdate(obj, ref parameters) + CreateSelect(GetIdField());

            return MapToObjects(executeQuery(query, parameters.ToArray())).OfType<Classe>().FirstOrDefault();
        }

        public Classe Update(Classe obj, Expression<Func<Classe, object>> expressions)
        {
            var properties = (expressions.Body as NewExpression).Members.Select(x => TypeReflection.GetProperty(x.Name)).ToArray();
            List<Object> parameters = new List<Object>();

            string query = CreateUpdate(obj, ref parameters, properties) + CreateSelect(GetIdField());

            return MapToObjects(executeQuery(query, parameters.ToArray())).OfType<Classe>().FirstOrDefault();
        }

        public void UpdateRange(params Classe[] objs)
        {
            List<Object> parameters = new List<Object>();
            string query = "";
            foreach (Classe obj in objs)
            {
                query += CreateUpdate(obj, ref parameters);
            }
            executeQueryNoReturn(query, parameters.ToArray());
        }

        public Classe[] WhereEquals<Att>(Expression<Func<Att>> expression)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            Att Value = expression.Compile()();

            string query = CreateSelect(fieldName);

            return MapToObjects(executeQuery(query, Value)).OfType<Classe>().ToArray();
        }

        public Classe[] Where(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = body.ExpressionToString();

            string query = CreateSelect("", where: validation);

            return MapToObjects(executeQuery(query)).OfType<Classe>().ToArray();
        }




        private void Migrations()
        {
            PropertyInfo[] properties = TypeReflection.GetProperties();
            string[] fieldsDB = GetFieldsNames(TypeReflection.Name);
            ColumnMigrations(properties, fieldsDB);
            string[] typesDB = GetFieldsTypes(TypeReflection.Name);
            TypeMigrations(properties, fieldsDB, typesDB);
        }

        private void CreateTable()
        {
            string query = "";
            string valType = "";
            string seqOr = "";
            string columnsSchema = "INFORMATION_SCHEMA.TABLES";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; break;
            }

            PropertyInfo[] properties = TypeReflection.GetProperties();

            query = "IF  NOT EXISTS (SELECT * FROM " + columnsSchema + " WHERE TABLE_NAME = '" + TypeReflection.Name + "') " +
                    "BEGIN " +
                    "CREATE TABLE " + TypeReflection.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (IsId(property))
                {
                    valType = " int primary key not null";

                    switch (DataManager.Provider.ToLower())
                    {
                        case "sqlclient":
                            valType += " IDENTITY(1,1)";
                            break;
                        case "odp.net":
                            seqOr = "CREATE SEQUENCE seq_" + TypeReflection.Name + "_" + property.Name +
                            " MINVALUE 1 START WITH 1 INCREMENT BY 1; ";
                            break;
                    }

                    valType += ", ";
                }
                else if (IsNotPrimitive(property.PropertyType))
                {
                    valType = "Id int FOREIGN KEY REFERENCES " + property.PropertyType.Name + "(" + GetIdFK(property).Name + ") ON DELETE CASCADE, ";
                }
                else
                {
                    switch (property.PropertyType.ToString().Substring(7))
                    {
                        case "Int32":
                            valType = " int, ";
                            break;
                        case "Double":
                            valType = " decimal(18,2), ";
                            break;
                        case "String":
                            valType = " varchar(255), ";
                            break;
                        case "Datetime":
                            valType = " datetime2, ";
                            break;
                        default:
                            valType = property.PropertyType.ToString().Substring(7) + ", ";
                            break;
                    }
                }
                query += property.Name + valType;
            }
            query = query.Substring(0, (query.Length - 2)) + ")" +
                    " END; " + seqOr;

            executeQueryNoReturn(query);
        }

        private static bool IsNotPrimitive(Type property)
        {
            return !property.IsPrimitive &&
                                property != typeof(Decimal) &&
                                property != typeof(String) &&
                                property != typeof(DateTime);
        }

        private bool IsId(PropertyInfo property)
        {
            return property.Name.ToLower().Equals("id") ||
                                property.Name.ToLower().Equals("id" + TypeReflection.Name.ToLower()) ||
                                property.Name.ToLower().Equals(TypeReflection.Name.ToLower() + "id") ||
                                property.Name.ToLower().Equals("id_" + TypeReflection.Name.ToLower()) ||
                                property.Name.ToLower().Equals(TypeReflection.Name.ToLower() + "_id");
        }

        private string GetIdField(Type type = null)
        {
            type = type ?? TypeReflection;
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (IsId(property))
                {
                    return property.Name;
                }
            }
            return null;
        }

        private PropertyInfo GetIdFK(PropertyInfo property)
        {
            Type fk = property.PropertyType;
            PropertyInfo[] fkAtt = fk.GetProperties();
            foreach (var item in fkAtt)
                if (IsId(item))
                    return item;
            return null;
        }

        private Object FindFK(Object id, Type type)
        {
            string IdField = GetIdField(type);

            string query = CreateSelect(IdField, type);

            return MapToObjects(executeQuery(query, id), type).FirstOrDefault();
        }

        private Object[] GetFields(string[] fields, Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            Object[] objs = new Object[properties.Length];
            string[] fieldsName = GetFieldsNames(type.Name);

            for (int i = 0; i < fields.Length; i++)
            {
                foreach (var property in properties)
                {
                    if (property.Name.Equals(fieldsName[i]) || (property.Name + "Id").Equals(fieldsName[i]))
                    {
                        if (IsNotPrimitive(property.PropertyType))
                            objs[i] = FindFK(fields[i], property.PropertyType);
                        else
                            objs[i] = ConvertToType(property, fields[i]);
                        break;
                    }
                }
            }

            return objs;
        }

        private Object ConvertToType(PropertyInfo property, string value)
        {
            switch (property.PropertyType.ToString().Substring(7))
            {
                case "Int32": return (value.Equals("")) ? default(Int32) : Convert.ToInt32(value);
                case "Double": return (value.Equals("")) ? default(Double) : Convert.ToDouble(value);
                case "DateTime": return (value.Equals("")) ? default(DateTime) : Convert.ToDateTime(value);
            }
            return value;
        }

        private void ColumnMigrations(PropertyInfo[] properties, string[] fieldsDB)
        {
            string[] propertiesName = properties.Select(p => p.Name).ToArray();
            string query = "";
            string valType = "";
            string fk = "";

            foreach (var property in properties)
            {
                fk = "";
                if (!fieldsDB.Contains(property.Name) && !fieldsDB.Contains(property.Name + "Id"))
                {
                    if (IsNotPrimitive(property.PropertyType))
                    {
                        fk = "Id";
                        valType = "int";
                    }
                    else
                    {
                        switch (property.PropertyType.ToString().Substring(7))
                        {
                            case "Int32":
                                valType = "int";
                                break;
                            case "Double":
                                valType = "decimal(18,2)";
                                break;
                            case "String":
                                valType = "varchar(255)";
                                break;
                            case "Datetime":
                                valType = "datetime2";
                                break;
                            default:
                                valType = property.PropertyType.ToString().Substring(7);
                                break;
                        }
                    }
                    query += "ALTER TABLE " + TypeReflection.Name + " ADD " + property.Name + fk + " " + valType + "; ";

                    if (!fk.Equals(""))
                        query = "ALTER TABLE " + TypeReflection.Name + " ADD FOREIGN KEY(" + property.Name + fk + ") REFERENCES " + property.PropertyType.Name + "(" + GetIdFK(property).Name + "); ";
                }
            }

            foreach (var column in fieldsDB)
            {
                if (!propertiesName.Contains(column) && !propertiesName.Contains(column.Replace("Id", "")))
                    query += "ALTER TABLE " + TypeReflection.Name + " DROP COLUMN " + column + "; ";
            }

            if (!String.IsNullOrEmpty(query))
                executeQuery(query);
        }

        private void TypeMigrations(PropertyInfo[] properties, string[] fieldsDB, string[] typesDB)
        {
            string valType = "";
            string query = "";

            for (int i = 0; i < fieldsDB.Length; i++)
            {
                foreach (var property in properties)
                {
                    if (property.Name.Equals(fieldsDB[i]) || (property.Name + "Id").Equals(fieldsDB[i]))
                    {
                        if (IsNotPrimitive(property.PropertyType))
                        {
                            valType = "int";
                        }
                        else
                        {
                            switch (property.PropertyType.ToString().Substring(7))
                            {
                                case "Int32":
                                    valType = "int";
                                    break;
                                case "Double":
                                    valType = "decimal";
                                    break;
                                case "String":
                                    valType = "varchar";
                                    break;
                                case "Datetime":
                                    valType = "datetime2";
                                    break;
                                default:
                                    valType = property.PropertyType.ToString().Substring(7);
                                    break;
                            }
                        }
                        if (!typesDB[i].Equals(valType))
                            query += "ALTER TABLE " + TypeReflection.Name + " ALTER COLUMN " + property.Name + " " + valType + "; ";

                    }
                }
            }
            if (!String.IsNullOrEmpty(query))
                executeQuery(query);
        }

        private Object[] MapToObjects(List<string[]> fields, Type type = null)
        {
            type = type ?? TypeReflection;
            string[] fieldsDB = GetFieldsNames(type.Name);

            Object[] objs = new Object[fields.Count];
            Object[] objsFields;

            for (int i = 0; i < fields.Count; i++)
            {
                objsFields = GetFields(fields[i], type);
                objs[i] = FormatterServices.GetUninitializedObject(type);
                for (int j = 0; j < fieldsDB.Length; j++)
                {
                    if (IsNotPrimitive(objsFields[j].GetType()))
                        fieldsDB[j] = fieldsDB[j].Replace("Id", "");
                    type.GetProperty(fieldsDB[j]).SetValue(objs[i], objsFields[j]);
                }
            }

            return objs;
        }

        private string CreateInsert(Classe obj, ref List<Object> parameters)
        {
            string query = "";
            int parametersCount = parameters.Count;
            int cont = 0;

            PropertyInfo[] properties = TypeReflection.GetProperties();
            PropertyInfo _property;

            query = "INSERT INTO " + TypeReflection.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (!IsId(property))
                {
                    if (IsNotPrimitive(property.PropertyType))
                        query += property.Name + "Id, ";
                    else
                        query += property.Name + ", ";
                }
                else if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                    query += property.Name + ", ";
            }

            query = query.Substring(0, (query.Length - 2)) + ")";
            query += " VALUES (";

            for (int i = parametersCount; i < (parametersCount + properties.Length); i++)
            {
                _property = properties[i - parametersCount];
                if (!IsId(_property))
                {
                    if (IsNotPrimitive(_property.PropertyType))
                        parameters.Add(GetIdFK(_property).GetValue(_property.GetValue(obj, null), null));
                    else
                        parameters.Add(_property.GetValue(obj, null) ?? "");
                    query += "@" + cont.ToString() + ", ";
                    cont++;
                }
                else if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                    query += "seq_" + TypeReflection.Name + "_" + _property.Name + ".nextval, ";
            }

            return query.Substring(0, (query.Length - 2)) + "); ";
        }

        private string CreateUpdate(Classe obj, ref List<Object> parameters, PropertyInfo[] properties = null, string where = null)
        {
            string IdField = GetIdField();
            Object IdValue = TypeReflection.GetProperty(IdField).GetValue(obj, null);
            string newWhere = where ?? " WHERE " + IdField + "=@" + parameters.Count;

            properties = properties ?? TypeReflection.GetProperties();
            parameters.Add(IdValue);

            var query = "UPDATE " + TypeReflection.Name + " SET ";

            foreach (PropertyInfo property in properties)
                if (!IsId(property))
                {
                    if (IsNotPrimitive(property.PropertyType))
                    {
                        query += property.Name + "Id=@" + parameters.Count + ", ";
                        parameters.Add(GetIdFK(property).GetValue(property.GetValue(obj, null), null));
                    }
                    else
                    {
                        query += property.Name + "=@" + parameters.Count + ", ";
                        parameters.Add(property.GetValue(obj, null));
                    }
                }

            return query.Substring(0, (query.Length - 2)) + newWhere + "; ";
        }

        private string CreateSelect(string fieldName, Type type = null, string where = null)
        {
            type = type ?? TypeReflection;
            string newWhere = where ?? " WHERE " + fieldName + "=@0";

            return "SELECT * FROM " + type.Name + newWhere + "; ";
        }

        private string CreateDelete(string fieldName, Type type = null, string where = null)
        {
            type = type ?? TypeReflection;
            string newWhere = where ?? " WHERE " + fieldName + "=@0";

            return "DELETE FROM " + type.Name + newWhere + "; ";
        }

        private string[] GetFieldsNames(string table)
        {
            var columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; break;
            }

            var sql = "SELECT COLUMN_NAME FROM " + columnsSchema + " WHERE TABLE_NAME = '" + table + "'";
            return executeQuery(sql).Select(x => x[0]).ToArray();
        }

        private string[] GetFieldsTypes(string table)
        {
            var columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; break;
            }

            var sql = "SELECT DATA_TYPE FROM " + columnsSchema + " WHERE TABLE_NAME = '" + table + "'";
            return executeQuery(sql).Select(x => x[0]).ToArray();
        }

        private void InsertOrUpdateSQL(Classe obj, Expression<Func<Classe, object>> IdentifierExpression = null)
        {
            PropertyInfo[] properties = TypeReflection.GetProperties();
            List<Object> parameters = new List<Object>();
            object propValue = "";
            string _where = "";
            string _update = "";
            string _params = "";
            string _values = "";
            string command = "if exists (select * from [dbo].[_tabela] where _where)\n" +
                             " begin \n" +
                             "     update [dbo].[_tabela] set _update \n" +
                             "     where _where \n" +
                             " end \n" +
                             " else \n" +
                             " begin \n" +
                             "   insert into [dbo].[_tabela] (_params)\n" +
                             "   values (_values)\n" +
                             " end";

            if (IdentifierExpression != null)
            {
                var expression = IdentifierExpression.Body as NewExpression;
                foreach (var member in expression.Members)
                {
                    propValue = TypeReflection.GetProperty(member.Name).GetValue(obj, null);
                    _where += member.Name + ((propValue == null) ? " is null" : " = @" + member.Name.ToLower()) + " and ";
                }
                _where = _where.Substring(0, _where.Length - 5);
            }
            else
                _where = "Id = @id";


            foreach (var prop in properties)
            {
                propValue = TypeReflection.GetProperty(prop.Name).GetValue(obj, null);
                if (propValue != null && !prop.Name.Equals("Id") && !prop.GetGetMethod().IsVirtual)
                {
                    parameters.Add(propValue);
                    _update += prop.Name + " = @" + prop.Name.ToLower() + ", ";
                    _params += "[" + prop.Name + "], ";
                    _values += "@" + prop.Name.ToLower() + ", ";
                }
            }

            executeQueryNoReturn(command.Replace("_where", _where)
                              .Replace("_tabela", TypeReflection.Name)
                              .Replace("_update", _update.Substring(0, _update.Length - 2))
                              .Replace("_params", _params.Substring(0, _params.Length - 2))
                              .Replace("_values", _values.Substring(0, _values.Length - 2)),
                              parameters.ToArray());
        }
    }

    public static class LambdaUtils
    {
        public static string ExpressionToString(this BinaryExpression body)
        {
            string result = "";
            var left = body.Left;
            var leftB = (left as BinaryExpression);
            if (leftB == null)
                result += (left as MemberExpression).Member.Name + GetOperatorNode(body.NodeType);
            else
                result += ExpressionToString(leftB) + GetOperatorNode(body.NodeType) + " ";

            var right = body.Right;
            var rightB = (right as BinaryExpression);
            if (rightB == null)
                return result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " ";
            else
                result += ExpressionToString(rightB).Replace("'", "").Replace("\"", "'").Replace(",", ".");
            return result;
        }

        private static string GetOperatorNode(ExpressionType nodo)
        {
            string expressionOperator = "";
            switch (nodo)
            {
                case ExpressionType.And:
                    expressionOperator = "and";
                    break;
                case ExpressionType.AndAlso:
                    expressionOperator = "and";
                    break;
                case ExpressionType.Or:
                    expressionOperator = "or";
                    break;
                case ExpressionType.OrElse:
                    expressionOperator = "or";
                    break;
                case ExpressionType.LessThan:
                    expressionOperator = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    expressionOperator = "<=";
                    break;
                case ExpressionType.GreaterThan:
                    expressionOperator = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    expressionOperator = ">=";
                    break;
                case ExpressionType.Equal:
                    expressionOperator = "=";
                    break;
                case ExpressionType.NotEqual:
                    expressionOperator = "<>";
                    break;
            }
            return expressionOperator;
        }
    }
}
