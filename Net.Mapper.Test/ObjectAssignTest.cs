using System;
using Xunit;

namespace Net.Mapper.Test
{
    public class ObjectAssignTest
    {
        [Fact]
        public void Test1()
        {
            var testA = new TestA()
            {
                Name = "My Name A",
                Age = 32,
                Nested = new TestAA()
                {
                    Name = "My Name Nested A",
                    Age = 50
                }
            };
            var testB = new TestB()
            {
                Name = "My Name B",
                Age = 40,
               
            };
            testA.ObjectAssign(testB);
        }
    }
}
