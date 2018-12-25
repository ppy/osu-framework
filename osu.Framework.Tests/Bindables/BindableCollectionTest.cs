// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableCollectionTest
    {
        private BindableCollection<string> bindableStringCollection;

        [SetUp]
        public void SetUp()
        {
            bindableStringCollection = new BindableCollection<string>();
        }

        #region Constructor

        [Test]
        public void TestConstructorDoesNotAddItemsByDefault()
        {
            Assert.IsEmpty(bindableStringCollection);
        }

        [Test]
        public void TestConstructorWithItemsAddsItemsInternally()
        {
            string[] array =
            {
                "ok", "nope", "random", null, ""
            };

            var bindableCollection = new BindableCollection<string>(array);

            Assert.Multiple(() =>
            {
                foreach (string item in array)
                    Assert.Contains(item, bindableCollection);

                Assert.AreEqual(array.Length, bindableCollection.Count);
            });
        }

        #endregion

        #region collection[index]

        [Test]
        public void TestGetRetrievesObjectAtIndex()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("1");
            bindableStringCollection.Add("2");

            Assert.AreEqual("1", bindableStringCollection[1]);
        }

        [Test]
        public void TestSetMutatesObjectAtIndex()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("1");
            bindableStringCollection[1] = "2";

            Assert.AreEqual("2", bindableStringCollection[1]);
        }

        [Test]
        public void TestGetWhileDisabledDoesNotThrowInvalidOperationException()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Disabled = true;

            Assert.AreEqual("0", bindableStringCollection[0]);
        }

        [Test]
        public void TestSetWhileDisabledThrowsInvalidOperationException()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringCollection[0] = "1");
        }

        [Test]
        public void TestSetNotifiesSubscribers()
        {
            bindableStringCollection.Add("0");

            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;

            bindableStringCollection.ItemsAdded += v => addedItem = v;
            bindableStringCollection.ItemsRemoved += v => removedItem = v;

            bindableStringCollection[0] = "1";

            Assert.AreEqual(removedItem.Single(), "0");
            Assert.AreEqual(addedItem.Single(), "1");
        }

        [Test]
        public void TestSetNotifiesBoundCollections()
        {
            bindableStringCollection.Add("0");

            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;

            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);
            collection.ItemsAdded += v => addedItem = v;
            collection.ItemsRemoved += v => removedItem = v;

            bindableStringCollection[0] = "1";

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
            bindableStringCollection.Add(str);

            Assert.Contains(str, bindableStringCollection);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriber(string str)
        {
            string addedString = null;
            bindableStringCollection.ItemsAdded += s => addedString = s.SingleOrDefault();

            bindableStringCollection.Add(str);

            Assert.AreEqual(str, addedString);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriberOnce(string str)
        {
            int notificationCount = 0;
            bindableStringCollection.ItemsAdded += s => notificationCount++;

            bindableStringCollection.Add(str);

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
            bindableStringCollection.ItemsAdded += s => subscriberANotified = true;
            bindableStringCollection.ItemsAdded += s => subscriberBNotified = true;
            bindableStringCollection.ItemsAdded += s => subscriberCNotified = true;

            bindableStringCollection.Add(str);

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
            bindableStringCollection.ItemsAdded += s => subscriberANotified = true;
            bindableStringCollection.ItemsAdded += s => subscriberBNotified = true;
            bindableStringCollection.ItemsAdded += s => subscriberCNotified = true;

            Assert.IsFalse(subscriberANotified);
            Assert.IsFalse(subscriberBNotified);
            Assert.IsFalse(subscriberCNotified);

            bindableStringCollection.Add(str);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundCollection(string str)
        {
            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);

            bindableStringCollection.Add(str);

            Assert.Contains(str, collection);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundCollections(string str)
        {
            var collectionA = new BindableCollection<string>();
            var collectionB = new BindableCollection<string>();
            var collectionC = new BindableCollection<string>();
            collectionA.BindTo(bindableStringCollection);
            collectionB.BindTo(bindableStringCollection);
            collectionC.BindTo(bindableStringCollection);

            bindableStringCollection.Add(str);

            Assert.Contains(str, collectionA);
            Assert.Contains(str, collectionB);
            Assert.Contains(str, collectionC);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithDisabledCollectionThrowsInvalidOperationException(string str)
        {
            bindableStringCollection.Disabled = true;

            Assert.Throws<InvalidOperationException>(() =>
            {
                bindableStringCollection.Add(str);
            });
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithCollectionContainingItemsDoesNotOverrideItems(string str)
        {
            const string item = "existing string";
            bindableStringCollection.Add(item);

            bindableStringCollection.Add(str);

            Assert.Contains(item, bindableStringCollection);
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

            bindableStringCollection.AddRange(items);

            Assert.Multiple(() =>
            {
                foreach (string item in items)
                    Assert.Contains(item, bindableStringCollection);
            });
        }

        [Test]
        public void TestAddRangeNotifiesBoundCollections()
        {
            string[] items = { "test1", "test2", "test3" };
            var collection = new BindableCollection<string>();
            bindableStringCollection.BindTo(collection);
            IEnumerable<string> addedItems = null;
            collection.ItemsAdded += e => addedItems = e;

            bindableStringCollection.AddRange(items);

            Assert.Multiple(() =>
            {
                Assert.NotNull(addedItems);
                CollectionAssert.AreEquivalent(items, addedItems);
                CollectionAssert.AreEquivalent(items, collection);
            });
        }

        #endregion

        #region .Insert

        [Test]
        public void TestInsertInsertsItemAtIndex()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("2");

            bindableStringCollection.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", bindableStringCollection[0]);
                Assert.AreEqual("1", bindableStringCollection[1]);
                Assert.AreEqual("2", bindableStringCollection[2]);
            });
        }

        [Test]
        public void TestInsertNotifiesSubscribers()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("2");

            bool wasAdded = false;
            bindableStringCollection.ItemsAdded += _ => wasAdded = true;

            bindableStringCollection.Insert(1, "1");

            Assert.IsTrue(wasAdded);
        }

        [Test]
        public void TestInsertNotifiesBoundCollections()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("2");

            bool wasAdded = false;

            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);
            collection.ItemsAdded += _ => wasAdded = true;

            bindableStringCollection.Insert(1, "1");

            Assert.IsTrue(wasAdded);
        }

        [Test]
        public void TestInsertInsertsItemAtIndexInBoundCollection()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("2");

            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);

            bindableStringCollection.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", collection[0]);
                Assert.AreEqual("1", collection[1]);
                Assert.AreEqual("2", collection[2]);
            });
        }

        #endregion

        #region .Remove(item)

        [Test]
        public void TestRemoveWithDisabledCollectionThrowsInvalidOperationException()
        {
            const string item = "hi";
            bindableStringCollection.Add(item);
            bindableStringCollection.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringCollection.Remove(item));
        }

        [Test]
        public void TestRemoveWithAnItemThatIsNotInTheCollectionReturnsFalse()
        {
            bool gotRemoved = bindableStringCollection.Remove("hm");

            Assert.IsFalse(gotRemoved);
        }

        [Test]
        public void TestRemoveWhenCollectionIsDisabledThrowsInvalidOperationException()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            bindableStringCollection.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringCollection.Remove(item); });
        }

        [Test]
        public void TestRemoveWithAnItemThatIsInTheCollectionReturnsTrue()
        {
            const string item = "item";
            bindableStringCollection.Add(item);

            bool gotRemoved = bindableStringCollection.Remove(item);

            Assert.IsTrue(gotRemoved);
        }

        [Test]
        public void TestRemoveNotifiesSubscriber()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            bool updated = false;
            bindableStringCollection.ItemsRemoved += s => updated = true;

            bindableStringCollection.Remove(item);

            Assert.True(updated);
        }

        [Test]
        public void TestRemoveNotifiesSubscribers()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            bool updatedA = false;
            bool updatedB = false;
            bool updatedC = false;
            bindableStringCollection.ItemsRemoved += s => updatedA = true;
            bindableStringCollection.ItemsRemoved += s => updatedB = true;
            bindableStringCollection.ItemsRemoved += s => updatedC = true;

            bindableStringCollection.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.True(updatedA);
                Assert.True(updatedB);
                Assert.True(updatedC);
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundCollection()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);

            bindableStringCollection.Remove(item);

            Assert.IsEmpty(collection);
        }

        [Test]
        public void TestRemoveNotifiesBoundCollections()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            var collectionA = new BindableCollection<string>();
            collectionA.BindTo(bindableStringCollection);
            var collectionB = new BindableCollection<string>();
            collectionB.BindTo(bindableStringCollection);
            var collectionC = new BindableCollection<string>();
            collectionC.BindTo(bindableStringCollection);

            bindableStringCollection.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.False(collectionA.Contains(item));
                Assert.False(collectionB.Contains(item));
                Assert.False(collectionC.Contains(item));
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundCollectionSubscription()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);
            bool wasRemoved = false;
            collection.ItemsRemoved += s => wasRemoved = true;

            bindableStringCollection.Remove(item);

            Assert.True(wasRemoved);
        }

        [Test]
        public void TestRemoveNotifiesBoundCollectionSubscriptions()
        {
            const string item = "item";
            bindableStringCollection.Add(item);
            var collectionA = new BindableCollection<string>();
            collectionA.BindTo(bindableStringCollection);
            bool wasRemovedA1 = false;
            bool wasRemovedA2 = false;
            collectionA.ItemsRemoved += s => wasRemovedA1 = true;
            collectionA.ItemsRemoved += s => wasRemovedA2 = true;
            var collectionB = new BindableCollection<string>();
            collectionB.BindTo(bindableStringCollection);
            bool wasRemovedB1 = false;
            bool wasRemovedB2 = false;
            collectionB.ItemsRemoved += s => wasRemovedB1 = true;
            collectionB.ItemsRemoved += s => wasRemovedB2 = true;

            bindableStringCollection.Remove(item);

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
            bindableStringCollection.Add(item);
        }

        #endregion

        #region .RemoveAt(index)

        [Test]
        public void TestRemoveAtRemovesItemAtIndex()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("1");
            bindableStringCollection.Add("2");

            bindableStringCollection.RemoveAt(1);

            Assert.AreEqual("0", bindableStringCollection[0]);
            Assert.AreEqual("2", bindableStringCollection[1]);
        }

        [Test]
        public void TestRemoveAtWithDisabledCollectionThrowsInvalidOperationException()
        {
            bindableStringCollection.Add("abc");
            bindableStringCollection.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringCollection.RemoveAt(0));
        }

        [Test]
        public void TestRemoveAtNotifiesSubscribers()
        {
            bool wasRemoved = false;

            bindableStringCollection.Add("abc");
            bindableStringCollection.ItemsRemoved += _ => wasRemoved = true;

            bindableStringCollection.RemoveAt(0);

            Assert.IsTrue(wasRemoved);
        }

        [Test]
        public void TestRemoveAtNotifiesBoundCollections()
        {
            bindableStringCollection.Add("abc");

            bool wasRemoved = false;

            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);
            collection.ItemsRemoved += s => wasRemoved = true;

            bindableStringCollection.RemoveAt(0);

            Assert.IsTrue(wasRemoved);
        }

        #endregion

        #region .RemoveAll(match)

        [Test]
        public void TestRemoveAllRemovesMatchingElements()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("1");
            bindableStringCollection.Add("2");

            bindableStringCollection.RemoveAll(m => m == "0");

            Assert.AreEqual(2, bindableStringCollection.Count);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("1", bindableStringCollection[0]);
                Assert.AreEqual("2", bindableStringCollection[1]);
            });
        }

        [Test]
        public void TestRemoveAllNotifiesSubscribers()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("0");

            List<string> itemsRemoved = null;
            bindableStringCollection.ItemsRemoved += i => itemsRemoved = i.ToList();
            bindableStringCollection.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);
        }

        [Test]
        public void TestRemoveAllNotifiesBoundCollections()
        {
            bindableStringCollection.Add("0");
            bindableStringCollection.Add("0");

            List<string> itemsRemoved = null;
            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);
            collection.ItemsRemoved += i => itemsRemoved = i.ToList();

            bindableStringCollection.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);
        }

        #endregion

        #region .Clear()

        [Test]
        public void TestClear()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");

            bindableStringCollection.Clear();

            Assert.IsEmpty(bindableStringCollection);
        }

        [Test]
        public void TestClearWithDisabledCollectionThrowsInvalidOperationException()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            bindableStringCollection.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringCollection.Clear());
        }

        [Test]
        public void TestClearWithEmptyDisabledCollectionThrowsInvalidOperationException()
        {
            bindableStringCollection.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringCollection.Clear());
        }

        [Test]
        public void TestClearUpdatesCountProperty()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");

            bindableStringCollection.Clear();

            Assert.AreEqual(0, bindableStringCollection.Count);
        }

        [Test]
        public void TestClearNotifiesSubscriber()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            bool wasNotified = false;
            bindableStringCollection.ItemsRemoved += items => wasNotified = true;

            bindableStringCollection.Clear();

            Assert.IsTrue(wasNotified);
        }

        [Test]
        public void TestClearDoesNotNotifySubscriberBeforeClear()
        {
            bool wasNotified = false;
            bindableStringCollection.ItemsRemoved += items => wasNotified = true;
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");

            Assert.IsFalse(wasNotified);

            bindableStringCollection.Clear();
        }

        [Test]
        public void TestClearNotifiesSubscribers()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            bool wasNotifiedA = false;
            bindableStringCollection.ItemsRemoved += items => wasNotifiedA = true;
            bool wasNotifiedB = false;
            bindableStringCollection.ItemsRemoved += items => wasNotifiedB = true;
            bool wasNotifiedC = false;
            bindableStringCollection.ItemsRemoved += items => wasNotifiedC = true;

            bindableStringCollection.Clear();

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
            var bindableCollection = new BindableCollection<string>();
            bindableCollection.BindTo(bindableStringCollection);
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollection.Add("testA");

            bindableStringCollection.Clear();

            Assert.IsEmpty(bindableCollection);
        }

        [Test]
        public void TestClearNotifiesBoundBindables()
        {
            var bindableCollectionA = new BindableCollection<string>();
            bindableCollectionA.BindTo(bindableStringCollection);
            var bindableCollectionB = new BindableCollection<string>();
            bindableCollectionB.BindTo(bindableStringCollection);
            var bindableCollectionC = new BindableCollection<string>();
            bindableCollectionC.BindTo(bindableStringCollection);
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionC.Add("testA");

            bindableStringCollection.Clear();

            Assert.Multiple(() =>
            {
                Assert.IsEmpty(bindableCollectionA);
                Assert.IsEmpty(bindableCollectionB);
                Assert.IsEmpty(bindableCollectionC);
            });
        }

        [Test]
        public void TestClearDoesNotNotifyBoundBindablesBeforeClear()
        {
            var bindableCollectionA = new BindableCollection<string>();
            bindableCollectionA.BindTo(bindableStringCollection);
            var bindableCollectionB = new BindableCollection<string>();
            bindableCollectionB.BindTo(bindableStringCollection);
            var bindableCollectionC = new BindableCollection<string>();
            bindableCollectionC.BindTo(bindableStringCollection);
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableCollectionC.Add("testA");

            Assert.Multiple(() =>
            {
                Assert.IsNotEmpty(bindableCollectionA);
                Assert.IsNotEmpty(bindableCollectionB);
                Assert.IsNotEmpty(bindableCollectionC);
            });

            bindableStringCollection.Clear();
        }

        #endregion

        #region .CopyTo(array, index)

        [Test]
        public void TestCopyTo()
        {
            for (int i = 0; i < 5; i++)
                bindableStringCollection.Add("test" + i);
            string[] array = new string[5];

            bindableStringCollection.CopyTo(array, 0);

            CollectionAssert.AreEquivalent(bindableStringCollection, array);
        }

        #endregion

        #region .Disabled

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscriber()
        {
            bool? isDisabled = null;
            bindableStringCollection.DisabledChanged += b => isDisabled = b;

            bindableStringCollection.Disabled = true;

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
            bindableStringCollection.DisabledChanged += b => isDisabledA = b;
            bindableStringCollection.DisabledChanged += b => isDisabledB = b;
            bindableStringCollection.DisabledChanged += b => isDisabledC = b;

            bindableStringCollection.Disabled = true;

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
            bindableStringCollection.DisabledChanged += b => Assert.Fail();

            bindableStringCollection.Disabled = bindableStringCollection.Disabled;
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscribers()
        {
            bindableStringCollection.DisabledChanged += b => Assert.Fail();
            bindableStringCollection.DisabledChanged += b => Assert.Fail();
            bindableStringCollection.DisabledChanged += b => Assert.Fail();

            bindableStringCollection.Disabled = bindableStringCollection.Disabled;
        }

        [Test]
        public void TestDisabledNotifiesBoundCollections()
        {
            var collection = new BindableCollection<string>();
            collection.BindTo(bindableStringCollection);

            bindableStringCollection.Disabled = true;

            Assert.IsTrue(collection.Disabled);
        }

        #endregion

        #region .GetEnumberator()

        [Test]
        public void TestGetEnumeratorDoesNotReturnNull()
        {
            Assert.NotNull(bindableStringCollection.GetEnumerator());
        }

        [Test]
        public void TestGetEnumeratorWhenCopyConstructorIsUsedDoesNotReturnTheEnumeratorOfTheInputtedEnumerator()
        {
            string[] array = { "" };
            var collection = new BindableCollection<string>(array);

            var enumerator = collection.GetEnumerator();

            Assert.AreNotEqual(array.GetEnumerator(), enumerator);
        }

        #endregion

        #region .Description

        [Test]
        public void TestDescriptionWhenSetReturnsSetValue()
        {
            const string description = "The collection used for testing.";

            bindableStringCollection.Description = description;

            Assert.AreEqual(description, bindableStringCollection.Description);
        }

        #endregion

        #region .Parse(obj)

        [Test]
        public void TestParseWithNullClearsCollection()
        {
            bindableStringCollection.Add("a item");

            bindableStringCollection.Parse(null);

            Assert.IsEmpty(bindableStringCollection);
        }

        [Test]
        public void TestParseWithArray()
        {
            IEnumerable<string> strings = new [] { "testA", "testB" };

            bindableStringCollection.Parse(strings);

            CollectionAssert.AreEquivalent(strings, bindableStringCollection);
        }

        [Test]
        public void TestParseWithDisabledCollectionThrowsInvalidOperationException()
        {
            bindableStringCollection.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringCollection.Parse(null));
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringCollection.Parse(new object[] {
                    "test", "testabc", "asdasdasdasd"
                }));
            });
        }

        [Test]
        public void TestParseWithInvalidArgumentTypesThrowsArgumentException()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(""));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(new object()));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(1.1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(1.1f));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse("test123"));
                Assert.Throws(typeof(ArgumentException), () => bindableStringCollection.Parse(29387L));
            });
        }

        [Test]
        public void TestParseWithNullNotifiesClearSubscribers()
        {
            string[] strings = { "testA", "testB", "testC" };
            bindableStringCollection.AddRange(strings);
            bool itemsGotCleared = false;
            IEnumerable<string> clearedItems = null;
            bindableStringCollection.ItemsRemoved += items =>
            {
                itemsGotCleared = true;
                clearedItems = items;
            };

            bindableStringCollection.Parse(null);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, clearedItems);
                Assert.IsTrue(itemsGotCleared);
            });
        }

        [Test]
        public void TestParseWithItemsNotifiesAddRangeAndClearSubscribers()
        {
            bindableStringCollection.Add("test123");

            IEnumerable<string> strings = new []{ "testA", "testB" };
            IEnumerable<string> addedItems = null;
            bool? itemsWereFirstCleaned = null;
            bindableStringCollection.ItemsAdded += items =>
            {
                addedItems = items;
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = false;
            };
            bindableStringCollection.ItemsRemoved += items => {
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = true;
            };

            bindableStringCollection.Parse(strings);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, bindableStringCollection);
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
            var boundCopy = bindableStringCollection.GetBoundCopy();
            bool boundCopyItemAdded = false;
            boundCopy.ItemsAdded += item => boundCopyItemAdded = true;

            bindableStringCollection.Add("test");

            Assert.IsTrue(boundCopyItemAdded);
        }

        #endregion
    }
}
