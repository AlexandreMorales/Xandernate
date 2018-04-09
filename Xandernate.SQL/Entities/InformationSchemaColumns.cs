using System;

namespace Xandernate.Sql.Entities
{
    internal class InformationSchemaColumns
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string TypeString { get; set; }
        public Type Type { get; set; }
    }
}
