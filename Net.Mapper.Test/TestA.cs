using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Mapper.Test
{
    public class TestA
    {
        public Dictionary<long, int> Dic { get; set; }=new Dictionary<long, int>();
        public string Name { get; set; }
        [Obsolete]
        public long Age { get; set; }
        public TestAA Nested { get; set; }
        public int[] IntList { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj is TestA x) return x.Name ==this.Name;
            return false;
        }
    }
  
    public class TestAA
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
