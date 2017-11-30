using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Xandernate.DAO;
using Xandernate.SQL.DTO;
using Xandernate.SQL.Utils;
using Xandernate.SQL.Utils.Extensions;
using Xandernate.Utils.Extensions;

namespace Xandernate.SQL.DAO
{
    public class DaoHandlerSQL<TDao> : IDaoHandler<TDao>
        where TDao : new()
    {
        private ExecuterManager Executer;
        private Type TypeReflection;

        public DaoHandlerSQL(string conn)
        {
            Executer = ExecuterManager.GetInstance(conn, DBTypes.Sql);
            TypeReflection = typeof(TDao);
            Init();
        }

        private void Init()
        {
            string createTable = QueryBuilder.GenerateCreate(TypeReflection);
            if (DataManager.DbType == DBTypes.Oracle)
                createTable = string.Format("BEGIN \n{0}\n END;", createTable);

            Executer.ExecuteQuery(createTable);

            List<INFORMATION_SCHEMA_COLUMNS> fields = GetFieldsDB();
            string migrationsSql = ColumnMigrations(fields) + TypeMigrations(fields);
            if (!String.IsNullOrEmpty(migrationsSql))
                Executer.ExecuteQuery(migrationsSql);
        }


        public void Add(params TDao[] objs)
        {
            QueryBuilder.GenericAction(objs, GenerateScriptsEnum.GenerateInsert);
        }


        public void AddOrUpdate(params TDao[] objs)
        {
            QueryBuilder.GenericAction(objs, GenerateScriptsEnum.GenerateInsertOrUpdate);
        }

        public void AddOrUpdate(Expression<Func<TDao, object>> identifierExpression, params TDao[] objs)
        {
            PropertyInfo[] properties = identifierExpression.GetProperties();
            QueryBuilder.GenericAction(objs, GenerateScriptsEnum.GenerateInsertOrUpdate, properties);
        }


        public void Update(params TDao[] objs)
        {
            QueryBuilder.GenericAction(objs, GenerateScriptsEnum.GenerateUpdate);
        }

        public void Update(Expression<Func<TDao, object>> identifierExpression, params TDao[] objs)
        {
            PropertyInfo[] properties = identifierExpression.GetProperties();
            QueryBuilder.GenericAction(objs, GenerateScriptsEnum.GenerateUpdate, properties);
        }


        public TDao Find<Att>(Att id)
        {
            PropertyInfo idProperty = TypeReflection.GetIdField();

            if (!typeof(Att).Equals(idProperty.PropertyType))
                throw new Exception("The parameter type is not the Id type of the class " + TypeReflection.Name);

            string query = QueryBuilder.GenerateSelect(idProperty.Name, TypeReflection);
            
            return Executer.ExecuteQuery<TDao>(query, new object[] { id }).FirstOrDefault();
        }

        public TDao Find<Att>(Expression<Func<TDao, Att>> identifierExpression, Att value)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryBuilder.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<TDao>(query, new object[] { value }).FirstOrDefault();
        }

        public List<TDao> FindAll()
        {
            string query = QueryBuilder.GenerateSelect("", TypeReflection, where: "");

            return Executer.ExecuteQuery<TDao>(query);
        }

        public List<TDao> WhereEquals<Att>(Expression<Func<Att>> identifierExpression)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            Att Value = identifierExpression.Compile()();

            string query = QueryBuilder.GenerateSelect(fieldName, TypeReflection);

            return Executer.ExecuteQuery<TDao>(query, new object[] { Value });
        }

        public List<TDao> Where(Expression<Func<TDao, bool>> identifierExpression)
        {
            BinaryExpression body = identifierExpression.Body as BinaryExpression;
            string validation = $" WHERE {body.ExpressionToString<SQLLambdaFunctions>().SubstringLast(1)}";
            string query = QueryBuilder.GenerateSelect("", TypeReflection, where: validation);

            return Executer.ExecuteQuery<TDao>(query);
        }


        public void Remove(TDao obj)
        {
            string IdField = TypeReflection.GetIdField().Name;

            string query = QueryBuilder.GenerateDelete(IdField, TypeReflection);

            Executer.ExecuteQuery(query, TypeReflection.GetProperty(IdField).GetValue(obj));
        }

        public void Remove<Att>(Att id)
        {
            string query = QueryBuilder.GenerateDelete(TypeReflection.GetIdField().Name, TypeReflection);

            Executer.ExecuteQuery(query, id);
        }

        public void Remove(Expression<Func<TDao, bool>> identifierExpression)
        {
            BinaryExpression body = identifierExpression.Body as BinaryExpression;
            string validation = $" WHERE {body.ExpressionToString<SQLLambdaFunctions>().SubstringLast(1)}";
            string query = QueryBuilder.GenerateDelete("", TypeReflection, where: validation);

            Executer.ExecuteQuery(query);
        }

        public void Remove<Att>(Expression<Func<TDao, Att>> identifierExpression, Att value)
        {
            MemberExpression member = identifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryBuilder.GenerateDelete(fieldName, TypeReflection);

            Executer.ExecuteQuery(query, value);
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
                if (!fieldsNameDB.Contains(property.Name) && !fieldsNameDB.Contains($"{property.Name}Id"))
                {
                    if (property.IsForeignKey())
                        fk = "Id";
                    else
                        valType = property.PropertyType.TypeToStringDB();

                    query += $"ALTER TABLE {TypeReflection.Name} ADD {property.Name}{fk} {valType};\n";

                    if (!fk.Equals(""))
                        query = $"ALTER TABLE {TypeReflection.Name} ADD FOREIGN KEY({property.Name}{fk}) REFERENCES {property.PropertyType.Name}({property.PropertyType.GetIdField().Name});\n";
                }
            }

            //DROP FIELDS
            foreach (string column in fieldsNameDB)
                if (TypeReflection.GetPropertyField(column) == null)
                    query += $"ALTER TABLE {TypeReflection.Name} DROP COLUMN {column};\n";

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
                    query += $"ALTER TABLE {TypeReflection.Name} ALTER COLUMN {property.Name} {valType};\n";
            }

            return query;
        }

        private List<INFORMATION_SCHEMA_COLUMNS> GetFieldsDB()
        {
            string columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.DbType)
            {
                case DBTypes.Oracle: columnsSchema = "all_tab_cols"; break;
            }

            string sql = $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH FROM {columnsSchema} WHERE TABLE_NAME = '{TypeReflection.Name}';\n";
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
