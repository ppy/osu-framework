using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableCollectionTest
    {
        private BindableCollection<string> bindableStringCollection;
        private BindableCollection<TestEnum> bindableEnumCollection;

        [SetUp]
        public void SetUp()
        {
            bindableStringCollection = new BindableCollection<string>();
            bindableEnumCollection = new BindableCollection<TestEnum>();
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringAddsStringToEnumerator(string str)
        {
            bindableStringCollection.Add(str);

            Assert.Contains(str, bindableStringCollection);
        }



        private enum TestEnum
        {
            Test0,
            Test1,
            Test2,
            Test3,
            Test4,
            Test5,
            Test6,
            Test7,
            Test8,
            Test9,
            Test10,
            Test11,
            Test12
        }
    }
}
