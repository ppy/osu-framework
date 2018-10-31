using System;
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
            var array = new []
            {
                "ok", "nope", "random", null, ""
            };

            var bindableCollection = new BindableCollection<string>(array);

            Assert.Multiple(() =>
            {
                foreach (var item in array)
                    Assert.Contains(item, bindableCollection);

                Assert.AreEqual(array.Length, bindableCollection.Count);
            });
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
            string addedString = "incorrect string";
            bindableStringCollection.ItemAdded += s => addedString = s;

            bindableStringCollection.Add(str);

            Assert.AreEqual(str, addedString);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriberOnce(string str)
        {
            int notificationCount = 0;
            bindableStringCollection.ItemAdded += s => notificationCount++;

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
            bindableStringCollection.ItemAdded += s => subscriberANotified = true;
            bindableStringCollection.ItemAdded += s => subscriberBNotified = true;
            bindableStringCollection.ItemAdded += s => subscriberCNotified = true;

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
            bindableStringCollection.ItemAdded += s => subscriberANotified = true;
            bindableStringCollection.ItemAdded += s => subscriberBNotified = true;
            bindableStringCollection.ItemAdded += s => subscriberCNotified = true;

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
            string existingItem = "existing string";
            bindableStringCollection.Add(existingItem);

            bindableStringCollection.Add(str);

            Assert.Contains(existingItem, bindableStringCollection);
        }

        #endregion

        #region .Remove(item)

        [Test]
        public void TestRemoveWithAnItemThatIsNotInTheCollectionReturnsFalse()
        {
            bool gotRemoved = bindableStringCollection.Remove("hm");

            Assert.IsFalse(gotRemoved);
        }

        [Test]
        public void TestRemoveWhenCollectionIsDisabledThrowsInvalidOperationException()
        {
            string item = "item";
            bindableStringCollection.Add(item);
            bindableStringCollection.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringCollection.Remove(item); });
        }

        [Test]
        public void TestRemoveWithAnItemThatIsInTheCollectionReturnsTrue()
        {
            string item = "item";
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
            bindableStringCollection.ItemRemoved += s => updated = true;

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
            bindableStringCollection.ItemRemoved += s => updatedA = true;
            bindableStringCollection.ItemRemoved += s => updatedB = true;
            bindableStringCollection.ItemRemoved += s => updatedC = true;

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
            collection.ItemRemoved += s => wasRemoved = true;

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
            collectionA.ItemRemoved += s => wasRemovedA1 = true;
            collectionA.ItemRemoved += s => wasRemovedA2 = true;
            var collectionB = new BindableCollection<string>();
            collectionB.BindTo(bindableStringCollection);
            bool wasRemovedB1 = false;
            bool wasRemovedB2 = false;
            collectionB.ItemRemoved += s => wasRemovedB1 = true;
            collectionB.ItemRemoved += s => wasRemovedB2 = true;

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
            bindableStringCollection.ItemsCleared += items => wasNotified = true;

            bindableStringCollection.Clear();

            Assert.IsTrue(wasNotified);
        }

        [Test]
        public void TestClearDoesNotNotifySubscriberBeforeClear()
        {
            bool wasNotified = false;
            bindableStringCollection.ItemsCleared += items => wasNotified = true;
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
            bindableStringCollection.ItemsCleared += items => wasNotifiedA = true;
            bool wasNotifiedB = false;
            bindableStringCollection.ItemsCleared += items => wasNotifiedB = true;
            bool wasNotifiedC = false;
            bindableStringCollection.ItemsCleared += items => wasNotifiedC = true;

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
            var array = new string[5];

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
            var array = new[] { "" };
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

        #endregion

        #region .UnbindBindings()

        public void Test

        #endregion
    }
}
