using Net.Json;
using Net.Proxy;
using System;
using System.Dynamic;
using Xunit;

namespace Net.Mapper.Test
{
    public class ObjectAssignTest
    {

        [Fact]
        public void IsLogicalEqualTest()
        {
            var a = new TestA() { Name="Salih"};
            var b = new TestA() { Name="Salihk"};
            var result=a.IsLogicalEqual(b);

        }

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
            dynamic testB = new ExpandoObject();
            testB.name = "Salih";
            testB.age = "3";
            testB.nested = new
            {
                Name = "Keleş",
                ages = 6
            };
            var testD=new ExpandoObject();
            var testC=new ExpandoObject();
            testC.ObjectAssign(testA);
            testD.ObjectAssign(testC);
            testA.ObjectAssign((object)testB);
        }
        [Fact]
        public void Test2()
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
            var testB = new TestA()
            {
                Name = "My Name A",
                Age = 32,
                Nested = new TestAA()
                {
                    Name = "My Name Nested A",
                    Age = 50
                }
            };
            var isEqual=testA.IsLogicalEqual(testB);
        }
        [Fact]
        public void Test3()
        {
            var testA = new TestA()
            {
                Name = "My Name A",
                Age = 32,
                Nested = new TestAA()
                {
                    Name = "",
                    Age = 50
                },
                IntList=new int[] {}
            };
            var testB = new TestA()
            {
                Name = "My Name A",
                Age = 32,
                Nested = new TestAA()
                {
                    Name = null,
                    Age = 50
                },
                IntList = null
            };
            var isEqual = testA.IsLogicalEqual(testB);
        }
    }
}
