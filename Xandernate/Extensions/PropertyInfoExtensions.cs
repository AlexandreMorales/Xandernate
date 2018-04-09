using System.Linq;
using Xandernate.Annotations;

namespace System.Reflection
{
    public static class PropertyInfoExtensions
    {
        public static bool IsPrimaryKey(this PropertyInfo property)
            => property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Any();

        public static bool IsForeignObj(this PropertyInfo property)
            => property.GetCustomAttributes(typeof(ForeignObjectAttribute), false).Any();
    }
}
