using System;

namespace Xandernate.Annotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignObjectAttribute : Attribute { }
}
