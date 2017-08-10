using System;
using System.Linq;
using System.Reflection;

namespace Xandernate.Utils.Extensions
{
    public static class TypeExtensions
    {
        public static object TypeToObject(this Type type, string value)
        {
            switch (type.Name)
            {
                case "Int32": return int.Parse(value);
                case "Double": return double.Parse(value);
                case "String": return value;
                case "Datetime": return DateTime.Parse(value);
                default: return value;
            }
        }

        public static bool IsNotPrimitive(this Type type)
            => !type.IsArray &&
                type != typeof(Enum) &&
                type != typeof(string) &&
                type != typeof(byte) &&
                type != typeof(int) &&
                type != typeof(long) &&
                type != typeof(decimal) &&
                type != typeof(double) &&
                type != typeof(DateTime) &&
                type != typeof(TimeSpan);

        public static PropertyInfo GetIdField(this Type type)
            => type.GetProperties().FirstOrDefault(p => p.IsPrimaryKey());

        public static PropertyInfo GetPropertyField(this Type type, string field)
            => type.GetProperty(field) ?? type.GetProperty(field.SubstringLast());
    }
}
