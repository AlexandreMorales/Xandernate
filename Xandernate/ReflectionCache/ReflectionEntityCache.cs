using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xandernate.ReflectionCache
{
    public class ReflectionEntityCache
    {
        private static readonly Dictionary<Type, ReflectionEntityCache> _entityCache = new Dictionary<Type, ReflectionEntityCache>();

        public readonly List<ReflectionPropertyCache> Properties;
        public readonly string Name;
        public readonly ReflectionPropertyCache PrimaryKey;

        private ReflectionEntityCache(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            Name = type.Name;
            Properties = new List<ReflectionPropertyCache>();

            foreach (PropertyInfo property in properties)
            {
                if (property.IsPrimaryKey())
                {
                    PrimaryKey = new ReflectionPropertyCache(property, true);
                    Properties.Add(PrimaryKey);
                }
                else
                    Properties.Add(new ReflectionPropertyCache(property, false));
            }
        }

        public static ReflectionEntityCache GetOrCreateEntity(Type type)
        {
            if (_entityCache.TryGetValue(type, out ReflectionEntityCache value))
                return value;

            value = new ReflectionEntityCache(type);
            _entityCache.Add(type, value);

            return value;
        }

        public static ReflectionEntityCache GetOrCreateEntity<TEntity>()
            => GetOrCreateEntity(typeof(TEntity));

        public ReflectionPropertyCache GetProperty(string name)
            => Properties.FirstOrDefault(p => name == p.Name || name == $"{p.Name}Id");
    }
}
