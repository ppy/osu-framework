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
            Assert.AreEqual(0, bindableStringCollection.Count);
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

        #region .Add

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




    }
}
