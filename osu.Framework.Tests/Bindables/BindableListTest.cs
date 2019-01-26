// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableListTest
    {
        private BindableList<string> bindableStringList;

        [SetUp]
        public void SetUp()
        {
            bindableStringList = new BindableList<string>();
        }

        #region Constructor

        [Test]
        public void TestConstructorDoesNotAddItemsByDefault()
        {
            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestConstructorWithItemsAddsItemsInternally()
        {
            string[] array =
            {
                "ok", "nope", "random", null, ""
            };

            var bindableList = new BindableList<string>(array);

            Assert.Multiple(() =>
            {
                foreach (string item in array)
                    Assert.Contains(item, bindableList);

                Assert.AreEqual(array.Length, bindableList.Count);
            });
        }

        #endregion

        #region list[index]

        [Test]
        public void TestGetRetrievesObjectAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            Assert.AreEqual("1", bindableStringList[1]);
        }

        [Test]
        public void TestSetMutatesObjectAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList[1] = "2";

            Assert.AreEqual("2", bindableStringList[1]);
        }

        [Test]
        public void TestGetWhileDisabledDoesNotThrowInvalidOperationException()
        {
            bindableStringList.Add("0");
            bindableStringList.Disabled = true;

            Assert.AreEqual("0", bindableStringList[0]);
        }

        [Test]
        public void TestSetWhileDisabledThrowsInvalidOperationException()
        {
            bindableStringList.Add("0");
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringList[0] = "1");
        }

        [Test]
        public void TestSetNotifiesSubscribers()
        {
            bindableStringList.Add("0");

            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;

            bindableStringList.ItemsAdded += v => addedItem = v;
            bindableStringList.ItemsRemoved += v => removedItem = v;

            bindableStringList[0] = "1";

            Assert.AreEqual(removedItem.Single(), "0");
            Assert.AreEqual(addedItem.Single(), "1");
        }

        [Test]
        public void TestSetNotifiesBoundLists()
        {
            bindableStringList.Add("0");

            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);
            list.ItemsAdded += v => addedItem = v;
            list.ItemsRemoved += v => removedItem = v;

            bindableStringList[0] = "1";

            Assert.AreEqual(removedItem.Single(), "0");
            Assert.AreEqual(addedItem.Single(), "1");
        }

        #endregion

        #region .Add(item)

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringAddsStringToEnumerator(string str)
        {
            bindableStringList.Add(str);

            Assert.Contains(str, bindableStringList);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriber(string str)
        {
            string addedString = null;
            bindableStringList.ItemsAdded += s => addedString = s.SingleOrDefault();

            bindableStringList.Add(str);

            Assert.AreEqual(str, addedString);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriberOnce(string str)
        {
            int notificationCount = 0;
            bindableStringList.ItemsAdded += s => notificationCount++;

            bindableStringList.Add(str);

            Assert.AreEqual(1, notificationCount);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesMultipleSubscribers(string str)
        {
            bool subscriberANotified = false;
            bool subscriberBNotified = false;
            bool subscriberCNotified = false;
            bindableStringList.ItemsAdded += s => subscriberANotified = true;
            bindableStringList.ItemsAdded += s => subscriberBNotified = true;
            bindableStringList.ItemsAdded += s => subscriberCNotified = true;

            bindableStringList.Add(str);

            Assert.IsTrue(subscriberANotified);
            Assert.IsTrue(subscriberBNotified);
            Assert.IsTrue(subscriberCNotified);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesMultipleSubscribersOnlyAfterTheAdd(string str)
        {
            bool subscriberANotified = false;
            bool subscriberBNotified = false;
            bool subscriberCNotified = false;
            bindableStringList.ItemsAdded += s => subscriberANotified = true;
            bindableStringList.ItemsAdded += s => subscriberBNotified = true;
            bindableStringList.ItemsAdded += s => subscriberCNotified = true;

            Assert.IsFalse(subscriberANotified);
            Assert.IsFalse(subscriberBNotified);
            Assert.IsFalse(subscriberCNotified);

            bindableStringList.Add(str);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundList(string str)
        {
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Add(str);

            Assert.Contains(str, list);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundLists(string str)
        {
            var listA = new BindableList<string>();
            var listB = new BindableList<string>();
            var listC = new BindableList<string>();
            listA.BindTo(bindableStringList);
            listB.BindTo(bindableStringList);
            listC.BindTo(bindableStringList);

            bindableStringList.Add(str);

            Assert.Contains(str, listA);
            Assert.Contains(str, listB);
            Assert.Contains(str, listC);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithDisabledListThrowsInvalidOperationException(string str)
        {
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() =>
            {
                bindableStringList.Add(str);
            });
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithListContainingItemsDoesNotOverrideItems(string str)
        {
            const string item = "existing string";
            bindableStringList.Add(item);

            bindableStringList.Add(str);

            Assert.Contains(item, bindableStringList);
        }

        #endregion

        #region .AddRange(items)

        [Test]
        public void TestAddRangeAddsItemsToEnumerator()
        {
            string[] items =
            {
                "A", "B", "C", "D"
            };

            bindableStringList.AddRange(items);

            Assert.Multiple(() =>
            {
                foreach (string item in items)
                    Assert.Contains(item, bindableStringList);
            });
        }

        [Test]
        public void TestAddRangeNotifiesBoundLists()
        {
            string[] items = { "test1", "test2", "test3" };
            var list = new BindableList<string>();
            bindableStringList.BindTo(list);
            IEnumerable<string> addedItems = null;
            list.ItemsAdded += e => addedItems = e;

            bindableStringList.AddRange(items);

            Assert.Multiple(() =>
            {
                Assert.NotNull(addedItems);
                CollectionAssert.AreEquivalent(items, addedItems);
                CollectionAssert.AreEquivalent(items, list);
            });
        }

        #endregion

        #region .Insert

        [Test]
        public void TestInsertInsertsItemAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            bindableStringList.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", bindableStringList[0]);
                Assert.AreEqual("1", bindableStringList[1]);
                Assert.AreEqual("2", bindableStringList[2]);
            });
        }

        [Test]
        public void TestInsertNotifiesSubscribers()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            bool wasAdded = false;
            bindableStringList.ItemsAdded += _ => wasAdded = true;

            bindableStringList.Insert(1, "1");

            Assert.IsTrue(wasAdded);
        }

        [Test]
        public void TestInsertNotifiesBoundLists()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            bool wasAdded = false;

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);
            list.ItemsAdded += _ => wasAdded = true;

            bindableStringList.Insert(1, "1");

            Assert.IsTrue(wasAdded);
        }

        [Test]
        public void TestInsertInsertsItemAtIndexInBoundList()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", list[0]);
                Assert.AreEqual("1", list[1]);
                Assert.AreEqual("2", list[2]);
            });
        }

        #endregion

        #region .Remove(item)

        [Test]
        public void TestRemoveWithDisabledListThrowsInvalidOperationException()
        {
            const string item = "hi";
            bindableStringList.Add(item);
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Remove(item));
        }

        [Test]
        public void TestRemoveWithAnItemThatIsNotInTheListReturnsFalse()
        {
            bool gotRemoved = bindableStringList.Remove("hm");

            Assert.IsFalse(gotRemoved);
        }

        [Test]
        public void TestRemoveWhenListIsDisabledThrowsInvalidOperationException()
        {
            const string item = "item";
            bindableStringList.Add(item);
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringList.Remove(item); });
        }

        [Test]
        public void TestRemoveWithAnItemThatIsInTheListReturnsTrue()
        {
            const string item = "item";
            bindableStringList.Add(item);

            bool gotRemoved = bindableStringList.Remove(item);

            Assert.IsTrue(gotRemoved);
        }

        [Test]
        public void TestRemoveNotifiesSubscriber()
        {
            const string item = "item";
            bindableStringList.Add(item);
            bool updated = false;
            bindableStringList.ItemsRemoved += s => updated = true;

            bindableStringList.Remove(item);

            Assert.True(updated);
        }

        [Test]
        public void TestRemoveNotifiesSubscribers()
        {
            const string item = "item";
            bindableStringList.Add(item);
            bool updatedA = false;
            bool updatedB = false;
            bool updatedC = false;
            bindableStringList.ItemsRemoved += s => updatedA = true;
            bindableStringList.ItemsRemoved += s => updatedB = true;
            bindableStringList.ItemsRemoved += s => updatedC = true;

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.True(updatedA);
                Assert.True(updatedB);
                Assert.True(updatedC);
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundList()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Remove(item);

            Assert.IsEmpty(list);
        }

        [Test]
        public void TestRemoveNotifiesBoundLists()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var listA = new BindableList<string>();
            listA.BindTo(bindableStringList);
            var listB = new BindableList<string>();
            listB.BindTo(bindableStringList);
            var listC = new BindableList<string>();
            listC.BindTo(bindableStringList);

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.False(listA.Contains(item));
                Assert.False(listB.Contains(item));
                Assert.False(listC.Contains(item));
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscription()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);
            bool wasRemoved = false;
            list.ItemsRemoved += s => wasRemoved = true;

            bindableStringList.Remove(item);

            Assert.True(wasRemoved);
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscriptions()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var listA = new BindableList<string>();
            listA.BindTo(bindableStringList);
            bool wasRemovedA1 = false;
            bool wasRemovedA2 = false;
            listA.ItemsRemoved += s => wasRemovedA1 = true;
            listA.ItemsRemoved += s => wasRemovedA2 = true;
            var listB = new BindableList<string>();
            listB.BindTo(bindableStringList);
            bool wasRemovedB1 = false;
            bool wasRemovedB2 = false;
            listB.ItemsRemoved += s => wasRemovedB1 = true;
            listB.ItemsRemoved += s => wasRemovedB2 = true;

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(wasRemovedA1);
                Assert.IsTrue(wasRemovedA2);
                Assert.IsTrue(wasRemovedB1);
                Assert.IsTrue(wasRemovedB2);
            });
        }

        [Test]
        public void TestRemoveDoesNotNotifySuBeforeItemIsRemoved()
        {
            const string item = "item";
            bindableStringList.Add(item);
        }

        #endregion

        #region .RemoveAt(index)

        [Test]
        public void TestRemoveAtRemovesItemAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            bindableStringList.RemoveAt(1);

            Assert.AreEqual("0", bindableStringList[0]);
            Assert.AreEqual("2", bindableStringList[1]);
        }

        [Test]
        public void TestRemoveAtWithDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Add("abc");
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringList.RemoveAt(0));
        }

        [Test]
        public void TestRemoveAtNotifiesSubscribers()
        {
            bool wasRemoved = false;

            bindableStringList.Add("abc");
            bindableStringList.ItemsRemoved += _ => wasRemoved = true;

            bindableStringList.RemoveAt(0);

            Assert.IsTrue(wasRemoved);
        }

        [Test]
        public void TestRemoveAtNotifiesBoundLists()
        {
            bindableStringList.Add("abc");

            bool wasRemoved = false;

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);
            list.ItemsRemoved += s => wasRemoved = true;

            bindableStringList.RemoveAt(0);

            Assert.IsTrue(wasRemoved);
        }

        #endregion

        #region .RemoveAll(match)

        [Test]
        public void TestRemoveAllRemovesMatchingElements()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, bindableStringList.Count);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("1", bindableStringList[0]);
                Assert.AreEqual("2", bindableStringList[1]);
            });
        }

        [Test]
        public void TestRemoveAllNotifiesSubscribers()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");

            List<string> itemsRemoved = null;
            bindableStringList.ItemsRemoved += i => itemsRemoved = i.ToList();
            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);
        }

        [Test]
        public void TestRemoveAllNotifiesBoundLists()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");

            List<string> itemsRemoved = null;
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);
            list.ItemsRemoved += i => itemsRemoved = i.ToList();

            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);
        }

        #endregion

        #region .Clear()

        [Test]
        public void TestClear()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

            bindableStringList.Clear();

            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestClearWithDisabledListThrowsInvalidOperationException()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Clear());
        }

        [Test]
        public void TestClearWithEmptyDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Clear());
        }

        [Test]
        public void TestClearUpdatesCountProperty()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

            bindableStringList.Clear();

            Assert.AreEqual(0, bindableStringList.Count);
        }

        [Test]
        public void TestClearNotifiesSubscriber()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            bool wasNotified = false;
            bindableStringList.ItemsRemoved += items => wasNotified = true;

            bindableStringList.Clear();

            Assert.IsTrue(wasNotified);
        }

        [Test]
        public void TestClearDoesNotNotifySubscriberBeforeClear()
        {
            bool wasNotified = false;
            bindableStringList.ItemsRemoved += items => wasNotified = true;
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

            Assert.IsFalse(wasNotified);

            bindableStringList.Clear();
        }

        [Test]
        public void TestClearNotifiesSubscribers()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            bool wasNotifiedA = false;
            bindableStringList.ItemsRemoved += items => wasNotifiedA = true;
            bool wasNotifiedB = false;
            bindableStringList.ItemsRemoved += items => wasNotifiedB = true;
            bool wasNotifiedC = false;
            bindableStringList.ItemsRemoved += items => wasNotifiedC = true;

            bindableStringList.Clear();

            Assert.Multiple(() =>
            {
                Assert.IsTrue(wasNotifiedA);
                Assert.IsTrue(wasNotifiedB);
                Assert.IsTrue(wasNotifiedC);
            });
        }

        [Test]
        public void TestClearNotifiesBoundBindable()
        {
            var bindableList = new BindableList<string>();
            bindableList.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableList.Add("testA");

            bindableStringList.Clear();

            Assert.IsEmpty(bindableList);
        }

        [Test]
        public void TestClearNotifiesBoundBindables()
        {
            var bindableListA = new BindableList<string>();
            bindableListA.BindTo(bindableStringList);
            var bindableListB = new BindableList<string>();
            bindableListB.BindTo(bindableStringList);
            var bindableListC = new BindableList<string>();
            bindableListC.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListC.Add("testA");

            bindableStringList.Clear();

            Assert.Multiple(() =>
            {
                Assert.IsEmpty(bindableListA);
                Assert.IsEmpty(bindableListB);
                Assert.IsEmpty(bindableListC);
            });
        }

        [Test]
        public void TestClearDoesNotNotifyBoundBindablesBeforeClear()
        {
            var bindableListA = new BindableList<string>();
            bindableListA.BindTo(bindableStringList);
            var bindableListB = new BindableList<string>();
            bindableListB.BindTo(bindableStringList);
            var bindableListC = new BindableList<string>();
            bindableListC.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListC.Add("testA");

            Assert.Multiple(() =>
            {
                Assert.IsNotEmpty(bindableListA);
                Assert.IsNotEmpty(bindableListB);
                Assert.IsNotEmpty(bindableListC);
            });

            bindableStringList.Clear();
        }

        #endregion

        #region .CopyTo(array, index)

        [Test]
        public void TestCopyTo()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("test" + i);
            string[] array = new string[5];

            bindableStringList.CopyTo(array, 0);

            CollectionAssert.AreEquivalent(bindableStringList, array);
        }

        #endregion

        #region .Disabled

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscriber()
        {
            bool? isDisabled = null;
            bindableStringList.DisabledChanged += b => isDisabled = b;

            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(isDisabled);
                Assert.IsTrue(isDisabled.Value);
            });
        }

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscribers()
        {
            bool? isDisabledA = null;
            bool? isDisabledB = null;
            bool? isDisabledC = null;
            bindableStringList.DisabledChanged += b => isDisabledA = b;
            bindableStringList.DisabledChanged += b => isDisabledB = b;
            bindableStringList.DisabledChanged += b => isDisabledC = b;

            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(isDisabledA);
                Assert.IsTrue(isDisabledA.Value);
                Assert.IsNotNull(isDisabledB);
                Assert.IsTrue(isDisabledB.Value);
                Assert.IsNotNull(isDisabledC);
                Assert.IsTrue(isDisabledC.Value);
            });
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscriber()
        {
            bindableStringList.DisabledChanged += b => Assert.Fail();

            bindableStringList.Disabled = bindableStringList.Disabled;
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscribers()
        {
            bindableStringList.DisabledChanged += b => Assert.Fail();
            bindableStringList.DisabledChanged += b => Assert.Fail();
            bindableStringList.DisabledChanged += b => Assert.Fail();

            bindableStringList.Disabled = bindableStringList.Disabled;
        }

        [Test]
        public void TestDisabledNotifiesBoundLists()
        {
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Disabled = true;

            Assert.IsTrue(list.Disabled);
        }

        #endregion

        #region .GetEnumberator()

        [Test]
        public void TestGetEnumeratorDoesNotReturnNull()
        {
            Assert.NotNull(bindableStringList.GetEnumerator());
        }

        [Test]
        public void TestGetEnumeratorWhenCopyConstructorIsUsedDoesNotReturnTheEnumeratorOfTheInputtedEnumerator()
        {
            string[] array = { "" };
            var list = new BindableList<string>(array);

            var enumerator = list.GetEnumerator();

            Assert.AreNotEqual(array.GetEnumerator(), enumerator);
        }

        #endregion

        #region .Description

        [Test]
        public void TestDescriptionWhenSetReturnsSetValue()
        {
            const string description = "The list used for testing.";

            bindableStringList.Description = description;

            Assert.AreEqual(description, bindableStringList.Description);
        }

        #endregion

        #region .Parse(obj)

        [Test]
        public void TestParseWithNullClearsList()
        {
            bindableStringList.Add("a item");

            bindableStringList.Parse(null);

            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestParseWithArray()
        {
            IEnumerable<string> strings = new [] { "testA", "testB" };

            bindableStringList.Parse(strings);

            CollectionAssert.AreEquivalent(strings, bindableStringList);
        }

        [Test]
        public void TestParseWithDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Parse(null));
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Parse(new object[] {
                    "test", "testabc", "asdasdasdasd"
                }));
            });
        }

        [Test]
        public void TestParseWithInvalidArgumentTypesThrowsArgumentException()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(""));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(new object()));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1.1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1.1f));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse("test123"));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(29387L));
            });
        }

        [Test]
        public void TestParseWithNullNotifiesClearSubscribers()
        {
            string[] strings = { "testA", "testB", "testC" };
            bindableStringList.AddRange(strings);
            bool itemsGotCleared = false;
            IEnumerable<string> clearedItems = null;
            bindableStringList.ItemsRemoved += items =>
            {
                itemsGotCleared = true;
                clearedItems = items;
            };

            bindableStringList.Parse(null);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, clearedItems);
                Assert.IsTrue(itemsGotCleared);
            });
        }

        [Test]
        public void TestParseWithItemsNotifiesAddRangeAndClearSubscribers()
        {
            bindableStringList.Add("test123");

            IEnumerable<string> strings = new []{ "testA", "testB" };
            IEnumerable<string> addedItems = null;
            bool? itemsWereFirstCleaned = null;
            bindableStringList.ItemsAdded += items =>
            {
                addedItems = items;
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = false;
            };
            bindableStringList.ItemsRemoved += items => {
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = true;
            };

            bindableStringList.Parse(strings);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, bindableStringList);
                CollectionAssert.AreEquivalent(strings, addedItems);
                Assert.NotNull(itemsWereFirstCleaned);
                Assert.IsTrue(itemsWereFirstCleaned ?? false);
            });
        }

        #endregion

        #region GetBoundCopy()

        [Test]
        public void TestBoundCopyWithAdd()
        {
            var boundCopy = bindableStringList.GetBoundCopy();
            bool boundCopyItemAdded = false;
            boundCopy.ItemsAdded += item => boundCopyItemAdded = true;

            bindableStringList.Add("test");

            Assert.IsTrue(boundCopyItemAdded);
        }

        #endregion
    }
}
