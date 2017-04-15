using System;
using System.Collections.Generic;
using System.Reflection;

using Xandernate.Utils.Extensions;

namespace Xandernate.DTO
{
    public class ReflectionMock
    {
        public List<ReflectionPropertieMock> Properties { get; set; }
        public string Name { get; set; }
        public ReflectionPropertieMock Id { get; set; }

        public ReflectionMock(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            Name = type.Name;
            Properties = new List<ReflectionPropertieMock>();

            foreach (PropertyInfo property in properties)
            {
                if (property.IsPrimaryKey())
                {
                    Id = new ReflectionPropertieMock(property);
                    Properties.Add(Id);
                }
                else
                    Properties.Add(new ReflectionPropertieMock(property));
            }
        }
    }
}
