using System;

namespace Net.Mapper
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
    public class PropertyMapAttribute : Attribute
    {
        public string MappedPropertyName { get; private set; }

        public PropertyMapAttribute(string mappedPropertyName)
        {
            this.MappedPropertyName = mappedPropertyName;
        }
    }
}
