﻿using System;
using System.Dynamic;
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
    }
}
