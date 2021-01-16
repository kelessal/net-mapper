using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper.Test
{
    public class TestA
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public TestAA Nested { get; set; }
    }
    public class TestAA
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
