using System;
using System.Reflection;

namespace Xandernate.ReflectionCache
{
    public class ReflectionPropertyCache
    {
        public readonly string Name;
        public readonly bool IsPrimaryKey;
        public readonly bool IsForeignObj;
        public readonly Type PropertyType;
        private readonly PropertyInfo Property;

        public ReflectionPropertyCache(PropertyInfo property, bool isPrimaryKey)
        {
            Property = property;
            PropertyType = Property.PropertyType;
            Name = Property.Name;
            IsPrimaryKey = isPrimaryKey;
            IsForeignObj = Property.IsForeignObj();
        }

        public void SetValue(object obj, object value)
            => Property.SetValue(obj, value);

        public object GetValue(object obj)
            => Property.GetValue(obj);

        public object GetObject(string value)
            => value.To(PropertyType);
    }
}
