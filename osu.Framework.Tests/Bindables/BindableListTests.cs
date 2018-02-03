// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableListTests
    {
        [Test]
        public void TestAdd()
        {
            string expected = "string";
            var list = new BindableList<string>();

            list.Add(expected);

            Assert.AreEqual(expected, list[0]);
        }

        [Test]
        public void TestClearRemovesAllItems()
        {
            var list = new BindableList<string> { "str1", "str2", "str3", "str4" };

            list.Clear();

            Assert.AreEqual(0, list.Count);
            Assert.Throws<ArgumentOutOfRangeException>(
                // ReSharper disable once RedundantToStringCall
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                () => list[0].ToString()
            );
        }

        [Test]
        public void TestRemoveRemovesTheGivenItem()
        {
            string itemSupposedToBeRemoved = "removeMe";
            var list = new BindableList<string> {  "str1", "str2", itemSupposedToBeRemoved };

            bool gotRemoved = list.Remove(itemSupposedToBeRemoved);

            Assert.IsTrue(gotRemoved);
            Assert.AreEqual(2, list.Count);
            Assert.IsFalse(list.Contains(itemSupposedToBeRemoved));
        }

        [Test]
        public void TestRemoveReturnsFalseWhenItemIsNotInList()
        {
            string itemSupposedToBeRemoved = "removeMe";
            var list = new BindableList<string> { "str1", "str2", "str3" };

            bool gotRemoved = list.Remove(itemSupposedToBeRemoved);

            Assert.IsFalse(gotRemoved);
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TestInsertAt(int index)
        {
            string insertString = "This string is going to be inserted";

            var list = new BindableList<string>
            {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };

            list.Insert(index, insertString);

            Assert.AreEqual(insertString, list[index]);
            Assert.AreEqual(9, list.Count);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TestRemoveAt(int index)
        {
            string[] strings = {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };
            var list = new BindableList<string>();
            list.AddAll(strings);

            list.RemoveAt(index);

            Assert.AreNotEqual(strings[index], list[index]);
            Assert.AreEqual(strings.Length, list.Count + 1);
        }

        [Test]
        public void TestGet()
        {
            string[] strings = {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };
            var list = new BindableList<string>();
            list.AddAll(strings);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(strings[i], list[i]);
            }
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        [Test]
        public void TestOverride(int index)
        {
            string newString = "new text";
            string[] strings = {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };
            var list = new BindableList<string>();
            list.AddAll(strings);

            list[index] = newString;

            for (int i = 0; i < strings.Length; i++)
            {
                if (i == index)
                    Assert.AreEqual(newString, list[i]);
                else
                    Assert.AreEqual(strings[i], list[i]);
            }
        }

        [Test]
        public void TestClearThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string>();
            list.AddAll(new List<string>{
                "test1", "test2"
            });

            list.Disabled = true;

            Assert.Throws<InvalidOperationException>(
                () => list.Clear());
        }

        [Test]
        public void TestRemoveThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string>();
            list.AddAll(new List<string>{
                "test1", "test2"
            });

            list.Disabled = true;

            Assert.Throws<InvalidOperationException>(
                () => list.Remove(list[0]));
        }

        [Test]
        public void TestAddThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string> { Disabled = true };

            Assert.Throws<InvalidOperationException>(
                () => list.Add("A random string"));
        }

        [Test]
        public void TestInsertThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string> { Disabled = true };

            Assert.Throws<InvalidOperationException>(
                () => list.Insert(0,"A random string"));
        }

        [Test]
        public void TestRemoveAtThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string> { Disabled = true };

            Assert.Throws<InvalidOperationException>(
                () => list.RemoveAt(0));
        }

        [Test]
        public void TestOverrideThrowsDisabledExceptionWhenDisabled()
        {
            var list = new BindableList<string> { "asdf"};
            list.Disabled = true;


            Assert.Throws<InvalidOperationException>(
                () => list[0] = "");
        }

        [TestCase(true)]
        [TestCase(false)]
        [Test]
        public void TestIsReadOnlyReturnsDisabledValue(bool disabled)
        {
            var list = new BindableList<string> { "asdf" };

            list.Disabled = disabled;

            Assert.AreEqual(disabled, list.IsReadOnly);
        }

        [Test]
        public void TestDisabledDefaultsToFalse()
        {
            var list = new BindableList<string>();

            Assert.IsFalse(list.Disabled);
        }

        [Test]
        public void TestIsReadOnlyDefaultsToFalse()
        {
            var list = new BindableList<string>();

            Assert.IsFalse(list.IsReadOnly);
        }

        [Test]
        public void TestIndexOf()
        {
            string[] strings = {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };
            var list = new BindableList<string>();
            list.AddAll(strings);

            for (int i = 0; i < strings.Length; i++)
            {
                Assert.AreEqual(i, list.IndexOf(strings[i]));
            }
        }

        [Test]
        public void TestCopyTo()
        {
            var list = new BindableList<string>
            {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };
            var targetArr = new string[list.Count * 2];

            list.CopyTo(targetArr, 0);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i], targetArr[i]);
            }
        }

        [Test]
        public void TestAsReadOnlyList()
        {
            var list = new BindableList<string> {
                "str1", "str2", "str3", "str4", "str5", "str6", "str7", "str8",
            };

            IReadOnlyList<string> readOnlyList = list.AsReadOnlyList();

            Assert.AreEqual(list.Count, readOnlyList.Count);
            for (int i = 0; i < list.Count; i++)
                Assert.AreEqual(list[i], readOnlyList[i]);
        }

        [Test]
        public void TestBindToSyncsItems()
        {
            var list1 = new BindableList<string> { "str1", "str2", "str3" };
            var list2 = new BindableList<string>();
            list2.BindTo(list1);

            Assert.AreEqual(list1.Count, list2.Count);
            for (int i = 0; i < list1.Count; i++)
                Assert.AreEqual(list1[i], list2[i]);
        }

        [TestCase(true)]
        [TestCase(false)]
        [Test]
        public void TestBindToSyncsDisabledState(bool disabled)
        {
            var list1 = new BindableList<string> { "str1", "str2", "str3" };
            list1.Disabled = disabled;
            var list2 = new BindableList<string>();
            list2.BindTo(list1);

            Assert.AreEqual(list1.Count, list2.Count);
        }

        [Test]
        public void TestAddSyncsAddedValue()
        {
            string newString1 = "a";
            string newString2 = "b";
            var list1 = new BindableList<string> { "str1", "str2", "str3" };
            var list2 = new BindableList<string>();
            list2.BindTo(list1);

            list1.Add(newString1);
            list2.Add(newString2);

            Assert.AreEqual(list1.Count, list2.Count);
            for (int i = 0; i < list1.Count; i++)
                Assert.AreEqual(list1[i], list2[i]);
        }
    }
}
