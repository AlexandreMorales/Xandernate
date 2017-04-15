using System;

namespace Xandernate.DTO
{
    public class INFORMATION_SCHEMA_COLUMNS
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string TypeString { get; set; }
        public Type Type { get; set; }
    }
}
