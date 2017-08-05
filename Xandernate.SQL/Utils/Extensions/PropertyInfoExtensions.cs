using System.Linq;
using System.Reflection;

using Xandernate.Annotations;
using Xandernate.Utils.Extensions;

namespace Xandernate.SQL.Utils.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsForeignKey(this PropertyInfo property)
            => property.GetCustomAttributes(typeof(ForeignKey), false).Any() || property.PropertyType.IsNotPrimitive();
    }
}
