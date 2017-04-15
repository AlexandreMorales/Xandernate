using System;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;

using Xandernate.Utils.Extensions;

namespace Xandernate.Utils
{
    public static class Mapper
    {
        public static TClass MapToObjects<TClass>(IDataRecord FieldsObj)
        {
            return (TClass)MapToObjects(FieldsObj, typeof(TClass));
        }

        private static object MapToObjects(IDataRecord FieldsObj, Type type)
        {
            object value;
            PropertyInfo[] properties = type.GetProperties();
            object obj = FormatterServices.GetUninitializedObject(type);

            foreach (PropertyInfo property in properties)
            {
                if (property.IsForeignKey())
                    value = MapToObjects(FieldsObj, property.PropertyType);
                else
                    value = StringToProp(FieldsObj[type.Name + "_" + property.Name], property.PropertyType);

                property.SetValue(obj, value);
            }

            return obj;
        }

        public static object StringToProp(object value, Type type)
        {
            switch (type.Name)
            {
                case "Int32": return Convert.ToInt32(value);
                case "Double": return Convert.ToDouble(value);
                case "Decimal": return Convert.ToDecimal(value);
                case "DateTime": return Convert.ToDateTime(value);
                case "String": return Convert.ToString(value);
            }
            return value;
        }

        public static Type StringDBToType(string type)
        {
            switch (type)
            {
                case "int": return typeof(Int32);
                case "decimal": return typeof(Double);
                case "datetime2": return typeof(DateTime);
            }
            return typeof(String);
        }
    }
}
