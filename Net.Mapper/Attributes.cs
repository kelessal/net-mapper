using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
    public class IgnoreAssignAttribute:Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NoMapAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreCompareAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PropertyMapAttribute : Attribute
    {
        public string MappedPropertyName { get; private set; }

        public PropertyMapAttribute(string mappedPropertyName)
        {
            this.MappedPropertyName = mappedPropertyName;
        }
    }
}
