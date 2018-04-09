using System;
using System.Data;
using System.Reflection;
using Xandernate.ReflectionCache;

namespace Xandernate.Sql
{
    internal static class Mapper
    {
        public static TEntity MapToObjects<TEntity>(IDataRecord FieldsObj, Type type = null)
            where TEntity : class, new()
        {
            if (type == null)
                type = typeof(TEntity);

            TEntity obj = new TEntity();

            return MapToObjects(obj, type, FieldsObj) as TEntity;
        }

        public static object MapToObjects(IDataRecord FieldsObj, Type type)
        {
            object obj = Activator.CreateInstance(type);

            return MapToObjects(obj, type, FieldsObj);
        }

        private static object MapToObjects(object obj, Type type, IDataRecord FieldsObj)
        {
            ReflectionEntityCache typeCache = ReflectionEntityCache.GetOrCreateEntity(type);

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                object value = (property.IsForeignObj) ?
                    MapToObjects(FieldsObj, property.Type) :
                    ConvertFromType(FieldsObj[$"{type.Name}_{property.Name}"], property.Type);

                property.SetValue(obj, value);
            }

            return obj;
        }

        public static object ConvertFromType(object value, Type type)
        {
            switch (type.Name)
            {
                case "Int32": return Convert.ToInt32(value);
                case "Double": return Convert.ToDouble(value);
                case "Decimal": return Convert.ToDecimal(value);
                case "DateTime": return Convert.ToDateTime(value);
                case "String": return Convert.ToString(value);
            }
            return value;
        }

        public static Type ColumnNameToType(string type)
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
}
