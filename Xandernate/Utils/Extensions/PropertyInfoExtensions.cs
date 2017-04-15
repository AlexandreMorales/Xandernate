using System.Linq;
using System.Reflection;
using Xandernate.Annotations;

namespace Xandernate.Utils.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsForeignKey(this PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ForeignKey), false).Any() || property.PropertyType.IsNotPrimitive();
        }

        public static bool IsPrimaryKey(this PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(PrimaryKey), false).Any();
        }
    }
}
