using System.Reflection;

namespace Xandernate.DTO
{
    public class ReflectionPropertieMock
    {
        public string Name { get; set; }
        public bool IsId { get; set; }
        public bool IsForeignKey { get; set; }
        public ReflectionMock Type { get; set; }
        private PropertyInfo PropertyMock;

        public ReflectionPropertieMock(PropertyInfo property)
        {
            PropertyMock = property;
            Name = property.Name;
            IsId = property.IsPrimaryKey();
            IsForeignKey = property.IsForeignKey();
            Type = new ReflectionMock(property.PropertyType);
        }

        public object GetValue(object obj)
        {
            return PropertyMock.GetValue(obj);
        }
    }
}
