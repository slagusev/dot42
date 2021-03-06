﻿using System.Collections.Generic;
using System.Linq;
using Junit.Framework;

namespace Dot42.Tests.System
{
    public class TestArrayEnumerable : TestCase
    {
        public void testInt1()
        {
            var array = new int[] {1, 2, 3, 4, 5};
            IEnumerable<int> enumerable = array;
            var sum = enumerable.Sum();
            AssertEquals(15, sum);
        }

        public void testInt2()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };
            var sum = MyStaticSum(array);
            AssertEquals(15, sum);
        }

        public void testInt3()
        {
            var array = new int[] { 1, 2, 3, 4 };
            var sum = MyInstanceSum(array);
            AssertEquals(10, sum);
        }

        public void testIntCount1()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };
            var count = MyStaticCount(array);
            AssertEquals(5, count);
        }

        public void testIntCount2()
        {
            var array = new int[] { 1, 2, 3, 4 };
            var count = MyInstanceCount(array);
            AssertEquals(4, count);
        }

        private static int MyInstanceCount<T>(IEnumerable<T> enumerable)
        {
            return enumerable.Count();
        }

        private static int MyStaticCount<T>(IEnumerable<T> enumerable)
        {
            return enumerable.Count();
        }

        private static int MyInstanceSum(IEnumerable<int> enumerable)
        {
            return enumerable.Sum();
        }

        private static int MyStaticSum(IEnumerable<int> enumerable)
        {
            return enumerable.Sum();
        }
    }
}
