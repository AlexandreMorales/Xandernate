using System;
using System.Linq;
using System.Reflection;

namespace Xandernate.Utils.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsNotPrimitive(this Type type)
        {
            return !type.IsPrimitive &&
                !type.IsEnum &&
                type != typeof(string) &&
                type != typeof(byte) &&
                type != typeof(int) &&
                type != typeof(long) &&
                type != typeof(decimal) &&
                type != typeof(double) &&
                type != typeof(DateTime) &&
                type != typeof(TimeSpan);
        }

        public static PropertyInfo GetIdField(this Type type)
        {
            return type.GetProperties().FirstOrDefault(p => p.IsPrimaryKey());
        }

        public static PropertyInfo GetPropertyField(this Type type, string field)
        {
            return type.GetProperty(field) ?? type.GetProperty(field.SubstringLast());
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
    }
}
