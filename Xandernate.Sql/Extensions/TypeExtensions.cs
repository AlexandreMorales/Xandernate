namespace System
{
    internal static class TypeExtensions
    {        
        public static string ToStringDb(this Type type)
        {
            if (type.IsNotPrimitive()) return "int";
            switch (type.Name)
            {
                case "Int32": return "int";
                case "Double": return "decimal(18,2)";
                case "String": return "varchar(255)";
                case "Datetime": return "datetime2";
                default: return type.Name;
            }
        }
    }
}
