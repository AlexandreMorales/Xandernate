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
    class DbDao<Classe> : GenericDAO, IDbDao<Classe>
    {
        /*         
         * IDENTITY(1,1) | FOREIGN KEY          
         * CONST STRINGS REPLACES
         */

        private Type TypeReflection;

        public DbDao(string _database)
            : base(_database)
        {
            TypeReflection = typeof(Classe);
            CreateTable();
            Migrations();
        }

        public Classe Add(Classe obj)
        {
            List<Object> parameters = new List<Object>();

            string query = CreateInsert(obj, ref parameters) + " SELECT CAST(SCOPE_IDENTITY() as int);";

            List<string[]> fields = executeQuery(query, parameters.ToArray());
            PropertyInfo idProperty = TypeReflection.GetProperty(GetIdField(TypeReflection));

            idProperty.SetValue(obj, ConvertToType(idProperty, fields[0][0]));
            return obj;
        }

        public Classe Find(Object id)
        {
            string query = "";

            string IdField = GetIdField(TypeReflection);

            query = "SELECT * FROM " + TypeReflection.Name + " WHERE " + IdField + "=@0;";

            return MapToObjects(executeQuery(query, id)).OfType<Classe>().FirstOrDefault();
        }

        public Classe Find<Att>(Expression<Func<Classe, Att>> expression, Att value)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            string query = "SELECT * FROM " + TypeReflection.Name + " WHERE " + fieldName + "=@0";

            return MapToObjects(executeQuery(query, value)).OfType<Classe>().FirstOrDefault();
        }

        public Classe[] FindAll()
        {
            string query = "SELECT * FROM " + TypeReflection.Name + ";";

            return MapToObjects(executeQuery(query)).OfType<Classe>().ToArray();
        }

        public void Remove(Classe obj)
        {
            string query = "";

            string IdField = GetIdField(this.TypeReflection);

            query = "DELETE FROM " + TypeReflection.Name + " WHERE " + IdField + "=@0;";

            executeQueryNoReturn(query, TypeReflection.GetProperty(IdField).GetValue(obj, null));
        }

        public void Remove(Object id)
        {
            string query = "";

            string IdField = GetIdField(this.TypeReflection);

            query = "DELETE FROM " + TypeReflection.Name + " WHERE " + IdField + "=@0;";

            executeQueryNoReturn(query, id);
        }

        public void Remove(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = BreakExpression(body);
            string query = "DELETE FROM " + TypeReflection.Name + " WHERE " + validation + ";";

            executeQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<Classe, Att>> expression, Att value)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            string query = "DELETE FROM " + TypeReflection.Name + " WHERE " + fieldName + "=@0;";

            executeQueryNoReturn(query, value);
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

        public void AddOrUpdate(params Classe[] objs)
        {
            string IdField = GetIdField(TypeReflection);
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

        public void InsertOrUpdateSQL(Classe obj, Expression<Func<Classe, object>> IdentifierExpression = null)
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

            int cont = 0;
            foreach (var prop in properties)
            {
                propValue = TypeReflection.GetProperty(prop.Name).GetValue(obj, null);
                if (propValue != null && !prop.Name.Equals("Id") && !prop.GetGetMethod().IsVirtual)
                {
                    parameters.Add(propValue);
                    _update += prop.Name + " = @" + prop.Name.ToLower() + ", ";
                    _params += "[" + prop.Name + "], ";
                    _values += "@" + cont.ToString() + ", ";
                    cont++;
                }
            }

            executeQueryNoReturn(command.Replace("_where", _where)
                              .Replace("_tabela", TypeReflection.Name)
                              .Replace("_update", _update.Substring(0, _update.Length - 2))
                              .Replace("_params", _params.Substring(0, _params.Length - 2))
                              .Replace("_values", _values.Substring(0, _values.Length - 2)),
                              parameters.ToArray());
        }

        public Classe Update(Classe obj)
        {
            string query = "";
            string IdField = GetIdField(this.TypeReflection);
            Object IdValue = TypeReflection.GetProperty(IdField).GetValue(obj, null);

            PropertyInfo[] properties = TypeReflection.GetProperties();
            List<Object> parameters = new List<Object>();
            parameters.Add(IdValue);

            query = "UPDATE " + TypeReflection.Name + " SET ";
            int cont = 1;

            foreach (PropertyInfo property in properties)
                if (!IsId(property))
                {
                    if (IsNotPrimitive(property.PropertyType))
                    {
                        query += property.Name + "Id=@" + cont.ToString() + ", ";
                        parameters.Add(GetIdFK(property).GetValue(property.GetValue(obj, null), null));
                    }
                    else
                    {
                        query += property.Name + "=@" + cont.ToString() + ", ";
                        parameters.Add(property.GetValue(obj, null));
                    }
                    cont++;
                }

            query = query.Substring(0, (query.Length - 2)) + " WHERE " + IdField + "=@0";

            executeQueryNoReturn(query, parameters.ToArray());
            return Find(IdValue);
        }

        public Classe Update<Att>(Classe obj, params Expression<Func<Classe, Att>>[] expressions)
        {
            MemberExpression member;
            string fieldName;
            PropertyInfo property;

            string query = "";
            string IdField = GetIdField(this.TypeReflection);
            Object IdValue = TypeReflection.GetProperty(IdField).GetValue(obj, null);
            List<Object> parameters = new List<Object>();
            parameters.Add(IdValue);

            query = "UPDATE " + TypeReflection.Name + " SET ";
            int cont = 1;

            for (int i = 0; i < expressions.Length; i++)
            {
                member = expressions[i].Body as MemberExpression;
                fieldName = member.Member.Name;
                property = TypeReflection.GetProperty(fieldName);
                if (!IsId(property))
                {
                    if (IsNotPrimitive(property.PropertyType))
                    {
                        query += property.Name + "Id=@" + cont.ToString() + ", ";
                        parameters.Add(GetIdFK(property).GetValue(property.GetValue(obj, null), null));
                    }
                    else
                    {
                        query += property.Name + "=@" + cont.ToString() + ", ";
                        parameters.Add(property.GetValue(obj, null));
                    }
                    cont++;
                }
            }

            query = query.Substring(0, (query.Length - 2)) + " WHERE " + IdField + "=@0";

            executeQueryNoReturn(query, parameters.ToArray());
            return Find(IdValue);
        }

        public Classe[] WhereEquals<Att>(Expression<Func<Att>> expression)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            Att Value = expression.Compile()();

            string query = "";

            query = "SELECT * FROM " + TypeReflection.Name + " WHERE " + fieldName + "=@0";

            return MapToObjects(executeQuery(query, Value)).OfType<Classe>().ToArray();
        }

        public Classe[] Where(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = BreakExpression(body);

            string query = "SELECT * FROM " + TypeReflection.Name + " WHERE " + validation;

            return MapToObjects(executeQuery(query)).OfType<Classe>().ToArray();
        }




        private void Migrations()
        {
            PropertyInfo[] properties = TypeReflection.GetProperties();
            ColumnMigrations(properties);
            TypeMigrations(properties);
        }

        private void CreateTable()
        {
            string query = "";
            string valType = "";

            PropertyInfo[] properties = TypeReflection.GetProperties();

            query = "IF  NOT EXISTS (SELECT * FROM sys.objects " +
                    "WHERE object_id = OBJECT_ID(N'[dbo].[" + TypeReflection.Name + "]') AND type in (N'U')) " +
                    "BEGIN " +
                    "CREATE TABLE " + TypeReflection.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (IsId(property))
                {
                    valType = " int primary key not null IDENTITY(1,1), ";
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
                    " END";

            executeQuery(query);
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

        private string GetIdField(Type type)
        {
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
            string query = "";

            string IdField = GetIdField(type);

            query = "SELECT * FROM " + type.Name + " WHERE " + IdField + "=@0";

            return MapToObjects(executeQuery(query, id), type).FirstOrDefault();
        }

        private Object[] GetFields(string[] fields, Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            Object[] objs = new Object[properties.Length];
            string[] fieldsName = getFieldsNames(type.Name);

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

        private string BreakExpression(BinaryExpression body)
        {
            string result = "";
            var left = body.Left;
            var leftB = (left as BinaryExpression);
            if (leftB == null)
            {
                result += (left as MemberExpression).Member.Name + GetOperatorNode(body.NodeType);
            }
            else
            {
                result += BreakExpression(leftB) + GetOperatorNode(body.NodeType) + " ";
            }
            var right = body.Right;
            var rightB = (right as BinaryExpression);
            if (rightB == null)
            {
                return result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " ";
            }
            else
            {
                result += BreakExpression(rightB).Replace("'", "").Replace("\"", "'").Replace(",", ".");
            }
            return result;
        }

        private string GetOperatorNode(ExpressionType nodo)
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

        private void ColumnMigrations(PropertyInfo[] properties)
        {
            string[] propertiesName = properties.Select(p => p.Name).ToArray();
            string[] fieldsDB = getFieldsNames(TypeReflection.Name);
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
                    query = "ALTER TABLE " + TypeReflection.Name + " ADD " + property.Name + fk + " " + valType;
                    executeQuery(query);

                    if (!fk.Equals(""))
                    {
                        query = "ALTER TABLE " + TypeReflection.Name + " ADD FOREIGN KEY(" + property.Name + fk + ") REFERENCES " + property.PropertyType.Name + "(" + GetIdFK(property).Name + ")";
                        executeQuery(query);
                    }
                }
            }

            foreach (var column in fieldsDB)
            {
                if (!propertiesName.Contains(column) && !propertiesName.Contains(column.Replace("Id", "")))
                {
                    query = "ALTER TABLE " + TypeReflection.Name + " DROP COLUMN " + column;
                    executeQuery(query);
                }
            }
        }

        private void TypeMigrations(PropertyInfo[] properties)
        {
            string[] typesDB = getFieldsTypes(TypeReflection.Name);
            string[] fieldsName = getFieldsNames(TypeReflection.Name);
            string valType = "";
            string query = "";

            for (int i = 0; i < fieldsName.Length; i++)
            {
                foreach (var property in properties)
                {
                    if (property.Name.Equals(fieldsName[i]) || (property.Name + "Id").Equals(fieldsName[i]))
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
                        if (!typesDB[i].Equals(valType))
                        {
                            query = "ALTER TABLE " + TypeReflection.Name + " ALTER COLUMN " + property.Name + " " + valType;
                            executeQuery(query);
                        }

                    }
                }
            }
        }

        private Object[] MapToObjects(List<string[]> fields, Type type = null)
        {
            type = type ?? TypeReflection;
            string[] fieldsDB = getFieldsNames(type.Name);

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
                    {
                        query += property.Name + "Id, ";
                    }
                    else
                    {
                        query += property.Name + ", ";
                    }
                }
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
            }

            return query.Substring(0, (query.Length - 2)) + ");";
        }

        public void InsertOrUpdateSQL(Classe obj, Expression<Func<Classe, object>> IdentifierExpression = null)
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
}
