using System.Linq;
using System.Reflection;
using Xandernate.Annotations;

namespace Xandernate.Utils.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsPrimaryKey(this PropertyInfo property)
            => property.GetCustomAttributes(typeof(PrimaryKey), false).Any();
    }
}
