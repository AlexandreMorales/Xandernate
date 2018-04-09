using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xandernate.ReflectionCache
{
    public class ReflectionEntityCache
    {
        private static readonly Dictionary<Type, ReflectionEntityCache> _dict = new Dictionary<Type, ReflectionEntityCache>();

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

        public ReflectionPropertyCache GetPropertyField(string field)
            => Properties.FirstOrDefault(p => p.Name == field || p.Name == field.SubstringLast());


        public static ReflectionEntityCache GetOrCreateEntity(Type type)
        {
            if (_dict.TryGetValue(type, out ReflectionEntityCache value))
                return value;

            value = new ReflectionEntityCache(type);
            _dict.Add(type, value);

            return value;
        }
    }
}
