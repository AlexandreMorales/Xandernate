using System;
using System.Reflection;

namespace Xandernate.ReflectionCache
{
    public class ReflectionPropertyCache
    {
        public readonly string Name;
        public readonly bool IsPrimaryKey;
        public readonly bool IsForeignObj;
        public readonly Type Type;

        private readonly PropertyInfo _property;

        internal ReflectionPropertyCache(PropertyInfo property, bool isPrimaryKey)
        {
            _property = property;
            Type = _property.PropertyType;
            Name = _property.Name;
            IsPrimaryKey = isPrimaryKey;
            IsForeignObj = _property.IsForeignObj();
        }

        public void SetValue(object obj, object value)
            => _property.SetValue(obj, value);

        public object GetValue(object obj)
            => _property.GetValue(obj);
    }
}
