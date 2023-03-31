using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper.Test
{
    public class TestA
    {
        public string Name { get; set; }
        [Obsolete]
        public long Age { get; set; }
        public TestAA Nested { get; set; }
        public int[] IntList { get; set; }
    }
    public interface Interface1
    {
        long Day { get; set; }
    }
    public interface Interface2
    {
        Interface1[] Items { get; set; }
    }
    public class TestAA
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
