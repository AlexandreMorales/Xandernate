using System;
using System.Collections.Generic;
using System.Globalization;
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

        Type typeReflection;

        public DbDao(string _database)
            : base(_database)
        {
            typeReflection = typeof(Classe);
            createTable();
            Migrations();
        }

        public Classe Add(Classe obj)
        {
            string query = "";

            PropertyInfo[] properties = typeReflection.GetProperties();

            query = "INSERT INTO " + typeReflection.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (!isId(property))
                {
                    if (isNotPrimitive(property.PropertyType))
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

            for (int i = 0; i < properties.Length; i++)
            {
                if (!isId(properties[i]))
                {
                    query += "@" + i.ToString() + ", ";
                }
            }

            query = query.Substring(0, (query.Length - 2)) + ")";

            Object[] parameters = new Object[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                if (isNotPrimitive(properties[i].PropertyType))
                {
                    PropertyInfo fkId = GetIdFK(properties[i]);
                    Object fkObject = properties[i].GetValue(obj, null);
                    parameters[i] = fkId.GetValue(fkObject, null);
                }
                else
                {
                    parameters[i] = properties[i].GetValue(obj, null) ?? "";
                }
            }

            executeQuery(query, parameters);
            Classe[] objs = FindAll();
            return objs[objs.Length - 1];
        }

        public Classe Find(Object id)
        {
            string query = "";

            string IdField = GetIdField(typeReflection);

            query = "SELECT * FROM " + typeReflection.Name + " WHERE " + IdField + "=@0";

            List<string[]> fields = executeQuery(query, id);
            string[] fieldsDB = getFieldsNames(typeReflection.Name);

            if (fields.Count != 0)
            {
                Object[] objsFields = GetFields(fields[0], typeReflection);
                Classe obj = (Classe)FormatterServices.GetUninitializedObject(typeReflection);
                for (int i = 0; i < fieldsDB.Length; i++)
                {
                    if (isNotPrimitive(objsFields[i].GetType()))
                        typeReflection.GetProperty(fieldsDB[i].Replace("Id", "")).SetValue(obj, objsFields[i]);
                    else
                        typeReflection.GetProperty(fieldsDB[i]).SetValue(obj, objsFields[i]);
                }
                return obj;
            }
            return default(Classe);
        }

        public Classe[] FindAll()
        {
            string query = "SELECT * FROM " + typeReflection.Name;

            List<string[]> fields = executeQuery(query);
            string[] fieldsDB = getFieldsNames(typeReflection.Name);

            Classe[] objs = new Classe[fields.Count];
            Object[] objsFields;

            for (int i = 0; i < fields.Count; i++)
            {
                objsFields = GetFields(fields[i], typeReflection);
                objs[i] = (Classe)FormatterServices.GetUninitializedObject(typeReflection);
                for (int j = 0; j < fieldsDB.Length; j++)
                {
                    if (isNotPrimitive(objsFields[j].GetType()))
                        typeReflection.GetProperty(fieldsDB[j].Replace("Id", "")).SetValue(objs[i], objsFields[j]);
                    else
                        typeReflection.GetProperty(fieldsDB[j]).SetValue(objs[i], objsFields[j]);
                }
            }

            return objs;
        }

        public void Remove(Classe obj)
        {
            string query = "";

            string IdField = GetIdField(this.typeReflection);

            query = "DELETE FROM " + typeReflection.Name + " WHERE " + IdField + "=@0";

            executeQueryNoReturn(query, typeReflection.GetProperty(IdField).GetValue(obj, null));
        }

        public void Remove(Object id)
        {
            string query = "";

            string IdField = GetIdField(this.typeReflection);

            query = "DELETE FROM " + typeReflection.Name + " WHERE " + IdField + "=@0";

            executeQueryNoReturn(query, id);
        }

        public void Remove(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = breakExpression(body);
            string query = "DELETE FROM " + typeReflection.Name + " WHERE " + validation;

            executeQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<Classe, Att>> expression, Att value)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            string query = "DELETE FROM " + typeReflection.Name + " WHERE " + fieldName + "=@0";

            executeQueryNoReturn(query, value);
        }

        public void AddRange(params Classe[] objs)
        {
            foreach (Classe obj in objs)
            {
                Add(obj);
            }
        }

        public void AddOrUpdate(params Classe[] objs)
        {
            string IdField = GetIdField(typeReflection);

            object idValue;

            foreach (var obj in objs)
            {
                idValue = typeReflection.GetProperty(IdField).GetValue(obj, null);

                if (Find(idValue) == null)
                {
                    Add(obj);
                }
                else
                {
                    Update(obj);
                }
            }
        }

        public void AddOrUpdate<Att>(Expression<Func<Classe, Att>> expression, params Classe[] objs)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            object value;

            foreach (var obj in objs)
            {
                value = typeReflection.GetProperty(fieldName).GetValue(obj, null);

                if (Find(fieldName, value) == null)
                {
                    Add(obj);
                }
                else
                {
                    Update(obj);
                }
            }
        }

        public void Update(Classe obj)
        {
            string query = "";
            string IdField = GetIdField(this.typeReflection);
            Object IdValue = typeReflection.GetProperty(IdField).GetValue(obj, null);

            PropertyInfo[] properties = typeReflection.GetProperties();

            query = "UPDATE " + typeReflection.Name + " SET ";

            for (int i = 0; i < properties.Length; i++)
            {
                if (!isId(properties[i]))
                {
                    query += properties[i].Name + "=@" + i.ToString() + ", ";
                }
            }

            query = query.Substring(0, (query.Length - 2)) + " WHERE " + IdField + "=@0";

            Object[] parameters = new Object[properties.Length];
            parameters[0] = IdValue;

            for (int i = 0; i < properties.Length; i++)
            {
                if (!isId(properties[i]))
                {
                    if (isNotPrimitive(properties[i].PropertyType))
                    {
                        PropertyInfo fkId = GetIdFK(properties[i]);
                        Object fkObject = properties[i].GetValue(obj, null);
                        parameters[i] = fkId.GetValue(fkObject, null);
                    }
                    else
                    {
                        parameters[i] = properties[i].GetValue(obj, null);
                    }
                }
            }

            executeQueryNoReturn(query, parameters);
        }

        public Classe[] WhereEquals<Att>(Expression<Func<Att>> expression)
        {
            var member = expression.Body as MemberExpression;
            var fieldName = member.Member.Name;
            Att Value = expression.Compile()();

            string query = "";

            query = "SELECT * FROM " + typeReflection.Name + " WHERE " + fieldName + "=@0";

            List<string[]> fields = executeQuery(query, Value);
            string[] fieldsDB = getFieldsNames(typeReflection.Name);

            Classe[] objs = new Classe[fields.Count];
            Object[] objsFields;

            for (int i = 0; i < fields.Count; i++)
            {
                objsFields = GetFields(fields[i], typeReflection);
                objs[i] = (Classe)FormatterServices.GetUninitializedObject(typeReflection);
                for (int j = 0; j < fieldsDB.Length; j++)
                {
                    if (isNotPrimitive(objsFields[j].GetType()))
                        typeReflection.GetProperty(fieldsDB[j].Replace("Id", "")).SetValue(objs[i], objsFields[j]);
                    else
                        typeReflection.GetProperty(fieldsDB[j]).SetValue(objs[i], objsFields[j]);
                }
            }

            return objs;
        }

        public Classe[] Where(Expression<Func<Classe, bool>> expression)
        {
            var body = expression.Body as BinaryExpression;
            var validation = breakExpression(body);

            string query = "SELECT * FROM " + typeReflection.Name + " WHERE " + validation;

            List<string[]> fields = executeQuery(query);
            string[] fieldsDB = getFieldsNames(typeReflection.Name);

            Classe[] objs = new Classe[fields.Count];
            Object[] objsFields;

            for (int i = 0; i < fields.Count; i++)
            {
                objsFields = GetFields(fields[i], typeReflection);
                objs[i] = (Classe)FormatterServices.GetUninitializedObject(typeReflection);
                for (int j = 0; j < fieldsDB.Length; j++)
                {
                    if (isNotPrimitive(objsFields[j].GetType()))
                        typeReflection.GetProperty(fieldsDB[j].Replace("Id", "")).SetValue(objs[i], objsFields[j]);
                    else
                        typeReflection.GetProperty(fieldsDB[j]).SetValue(objs[i], objsFields[j]);
                }
            }

            return objs;
        }

        private void Migrations()
        {
            PropertyInfo[] properties = typeReflection.GetProperties();
            columnMigrations(properties);
            typeMigrations(properties);
        }

        private void createTable()
        {
            string query = "";
            string valType = "";

            PropertyInfo[] properties = typeReflection.GetProperties();

            query = "IF  NOT EXISTS (SELECT * FROM sys.objects " +
                    "WHERE object_id = OBJECT_ID(N'[dbo].[" + typeReflection.Name + "]') AND type in (N'U')) " +
                    "BEGIN " +
                    "CREATE TABLE " + typeReflection.Name + " (";

            foreach (PropertyInfo property in properties)
            {
                if (isId(property))
                {
                    valType = " int primary key not null IDENTITY(1,1), ";
                }
                else if (isNotPrimitive(property.PropertyType))
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

        private static bool isNotPrimitive(Type property)
        {
            return !property.IsPrimitive &&
                                property != typeof(Decimal) &&
                                property != typeof(String) &&
                                property != typeof(DateTime);
        }

        private bool isId(PropertyInfo property)
        {
            return property.Name.ToLower().Equals("id") ||
                                property.Name.ToLower().Equals("id" + typeReflection.Name.ToLower()) ||
                                property.Name.ToLower().Equals(typeReflection.Name.ToLower() + "id");
        }

        private string GetIdField(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (isId(property))
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
            {
                if (isId(item))
                {
                    return item;
                }
            }
            return null;
        }

        private Object FindFK(Object id, Type type)
        {
            string query = "";

            string IdField = GetIdField(type);

            query = "SELECT * FROM " + type.Name + " WHERE " + IdField + "=@0";

            List<string[]> fields = executeQuery(query, id);
            string[] fieldsDB = getFieldsNames(type.Name);

            if (fields.Count != 0)
            {
                Object[] objsFields = GetFields(fields[0], type);
                Object obj = (Object)FormatterServices.GetUninitializedObject(type);
                for (int i = 0; i < fieldsDB.Length; i++)
                {
                    if (isNotPrimitive(objsFields[i].GetType()))
                        type.GetProperty(fieldsDB[i].Replace("Id", "")).SetValue(obj, objsFields[i]);
                    else
                        type.GetProperty(fieldsDB[i]).SetValue(obj, objsFields[i]);
                }
                return obj;
            }
            return null;
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
                        if (isNotPrimitive(property.PropertyType))
                            objs[i] = FindFK(fields[i], property.PropertyType);
                        else
                        {
                            switch (property.PropertyType.ToString().Substring(7))
                            {
                                case "Int32":
                                    objs[i] = (fields[i].Equals("")) ? default(Int32) : Convert.ToInt32(fields[i]);
                                    break;
                                case "Double":
                                    objs[i] = (fields[i].Equals("")) ? default(Double) : Convert.ToDouble(fields[i]);
                                    break;
                                case "DateTime":
                                    objs[i] = (fields[i].Equals("")) ? default(DateTime) : Convert.ToDateTime(fields[i]);
                                    break;
                                default:
                                    objs[i] = fields[i];
                                    break;
                            }
                        }
                        break;
                    }
                }
            }

            return objs;
        }

        private string breakExpression(BinaryExpression body)
        {
            string result = "";
            var left = body.Left;
            var leftB = (left as BinaryExpression);
            if (leftB == null)
            {
                result += (left as MemberExpression).Member.Name + getOperatorNode(body.NodeType);
            }
            else
            {
                result += breakExpression(leftB) + getOperatorNode(body.NodeType) + " ";
            }
            var right = body.Right;
            var rightB = (right as BinaryExpression);
            if (rightB == null)
            {
                return result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " ";
            }
            else
            {
                result += breakExpression(rightB).Replace("'", "").Replace("\"", "'").Replace(",", ".");
            }
            return result;
        }

        private string getOperatorNode(ExpressionType nodo)
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

        private void columnMigrations(PropertyInfo[] properties)
        {
            string[] propertiesName = properties.Select(p => p.Name).ToArray();
            string[] fieldsDB = getFieldsNames(typeReflection.Name);
            string query = "";
            string valType = "";
            string fk = "";

            foreach (var property in properties)
            {
                fk = "";
                if (!fieldsDB.Contains(property.Name) && !fieldsDB.Contains(property.Name + "Id"))
                {
                    if (isNotPrimitive(property.PropertyType))
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
                    query = "ALTER TABLE " + typeReflection.Name + " ADD " + property.Name + fk + " " + valType;
                    executeQuery(query);

                    if (!fk.Equals(""))
                    {
                        query = "ALTER TABLE " + typeReflection.Name + " ADD FOREIGN KEY(" + property.Name + fk + ") REFERENCES " + property.PropertyType.Name + "(" + GetIdFK(property).Name + ")";
                        executeQuery(query);
                    }
                }
            }

            foreach (var column in fieldsDB)
            {
                if (!propertiesName.Contains(column) && !propertiesName.Contains(column.Replace("Id", "")))
                {
                    query = "ALTER TABLE " + typeReflection.Name + " DROP COLUMN " + column;
                    executeQuery(query);
                }
            }
        }

        private void typeMigrations(PropertyInfo[] properties)
        {
            string[] typesDB = getFieldsTypes(typeReflection.Name);
            string[] fieldsName = getFieldsNames(typeReflection.Name);
            string valType = "";
            string query = "";

            for (int i = 0; i < fieldsName.Length; i++)
            {
                foreach (var property in properties)
                {
                    if (property.Name.Equals(fieldsName[i]) || (property.Name + "Id").Equals(fieldsName[i]))
                    {
                        if (isNotPrimitive(property.PropertyType))
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
                            query = "ALTER TABLE " + typeReflection.Name + " ALTER COLUMN " + property.Name + " " + valType;
                            executeQuery(query);
                        }

                    }
                }
            }
        }

        private Classe Find(string fieldName, Object value)
        {
            string query = "SELECT * FROM " + typeReflection.Name + " WHERE " + fieldName + "=@0";

            List<string[]> fields = executeQuery(query, value);
            string[] fieldsDB = getFieldsNames(typeReflection.Name);

            if (fields.Count != 0)
            {
                Object[] objsFields = GetFields(fields[0], typeReflection);
                Classe obj = (Classe)FormatterServices.GetUninitializedObject(typeReflection);
                for (int i = 0; i < fieldsDB.Length; i++)
                {
                    if (isNotPrimitive(objsFields[i].GetType()))
                        typeReflection.GetProperty(fieldsDB[i].Replace("Id", "")).SetValue(obj, objsFields[i]);
                    else
                        typeReflection.GetProperty(fieldsDB[i]).SetValue(obj, objsFields[i]);
                }
                return obj;
            }
            return default(Classe);
        }
    }
}
