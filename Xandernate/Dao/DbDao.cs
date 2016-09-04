using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Xandernate.Utils;

namespace Xandernate.Dao
{
    public class DbDao<Classe> : IDbDao<Classe>
    {
        private ExecuterManager Executer;
        private Type TypeReflection;

        public DbDao(string _database = null, string _provider = null)
        {
            Executer = ExecuterManager.GetInstance(_database, _provider);
            TypeReflection = typeof(Classe);
            Init();
        }

        private void Init()
        {
            string createTable = QueryUtils.GenerateCreate(TypeReflection);
            if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                createTable = string.Format("BEGIN \n{0}\n END;", createTable);

            Executer.ExecuteQueryNoReturn(createTable);

            List<INFORMATION_SCHEMA_COLUMNS> fields = GetFieldsDB();
            string migrationsSql = ColumnMigrations(fields) + TypeMigrations(fields);
            if (!String.IsNullOrEmpty(migrationsSql))
                Executer.ExecuteQueryNoReturn(migrationsSql);
        }


        public void Add(params Classe[] Objs)
        {
            QueryUtils.GenericAction(Objs, GenerateScriptsEnum.GenerateInsert);
        }


        public void AddOrUpdate(params Classe[] Objs)
        {
            QueryUtils.GenericAction(Objs, GenerateScriptsEnum.GenerateInsertOrUpdate);
        }

        public void AddOrUpdate(Expression<Func<Classe, object>> IdentifierExpression, params Classe[] Objs)
        {
            PropertyInfo[] properties = IdentifierExpression.GetProperties();
            QueryUtils.GenericAction(Objs, GenerateScriptsEnum.GenerateInsertOrUpdate, properties);
        }


        public void Update(params Classe[] Objs)
        {
            QueryUtils.GenericAction(Objs, GenerateScriptsEnum.GenerateUpdate);
        }

        public void Update(Expression<Func<Classe, object>> IdentifierExpression, params Classe[] Objs)
        {
            PropertyInfo[] properties = IdentifierExpression.GetProperties();
            QueryUtils.GenericAction(Objs, GenerateScriptsEnum.GenerateUpdate, properties);
        }


        public Classe Find<Att>(Att Id)
        {
            PropertyInfo idProperty = TypeReflection.GetIdField();

            if (!typeof(Att).Equals(idProperty.PropertyType))
                throw new Exception("The parameter type is not the Id type of the class " + TypeReflection.Name);

            string query = QueryUtils.GenerateSelect(idProperty.Name, TypeReflection);

            return Executer.ExecuteQuery<Classe>(query, new object[] { Id }).FirstOrDefault();
        }

        public Classe Find<Att>(Expression<Func<Classe, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryUtils.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<Classe>(query, new object[] { Value }).FirstOrDefault();
        }

        public List<Classe> FindAll()
        {
            string query = QueryUtils.GenerateSelect("", TypeReflection, where: "");

            return Executer.ExecuteQuery<Classe>(query);
        }

        public List<Classe> WhereEquals<Att>(Expression<Func<Att>> IdentifierExpression)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            Att Value = IdentifierExpression.Compile()();

            string query = QueryUtils.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<Classe>(query, new object[] { Value });
        }

        public List<Classe> Where(Expression<Func<Classe, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryUtils.GenerateSelect("", TypeReflection, where: validation);

            return Executer.ExecuteQuery<Classe>(query);
        }


        public void Remove(Classe Obj)
        {
            string IdField = TypeReflection.GetIdField().Name;

            string query = QueryUtils.GenerateDelete(IdField, TypeReflection);

            Executer.ExecuteQueryNoReturn(query, TypeReflection.GetProperty(IdField).GetValue(Obj));
        }

        public void Remove<Att>(Att Id)
        {
            string query = QueryUtils.GenerateDelete(TypeReflection.GetIdField().Name, TypeReflection);

            Executer.ExecuteQueryNoReturn(query, Id);
        }

        public void Remove(Expression<Func<Classe, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryUtils.GenerateDelete("", TypeReflection, where: validation);

            Executer.ExecuteQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<Classe, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryUtils.GenerateDelete(fieldName, TypeReflection);

            Executer.ExecuteQueryNoReturn(query, Value);
        }



        private string ColumnMigrations(List<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            PropertyInfo[] properties = TypeReflection.GetProperties();
            List<string> fieldsNameDB = fields.Select(x => x.Name).ToList();
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

                    query += "ALTER TABLE " + TypeReflection.Name + " ADD " + property.Name + fk + " " + valType + ";\n";

                    if (!fk.Equals(""))
                        query = "ALTER TABLE " + TypeReflection.Name + " ADD FOREIGN KEY(" + property.Name + fk + ") REFERENCES " + property.PropertyType.Name + "(" + property.PropertyType.GetIdField().Name + ");\n";
                }
            }

            //DROP FIELDS
            foreach (string column in fieldsNameDB)
                if (TypeReflection.GetPropertyField(column) == null)
                    query += "ALTER TABLE " + TypeReflection.Name + " DROP COLUMN " + column + ";\n";

            return query;
        }

        private string TypeMigrations(List<INFORMATION_SCHEMA_COLUMNS> fields)
        {
            string valType = null;
            string query = "";
            PropertyInfo property = null;
            foreach (INFORMATION_SCHEMA_COLUMNS field in fields)
            {
                property = TypeReflection.GetPropertyField(field.Name);
                valType = property.PropertyType.TypeToStringDB();

                if (!field.TypeString.Equals(Regex.Replace(valType, @"(\(.*\))", "")))
                    query += "ALTER TABLE " + TypeReflection.Name + " ALTER COLUMN " + property.Name + " " + valType + ";\n";
            }

            return query;
        }

        private List<INFORMATION_SCHEMA_COLUMNS> GetFieldsDB()
        {
            string columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; break;
            }

            string sql = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH FROM " + columnsSchema + " WHERE TABLE_NAME = '" + TypeReflection.Name + "';\n";
            return Executer.ExecuteQuery(sql, null, x => new INFORMATION_SCHEMA_COLUMNS
            {
                TableName = TypeReflection.Name,
                Name = x.GetString(0),
                TypeString = x.GetString(1),
                Type = Mapper.StringDBToType(x.GetString(1))
            });
        }

    }

    public class INFORMATION_SCHEMA_COLUMNS
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string TypeString { get; set; }
        public Type Type { get; set; }
    }

    public class ReflectionMock
    {
        public List<ReflectionPropertieMock> Properties { get; set; }
        public string Name { get; set; }
        public ReflectionPropertieMock Id { get; set; }

        public ReflectionMock(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            Name = type.Name;
            Properties = new List<ReflectionPropertieMock>();

            foreach (PropertyInfo property in properties)
            {
                if (property.IsPrimaryKey())
                {
                    Id = new ReflectionPropertieMock(property);
                    Properties.Add(Id);
                }
                else
                    Properties.Add(new ReflectionPropertieMock(property));
            }
        }
    }

    public class ReflectionPropertieMock
    {
        public string Name { get; set; }
        public bool IsId { get; set; }
        public bool IsForeignKey { get; set; }
        public ReflectionMock Type { get; set; }
        private PropertyInfo PropertyMock;

        public ReflectionPropertieMock(PropertyInfo property)
        {
            PropertyMock = property;
            Name = property.Name;
            IsId = property.IsPrimaryKey();
            IsForeignKey = property.IsForeignKey();
            Type = new ReflectionMock(property.PropertyType);
        }

        public object GetValue(object obj)
        {
            return PropertyMock.GetValue(obj);
        }
    }
}
