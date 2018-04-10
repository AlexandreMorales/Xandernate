using System;
using System.Data;
using System.Reflection;
using Xandernate.ReflectionCache;

namespace Xandernate.Sql
{
    internal static class Mapper
    {
        public static TEntity MapToEntity<TEntity>(IDataRecord dataRecord, Type type)
            where TEntity : class, new()
        {
            if (type == null)
                type = typeof(TEntity);

            TEntity obj = new TEntity();

            ReflectionEntityCache typeCache = ReflectionEntityCache.GetOrCreateEntity(type);

            foreach (ReflectionPropertyCache property in typeCache.Properties)
            {
                object value = (property.IsForeignObj) ?
                    typeof(Mapper)
                            .GetMethod(nameof(Mapper.MapToEntity), BindingFlags.Static)
                            .MakeGenericMethod(property.Type)
                            .Invoke(null, new object[] { dataRecord, property.Type }) :
                    ConvertFromType(dataRecord[$"{type.Name}_{property.Name}"], property.Type);

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
