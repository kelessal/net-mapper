using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper.Test
{
    public class TestB
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public TestBB Nested { get; set; }
    }
    public class TestBB
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
   
}
