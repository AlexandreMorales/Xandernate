using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Xandernate.DTO;
using Xandernate.Utils;
using Xandernate.Utils.Extensions;

namespace Xandernate.DAO
{
    public class DbDao<TClass> : IDbDao<TClass>
    {
        private ExecuterManager Executer;
        private Type TypeReflection;

        public DbDao(string _database = null, string _provider = null)
        {
            Executer = ExecuterManager.GetInstance(_database, _provider);
            TypeReflection = typeof(TClass);
            Init();
        }

        private void Init()
        {
            string createTable = QueryBuilder.GenerateCreate(TypeReflection);
            if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                createTable = string.Format("BEGIN \n{0}\n END;", createTable);

            Executer.ExecuteQueryNoReturn(createTable);

            List<INFORMATION_SCHEMA_COLUMNS> fields = GetFieldsDB();
            string migrationsSql = ColumnMigrations(fields) + TypeMigrations(fields);
            if (!String.IsNullOrEmpty(migrationsSql))
                Executer.ExecuteQueryNoReturn(migrationsSql);
        }


        public void Add(params TClass[] Objs)
        {
            QueryBuilder.GenericAction(Objs, GenerateScriptsEnum.GenerateInsert);
        }


        public void AddOrUpdate(params TClass[] Objs)
        {
            QueryBuilder.GenericAction(Objs, GenerateScriptsEnum.GenerateInsertOrUpdate);
        }

        public void AddOrUpdate(Expression<Func<TClass, object>> IdentifierExpression, params TClass[] Objs)
        {
            PropertyInfo[] properties = IdentifierExpression.GetProperties();
            QueryBuilder.GenericAction(Objs, GenerateScriptsEnum.GenerateInsertOrUpdate, properties);
        }


        public void Update(params TClass[] Objs)
        {
            QueryBuilder.GenericAction(Objs, GenerateScriptsEnum.GenerateUpdate);
        }

        public void Update(Expression<Func<TClass, object>> IdentifierExpression, params TClass[] Objs)
        {
            PropertyInfo[] properties = IdentifierExpression.GetProperties();
            QueryBuilder.GenericAction(Objs, GenerateScriptsEnum.GenerateUpdate, properties);
        }


        public TClass Find<Att>(Att Id)
        {
            PropertyInfo idProperty = TypeReflection.GetIdField();

            if (!typeof(Att).Equals(idProperty.PropertyType))
                throw new Exception("The parameter type is not the Id type of the class " + TypeReflection.Name);

            string query = QueryBuilder.GenerateSelect(idProperty.Name, TypeReflection);

            return Executer.ExecuteQuery<TClass>(query, new object[] { Id }).FirstOrDefault();
        }

        public TClass Find<Att>(Expression<Func<TClass, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryBuilder.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<TClass>(query, new object[] { Value }).FirstOrDefault();
        }

        public List<TClass> FindAll()
        {
            string query = QueryBuilder.GenerateSelect("", TypeReflection, where: "");

            return Executer.ExecuteQuery<TClass>(query);
        }

        public List<TClass> WhereEquals<Att>(Expression<Func<Att>> IdentifierExpression)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            Att Value = IdentifierExpression.Compile()();

            string query = QueryBuilder.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<TClass>(query, new object[] { Value });
        }

        public List<TClass> Where(Expression<Func<TClass, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryBuilder.GenerateSelect("", TypeReflection, where: validation);

            return Executer.ExecuteQuery<TClass>(query);
        }


        public void Remove(TClass Obj)
        {
            string IdField = TypeReflection.GetIdField().Name;

            string query = QueryBuilder.GenerateDelete(IdField, TypeReflection);

            Executer.ExecuteQueryNoReturn(query, TypeReflection.GetProperty(IdField).GetValue(Obj));
        }

        public void Remove<Att>(Att Id)
        {
            string query = QueryBuilder.GenerateDelete(TypeReflection.GetIdField().Name, TypeReflection);

            Executer.ExecuteQueryNoReturn(query, Id);
        }

        public void Remove(Expression<Func<TClass, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryBuilder.GenerateDelete("", TypeReflection, where: validation);

            Executer.ExecuteQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<TClass, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryBuilder.GenerateDelete(fieldName, TypeReflection);

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
}
