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
    public class DbDao<Classe> : IDbDao<Classe>
    {
        /*         
         * NUMERIC    
         */

        private Type TypeReflection;

        public DbDao(string _database = null, string _provider = null)
        {
            ExecuterManager.Config(_database, _provider);
            TypeReflection = typeof(Classe);
            Init();
        }

        private void Init()
        {
            string createTable = QueryUtils.GenerateCreate(TypeReflection);
            if (DataManager.Provider.Equals("ODP.NET", StringComparison.InvariantCultureIgnoreCase))
                createTable = "BEGIN" + ExecuterManager.breakLine + createTable + "END;" + ExecuterManager.breakLine;

            ExecuterManager.ExecuteQueryNoReturn(createTable);

            string migrationsQuery = MigrationsUtils.Migrations(TypeReflection);
            if (!String.IsNullOrEmpty(migrationsQuery))
                ExecuterManager.ExecuteQueryNoReturn(migrationsQuery);
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

            if (typeof(Att).Equals(idProperty.PropertyType))
                throw new Exception("The parameter type is not the Id type of the class " + TypeReflection.Name);

            string query = QueryUtils.GenerateSelect(idProperty.Name, TypeReflection);

            return ExecuterManager.ExecuteQuery<Classe>(query, new object[] { Id }).FirstOrDefault();
        }

        public Classe Find<Att>(Expression<Func<Classe, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryUtils.GenerateSelect(fieldName, TypeReflection);

            return ExecuterManager.ExecuteQuery<Classe>(query, new object[] { Value }).FirstOrDefault();
        }

        public List<Classe> FindAll()
        {
            string query = QueryUtils.GenerateSelect("", TypeReflection, where: "");

            return ExecuterManager.ExecuteQuery<Classe>(query);
        }

        public List<Classe> WhereEquals<Att>(Expression<Func<Att>> IdentifierExpression)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            Att Value = IdentifierExpression.Compile()();

            string query = QueryUtils.GenerateSelect(fieldName, TypeReflection);

            return ExecuterManager.ExecuteQuery<Classe>(query, new object[] { Value });
        }

        public List<Classe> Where(Expression<Func<Classe, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryUtils.GenerateSelect("", TypeReflection, where: validation);

            return ExecuterManager.ExecuteQuery<Classe>(query);
        }


        public void Remove(Classe Obj)
        {
            string IdField = TypeReflection.GetIdField().Name;

            string query = QueryUtils.GenerateDelete(IdField, TypeReflection);

            ExecuterManager.ExecuteQueryNoReturn(query, TypeReflection.GetProperty(IdField).GetValue(Obj));
        }

        public void Remove<Att>(Att Id)
        {
            string query = QueryUtils.GenerateDelete(TypeReflection.GetIdField().Name, TypeReflection);

            ExecuterManager.ExecuteQueryNoReturn(query, Id);
        }

        public void Remove(Expression<Func<Classe, bool>> IdentifierExpression)
        {
            BinaryExpression body = IdentifierExpression.Body as BinaryExpression;
            string validation = " WHERE " + body.ExpressionToString().SubstringLast(1);
            string query = QueryUtils.GenerateDelete("", TypeReflection, where: validation);

            ExecuterManager.ExecuteQueryNoReturn(query);
        }

        public void Remove<Att>(Expression<Func<Classe, Att>> IdentifierExpression, Att Value)
        {
            MemberExpression member = IdentifierExpression.Body as MemberExpression;
            string fieldName = member.Member.Name;
            string query = QueryUtils.GenerateDelete(fieldName, TypeReflection);

            ExecuterManager.ExecuteQueryNoReturn(query, Value);
        }

    }
}
