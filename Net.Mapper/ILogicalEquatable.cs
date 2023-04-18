using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper
{
    public interface ILogicalEquatable
    {
        bool LogicalEquals(object other);
    }
}
