using System.ComponentModel;
using System.Globalization;

namespace System
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Converts given object to a value type using <see cref="Convert.ChangeType(object,TypeCode)"/> method.
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <param name="type">Type of the object</param>
        /// <returns>Converted object</returns>
        public static object To(this object obj, Type type)
        {
            if (type == typeof(Guid))
                return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(obj.ToString());

            return Convert.ChangeType(obj, type, CultureInfo.InvariantCulture);
        }

        public static bool IsNotPrimitive(this Type type)
            => !type.IsArray &&
                !type.IsEnum &&
                !type.IsPrimitive &&
                type != typeof(string) &&
                type != typeof(byte) &&
                type != typeof(int) &&
                type != typeof(long) &&
                type != typeof(decimal) &&
                type != typeof(double) &&
                type != typeof(DateTime) &&
                type != typeof(TimeSpan);

        public static object GetDefaultValue(this Type type)
        {
            if (type == typeof(Guid))
                return Guid.Empty;
            if (type.IsValueType && (type == typeof(int) || type == typeof(long) || type == typeof(decimal)))
                return 0;

            return null;
        }
    }
}
