using System;

namespace Net.Mapper
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
    public class NoMapAttribute:Attribute
    {
    }
}
