using System;

namespace Hector.Data.Entities.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class EntityInfoAttribute : Attribute
    {
        public string TableName { get; set; } = null!;
        public string? Alias { get; set; }
        public string? Note { get; set; }
        public bool IsView { get; set; }
    }
}
