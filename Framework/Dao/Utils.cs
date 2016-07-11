using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xandernate.Annotations;

namespace Xandernate.Dao
{
    public class INFORMATION_SCHEMA_COLUMNS
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string TypeString { get; set; }
        public Type Type { get; set; }
    }

    public enum GenerateScriptsEnum { GenerateInsertOrUpdate, GenerateInsert, GenerateUpdate }

    public static class LambdaUtils
    {
        public static string ExpressionToString(this BinaryExpression body)
        {
            Expression left = body.Left;
            BinaryExpression leftB = (left as BinaryExpression);

            string result = (leftB == null) ?
                (left as MemberExpression).Member.Name + body.NodeType.GetOperatorNode() :
                leftB.ExpressionToString() + body.NodeType.GetOperatorNode() + " ";

            Expression right = body.Right;
            BinaryExpression rightB = (right as BinaryExpression);

            return (rightB == null) ?
                result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " " :
                result + rightB.ExpressionToString().Replace("'", "").Replace("\"", "'").Replace(",", ".");
        }

        private static string GetOperatorNode(this ExpressionType nodo)
        {
            switch (nodo)
            {
                case ExpressionType.And: return "and";
                case ExpressionType.AndAlso: return "and";
                case ExpressionType.Or: return "or";
                case ExpressionType.OrElse: return "or";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "<>";
                default: return "";
            }
        }

        public static PropertyInfo[] GetProperties<Classe>(this Expression<Func<Classe, object>> IdentifierExpression)
        {
            NewExpression newExpression = (IdentifierExpression.Body as NewExpression);
            if (newExpression == null)
            {
                UnaryExpression unaryExpression = (IdentifierExpression.Body as UnaryExpression);
                PropertyInfo property;
                if (unaryExpression == null)
                    property = (IdentifierExpression.Body as MemberExpression).Member as PropertyInfo;
                else
                    property = (unaryExpression.Operand as MemberExpression).Member as PropertyInfo;

                return new PropertyInfo[] { property };
            }
            return newExpression.Members.Select(x => typeof(Classe).GetProperty(x.Name)).ToArray();
        }
    }

    public static class MigrationsUtils
    {
        private static Dictionary<string, List<INFORMATION_SCHEMA_COLUMNS>> fieldsDB = new Dictionary<string, List<INFORMATION_SCHEMA_COLUMNS>>();

        public static string Migrations(Type type)
        {
            return ColumnMigrations(type) + TypeMigrations(type);
        }

        private static string ColumnMigrations(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            List<string> fieldsNameDB = GetFieldsName(type.Name).Select(x => x.Name).ToList();
            string query = "";
            string valType = null;
            string fk = null;

            //ADD FIELDS
            foreach (PropertyInfo property in properties)
            {
                fk = "";
                if (!fieldsNameDB.Contains(property.Name) && !fieldsNameDB.Contains(property.Name + "Id"))
                {
                    if (property.IsForeignKey())
                        fk = "Id";
                    else
                        valType = property.PropertyType.TypeToStringDB();

                    query += "ALTER TABLE " + type.Name + " ADD " + property.Name + fk + " " + valType + ";" + ExecuterManager.breakLine;

                    if (!fk.Equals(""))
                        query = "ALTER TABLE " + type.Name + " ADD FOREIGN KEY(" + property.Name + fk + ") REFERENCES " + property.PropertyType.Name + "(" + property.PropertyType.GetIdField().Name + ");" + ExecuterManager.breakLine;
                }
            }

            //DROP FIELDS
            foreach (string column in fieldsNameDB)
                if (type.GetPropertyField(column) == null)
                    query += "ALTER TABLE " + type.Name + " DROP COLUMN " + column + ";" + ExecuterManager.breakLine;

            if (!String.IsNullOrEmpty(query))
                fieldsDB.Remove(type.Name);

            return query;
        }

        private static string TypeMigrations(Type type)
        {
            List<INFORMATION_SCHEMA_COLUMNS> fields = GetFieldsDB(type.Name);
            string valType = null;
            string query = "";
            PropertyInfo property = null;
            foreach (INFORMATION_SCHEMA_COLUMNS field in fields)
            {
                property = type.GetPropertyField(field.Name);
                valType = property.PropertyType.TypeToStringDB();

                if (!field.TypeString.Equals(Regex.Replace(valType, @"(\(.*\))", "")))
                    query += "ALTER TABLE " + type.Name + " ALTER COLUMN " + property.Name + " " + valType + ";" + ExecuterManager.breakLine;
            }

            return query;
        }

        private static List<INFORMATION_SCHEMA_COLUMNS> GetFieldsDB(string tableName)
        {
            string columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; break;
            }

            string sql = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH FROM " + columnsSchema + " WHERE TABLE_NAME = '" + tableName + "';" + ExecuterManager.breakLine;
            return ExecuterManager.ExecuteQuery(sql, null, x => new INFORMATION_SCHEMA_COLUMNS
            {
                TableName = tableName,
                Name = x.GetString(0),
                TypeString = x.GetString(1),
                Type = MappingUtils.StringDBToType(x.GetString(1))
            });
        }

        public static List<INFORMATION_SCHEMA_COLUMNS> GetFieldsName(string tableName)
        {
            List<INFORMATION_SCHEMA_COLUMNS> result;
            if (fieldsDB.TryGetValue(tableName, out result))
                return result;

            result = GetFieldsDB(tableName);
            fieldsDB.Add(tableName, result);
            return result;
        }
    }

    public static class FieldsUtils
    {
        public static bool IsForeignKey(this PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ForeignKey), false).Count() > 0 || property.PropertyType.IsNotPrimitive();
        }

        public static bool IsPrimaryKey(this PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(PrimaryKey), false).Count() > 0;
        }

        public static bool IsNotPrimitive(this Type type)
        {
            return !type.IsPrimitive &&
                     type != typeof(Decimal) &&
                     type != typeof(String) &&
                     type != typeof(DateTime);
        }

        public static PropertyInfo GetIdField(this Type type)
        {
            return type.GetProperties().FirstOrDefault(p => p.IsPrimaryKey());
        }

        public static PropertyInfo GetPropertyField(this Type type, string field)
        {
            return type.GetProperty(field) ?? type.GetProperty(field.SubstringLast());
        }

        public static string SubstringLast(this string str, int cont = 2)
        {
            return str.Substring(0, (str.Length - cont));
        }
    }

    public static class MappingUtils
    {
        public static Classe MapToObjects<Classe>(IDataReader FieldsObj)
        {
            object value;
            Type type = typeof(Classe);
            PropertyInfo[] properties = type.GetProperties();
            Classe obj = (Classe)FormatterServices.GetUninitializedObject(type);

            foreach (PropertyInfo property in properties)
            {
                if (property.IsForeignKey())
                    value = typeof(MappingUtils).GetMethod("MapToObjects").MakeGenericMethod(property.PropertyType).Invoke(null, new object[] { FieldsObj });
                else
                    value = StringToProp(FieldsObj[type.Name + "_" + property.Name], property.PropertyType);

                property.SetValue(obj, value);
            }

            return obj;
        }

        public static object StringToProp(object value, Type type)
        {
            switch (type.Name)
            {
                case "Int32": return Convert.ToInt32(value);
                case "Double": return Convert.ToDouble(value);
                case "Decimal": return Convert.ToDecimal(value);
                case "DateTime": return Convert.ToDateTime(value);
                case "String": return Convert.ToString(value);
                default: return value;
            }
        }

        public static string TypeToStringDB(this Type type)
        {
            if (type.IsNotPrimitive()) return "int";
            switch (type.Name)
            {
                case "Int32": return "int";
                case "Double": return "decimal(18,2)";
                case "String": return "varchar(255)";
                case "Datetime": return "datetime2";
                default: return type.Name;
            }
        }

        public static Type StringDBToType(string type)
        {
            switch (type)
            {
                case "int": return typeof(Int32);
                case "decimal": return typeof(Double);
                case "datetime2": return typeof(DateTime);
            }
            return typeof(String);
        }
    }

    public static class QueryUtils
    {
        public static void GenericAction<Classe>(Classe[] Objs, GenerateScriptsEnum method)
        {
            GenericAction(Objs, method, null);
        }
        public static void GenericAction<Classe>(Classe[] Objs, GenerateScriptsEnum method, PropertyInfo[] properties)
        {
            Type type = Objs[0].GetType();
            List<object> parameters = new List<object>();
            PropertyInfo idProperty = type.GetIdField();
            string query = "";
            List<object> fields = null;
            List<Classe> modified = new List<Classe>();

            foreach (Classe obj in Objs)
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

                if (idProperty.GetValue(obj).Equals(0) || idProperty.GetValue(obj) == null)
                {
                    modified.Add(obj);
                    switch (DataManager.Provider.ToLower())
                    {
                        case "odp.net":
                            query += "SELECT seq_" + type.Name + "_" + type.GetIdField().Name + ".currval FROM dual;" + ExecuterManager.breakLine;
                            break;
                        default: query += "SELECT IDENT_CURRENT('" + type.Name + "');" + ExecuterManager.breakLine; break;
                    }
                }
            }
            if (modified.Count > 0)
            {
                fields = ExecuterManager.ExecuteQuery<object>(query, parameters.ToArray(), x => MappingUtils.StringToProp(x.GetDecimal(0), idProperty.PropertyType));

                for (int i = 0; i < modified.Count; i++)
                    idProperty.SetValue(modified[i], fields[i]);
            }
            else ExecuterManager.ExecuteQueryNoReturn(query, parameters.ToArray());
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

                    switch (DataManager.Provider.ToLower())
                    {
                        case "sqlclient":
                            valType += " IDENTITY(1,1)";
                            break;
                        case "odp.net":
                            beforeCreate +=
                        "    BEGIN" + ExecuterManager.breakLine +
                        "        EXECUTE IMMEDIATE 'CREATE SEQUENCE seq_" + type.Name + "_" + property.Name + " MINVALUE 1 START WITH 1 INCREMENT BY 1';" + ExecuterManager.breakLine +
                        "    EXCEPTION" + ExecuterManager.breakLine +
                        "        WHEN OTHERS THEN" + ExecuterManager.breakLine +
                        "        IF SQLCODE != -955 THEN" + ExecuterManager.breakLine +
                        "            RAISE;" + ExecuterManager.breakLine +
                        "        END IF;" + ExecuterManager.breakLine +
                        "    END;" + ExecuterManager.breakLine;
                            break;
                    }

                    valType += ", ";
                }
                else if (property.IsForeignKey())
                {
                    beforeCreate += GenerateCreate(property.PropertyType);
                    valType = "Id int " + (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase) ? "" : "FOREIGN KEY")
                        + " REFERENCES " + property.PropertyType.Name + "(" + property.PropertyType.GetIdField().Name + ") ON DELETE CASCADE, ";
                }
                else
                    valType = " " + property.PropertyType.TypeToStringDB() + ", ";

                query += property.Name + valType;
            }

            query = query.SubstringLast() + ")";


            switch (DataManager.Provider.ToLower())
            {
                case "odp.net":
                    return beforeCreate +
                        "    BEGIN" + ExecuterManager.breakLine +
                        "        EXECUTE IMMEDIATE '" + query + "';" + ExecuterManager.breakLine +
                        "    EXCEPTION" + ExecuterManager.breakLine +
                        "        WHEN OTHERS THEN" + ExecuterManager.breakLine +
                        "        IF SQLCODE != -955 THEN" + ExecuterManager.breakLine +
                        "            RAISE;" + ExecuterManager.breakLine +
                        "        END IF;" + ExecuterManager.breakLine +
                        "    END;" + ExecuterManager.breakLine;
                case "sqlclient":
                default:
                    return beforeCreate +
                        "IF  NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + type.Name + "')" + ExecuterManager.breakLine +
                        "   BEGIN" + ExecuterManager.breakLine +
                        "       " + query + ";" + ExecuterManager.breakLine +
                        "   END;" + ExecuterManager.breakLine;
            }
        }

        public static string GenerateInsert(object obj, List<object> parameters)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();
            object idFKValue;
            List<object> parametersFK;
            object propertyVal;
            string insertFK = "";
            string values = "";
            string query = "INSERT INTO " + type.Name + " (";

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
                            idFKValue = ExecuterManager.ExecuteQuery(insertFK, parametersFK.ToArray(), x => MappingUtils.StringToProp(x.GetDecimal(0), property.PropertyType.GetIdField().PropertyType))[0];
                        else
                            ExecuterManager.ExecuteQueryNoReturn(insertFK, parametersFK.ToArray());

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
                else if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                {
                    query += property.Name + ", ";
                    values += "seq_" + type.Name + "_" + property.Name + ".nextval, ";
                }
            }

            return query.SubstringLast() + ") VALUES (" + values.SubstringLast() + ");" + ExecuterManager.breakLine;
        }

        private static string GenerateInsertFK(object obj, List<object> parameters)
        {
            Type type = obj.GetType();
            object propertyVal;
            object idValue = type.GetIdField().GetValue(obj);
            if (idValue == null || idValue.Equals(0))
                switch (DataManager.Provider.ToLower())
                {
                    case "odp.net": return GenerateInsert(obj, parameters) + "SELECT seq_" + type + "_" + type.GetIdField() + ".currval FROM dual;" + ExecuterManager.breakLine;
                    default: return GenerateInsert(obj, parameters) + "SELECT IDENT_CURRENT('" + type.Name + "');" + ExecuterManager.breakLine;
                }
            PropertyInfo[] properties = type.GetProperties();
            string values = "";
            string beforeInsert = "";
            int idIndex = parameters.Count;
            string query = "IF NOT EXISTS (" + GenerateSelectSimple(type.GetIdField().Name, type, cont: idIndex).SubstringLast(3) + ")" + ExecuterManager.breakLine +
                           "  BEGIN " + ExecuterManager.breakLine +
                            (DataManager.Provider.Equals("SQLCLIENT", StringComparison.InvariantCultureIgnoreCase) ?
                           "    SET IDENTITY_INSERT " + type.Name + " ON " + ExecuterManager.breakLine : "") +
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

            return beforeInsert + query.SubstringLast() + ") VALUES (" + values.SubstringLast() + ");" + ExecuterManager.breakLine +
                         (DataManager.Provider.Equals("SQLCLIENT", StringComparison.InvariantCultureIgnoreCase) ?
                         "    SET IDENTITY_INSERT " + type.Name + " OFF " + ExecuterManager.breakLine : "") +
                         "  END" + ExecuterManager.breakLine;
        }

        public static string GenerateInsertOrUpdate(object obj, List<object> parameters, PropertyInfo[] properties)
        {
            Type type = obj.GetType();
            string where = " WHERE ";
            object propValue;

            if (properties != null && properties.Count() > 0)
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

            return "IF EXISTS (" + GenerateSelectSimple("", type, where).SubstringLast(3) + ")" + ExecuterManager.breakLine +
                    "  BEGIN " + ExecuterManager.breakLine +
                    "    " + GenerateUpdate(obj, parameters, where) +
                    "  END " + ExecuterManager.breakLine +
                    "ELSE" + ExecuterManager.breakLine +
                    "  BEGIN " + ExecuterManager.breakLine +
                    "    " + GenerateInsert(obj, parameters) +
                    "  END;" + ExecuterManager.breakLine;
        }

        public static string GenerateUpdate(object obj, List<object> parameters, string where = null, params PropertyInfo[] properties)
        {
            Type type = obj.GetType();
            object propertyVal;
            string IdField = type.GetIdField().Name;
            object IdValue = type.GetProperty(IdField).GetValue(obj);
            string query = "UPDATE " + type.Name + " SET ";

            where = where ?? " WHERE " + IdField + "=@" + parameters.Count;
            if (properties == null || properties.Count() == 0)
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
            return query.SubstringLast() + where + ";" + ExecuterManager.breakLine;
        }

        public static string GenerateSelect(string fieldName, Type type, string where = null, int cont = 0)
        {
            where = where ?? ("WHERE " + type.Name.ToLower() + "." + fieldName + "=@" + cont);
            string query = "SELECT ";
            string joins = "";
            joins = GenerateSelectFields(type, ref query);
            return query.SubstringLast() + " " + ExecuterManager.breakLine +
                    "FROM " + type.Name + " " + ExecuterManager.breakLine + joins + where + ";" + ExecuterManager.breakLine;
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
                    joins += "inner join " + fk.Name + " " + fk.Name.ToLower() + " on  " + type.Name.ToLower() + "." + property.Name + "Id = " + fk.Name.ToLower() + "." + fk.GetIdField().Name + " " + ExecuterManager.breakLine
                          + GenerateSelectFields(fk, ref query);
                }
                else
                    query += type.Name.ToLower() + "." + property.Name + " as " + type.Name + "_" + property.Name + ", ";

            return joins;
        }

        private static string GenerateSelectSimple(string fieldName, Type type, string where = null, int cont = 0)
        {
            return GenerateGenericSimple("SELECT *", fieldName, type, where, cont);
        }
        public static string GenerateDelete(string fieldName, Type type, string where = null, int cont = 0)
        {
            return GenerateGenericSimple("DELETE", fieldName, type, where, cont);
        }
        private static string GenerateGenericSimple(string action, string fieldName, Type type, string where, int cont)
        {
            return action + " FROM " + type.Name + (where ?? (" WHERE " + fieldName + "=@" + cont)) + ";" + ExecuterManager.breakLine;
        }
    }

    public static class DBFirst
    {
        public static void Init(string _database = null, string _provider = null)
        {
            ExecuterManager.Config(_database, _provider);
            List<Type> newObjs = new List<Type>();
            string tableSchema = "INFORMATION_SCHEMA.TABLES";
            string columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; tableSchema = "DBA_TABLES"; break;
            }

            string sql = "SELECT t.TABLE_NAME, p.COLUMN_NAME, p.DATA_TYPE, p.IS_NULLABLE, p.NUMERIC_PRECISION, p.NUMERIC_SCALE, p.CHARACTER_MAXIMUM_LENGTH  " + ExecuterManager.breakLine +
                         "FROM " + columnsSchema + " p " + ExecuterManager.breakLine +
                         "INNER JOIN " + tableSchema + " t " + ExecuterManager.breakLine +
                         "ON p.TABLE_NAME = t.TABLE_NAME " + ExecuterManager.breakLine +
                         "WHERE t.TABLE_TYPE = 'BASE TABLE'; ";

            List<INFORMATION_SCHEMA_COLUMNS> tables = ExecuterManager.ExecuteQuery<INFORMATION_SCHEMA_COLUMNS>(sql, null, x => new INFORMATION_SCHEMA_COLUMNS
            {
                TableName = x.GetString(0),
                Name = x.GetString(1),
                TypeString = x.GetString(2),
                Type = MappingUtils.StringDBToType(x.GetString(2))
            });

            tables.GroupBy(x => x.TableName).Select(x => x.ToList()).ToList().ForEach(x => TypeBuilderUtils.CreatClass(x));
        }
    }

    public static class TypeBuilderUtils
    {
        public static Type CreateResultType(List<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            TypeBuilder tb = GetTypeBuilder(fields[0].TableName);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var field in fields)
                CreateProperty(tb, field.Name, field.Type);

            return tb.CreateType();
        }

        private static TypeBuilder GetTypeBuilder(string tableName)
        {
            var an = new AssemblyName(tableName);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            return moduleBuilder.DefineType(tableName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout);
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, System.Reflection.PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }



        public static void CreatClass(List<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            CodeCompileUnit targetUnit = new CodeCompileUnit();
            CodeNamespace samples = new CodeNamespace("Xandernate.Models");
            samples.Imports.Add(new CodeNamespaceImport("System"));
            CodeTypeDeclaration targetClass = new CodeTypeDeclaration(fields[0].TableName);
            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public;
            samples.Types.Add(targetClass);
            targetUnit.Namespaces.Add(samples);

            fields.ForEach(f => targetClass.Members.Add(new CodeMemberField() { 
                Attributes = MemberAttributes.Public,
                Name = f.Name,
                Type = new CodeTypeReference(f.Type),
            }));

            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

            string directoryPath = AppDomain.CurrentDomain.BaseDirectory + @"\GenerateModels";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            string sourceFile = directoryPath + @"\" + fields[0].TableName + ".cs";

            using (StreamWriter sourceWriter = new StreamWriter(sourceFile))
            {
                provider.GenerateCodeFromCompileUnit(targetUnit, sourceWriter, options);
            }

            CompilerResults cr = provider.CompileAssemblyFromFile(new CompilerParameters(), sourceFile);
            Assembly assembly = cr.CompiledAssembly;
        }
    }
}
