// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableDictionaryTest
    {
        private BindableDictionary<string, byte> bindableStringByteDictionary;

        [SetUp]
        public void SetUp()
        {
            bindableStringByteDictionary = new BindableDictionary<string, byte>();
        }

        #region Constructor

        [Test]
        public void TestConstructorDoesNotAddItemsByDefault()
        {
            Assert.IsEmpty(bindableStringByteDictionary);
        }

        [Test]
        public void TestConstructorWithItemsAddsItemsInternally()
        {
            KeyValuePair<string, byte>[] array =
            {
                new KeyValuePair<string, byte>("ok", 0),
                new KeyValuePair<string, byte>("nope", 1),
                new KeyValuePair<string, byte>("random", 2),
                new KeyValuePair<string, byte>("", 4)
            };

            var dict = new BindableDictionary<string, byte>(array);

            Assert.Multiple(() =>
            {
                foreach (var (key, value) in array)
                {
                    Assert.That(dict.TryGetValue(key, out var val), Is.True);
                    Assert.That(val, Is.EqualTo(value));
                }

                Assert.AreEqual(array.Length, dict.Count);
            });
        }

        #endregion

        #region BindTarget

        /// <summary>
        /// Tests binding via the various <see cref="BindableDictionary{TKey,TValue}"/> methods.
        /// </summary>
        [Test]
        public void TestBindViaBindTarget()
        {
            BindableDictionary<string, byte> parentBindable = new BindableDictionary<string, byte>();

            BindableDictionary<string, byte> bindable1 = new BindableDictionary<string, byte>();
            BindableDictionary<string, byte> bindable2 = new BindableDictionary<string, byte>();

            bindable1.BindTarget = parentBindable;
            bindable2.BindTarget = parentBindable;

            parentBindable.Add("5", 1);

            Assert.That(bindable1["5"], Is.EqualTo(1));
            Assert.That(bindable2["5"], Is.EqualTo(1));
        }

        #endregion

        #region BindCollectionChanged

        [Test]
        public void TestBindCollectionChangedWithoutRunningImmediately()
        {
            var list = new BindableDictionary<string, byte> { { "a", 1 } };

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.BindCollectionChanged((_, args) => triggeredArgs = args);

            Assert.That(triggeredArgs, Is.Null);
        }

        [Test]
        public void TestBindCollectionChangedWithRunImmediately()
        {
            var list = new BindableDictionary<string, byte> { { "a", 1 } };

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.BindCollectionChanged((_, args) => triggeredArgs = args, true);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(list));
        }

        [Test]
        public void TestBindCollectionChangedNotRunIfBoundToSequenceEqualList()
        {
            var list = new BindableDictionary<string, byte>
            {
                { "a", 1 },
                { "b", 3 },
                { "c", 5 },
                { "d", 7 }
            };

            var otherList = new BindableDictionary<string, byte>
            {
                { "a", 1 },
                { "b", 3 },
                { "c", 5 },
                { "d", 7 }
            };

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.BindCollectionChanged((_, args) => triggeredArgs = args);
            list.BindTo(otherList);

            Assert.That(triggeredArgs, Is.Null);
        }

        [Test]
        public void TestBindCollectionChangedNotRunIfParsingSequenceEqualEnumerable()
        {
            var list = new BindableDictionary<string, byte>
            {
                { "a", 1 },
                { "b", 3 },
                { "c", 5 },
                { "d", 7 }
            };

            var enumerable = new[]
            {
                new KeyValuePair<string, byte>("a", 1),
                new KeyValuePair<string, byte>("b", 3),
                new KeyValuePair<string, byte>("c", 5),
                new KeyValuePair<string, byte>("d", 7)
            };

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.BindCollectionChanged((_, args) => triggeredArgs = args);
            list.Parse(enumerable);

            Assert.That(triggeredArgs, Is.Null);
        }

        [Test]
        public void TestBindCollectionChangedEventsRanIfBoundToDifferentList()
        {
            var firstListContents = new[]
            {
                new KeyValuePair<string, byte>("a", 1),
                new KeyValuePair<string, byte>("b", 3),
                new KeyValuePair<string, byte>("c", 5),
                new KeyValuePair<string, byte>("d", 7),
            };

            var otherListContents = new[]
            {
                new KeyValuePair<string, byte>("a", 2),
                new KeyValuePair<string, byte>("b", 4),
                new KeyValuePair<string, byte>("c", 6),
                new KeyValuePair<string, byte>("d", 8),
                new KeyValuePair<string, byte>("e", 10),
            };

            var list = new BindableDictionary<string, byte>(firstListContents);
            var otherList = new BindableDictionary<string, byte>(otherListContents);

            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            list.BindCollectionChanged((_, args) => triggeredArgs.Add(args));
            list.BindTo(otherList);

            Assert.That(triggeredArgs, Has.Count.EqualTo(2));

            var removeEvent = triggeredArgs.SingleOrDefault(ev => ev.Action == NotifyCollectionChangedAction.Remove);
            Assert.That(removeEvent, Is.Not.Null);
            Assert.That(removeEvent.OldItems, Is.EquivalentTo(firstListContents));

            var addEvent = triggeredArgs.SingleOrDefault(ev => ev.Action == NotifyCollectionChangedAction.Add);
            Assert.That(addEvent, Is.Not.Null);
            Assert.That(addEvent.NewItems, Is.EquivalentTo(otherList));
        }

        #endregion

        #region dictionary[index]

        [Test]
        public void TestGetRetrievesObjectAtIndex()
        {
            bindableStringByteDictionary.Add("0", 0);
            bindableStringByteDictionary.Add("1", 1);
            bindableStringByteDictionary.Add("2", 2);

            Assert.AreEqual(1, bindableStringByteDictionary["1"]);
        }

        [Test]
        public void TestSetMutatesObjectAtIndex()
        {
            bindableStringByteDictionary.Add("0", 0);
            bindableStringByteDictionary.Add("1", 1);
            bindableStringByteDictionary["1"] = 2;

            Assert.AreEqual(2, bindableStringByteDictionary["1"]);
        }

        [Test]
        public void TestGetWhileDisabledDoesNotThrowInvalidOperationException()
        {
            bindableStringByteDictionary.Add("0", 0);
            bindableStringByteDictionary.Disabled = true;

            Assert.AreEqual(0, bindableStringByteDictionary["0"]);
        }

        [Test]
        public void TestSetWhileDisabledThrowsInvalidOperationException()
        {
            bindableStringByteDictionary.Add("0", 0);
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringByteDictionary["0"] = 1);
        }

        [Test]
        public void TestSetNotifiesSubscribers()
        {
            bindableStringByteDictionary.Add("0", 0);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary["0"] = 1;

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new KeyValuePair<string, byte>("0", 0).Yield()));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(new KeyValuePair<string, byte>("0", 1).Yield()));
        }

        [Test]
        public void TestSetNotifiesBoundLists()
        {
            bindableStringByteDictionary.Add("0", 0);

            var list = new BindableDictionary<string, byte>();
            list.BindTo(bindableStringByteDictionary);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary["0"] = 1;

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new KeyValuePair<string, byte>("0", 0).Yield()));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(new KeyValuePair<string, byte>("0", 1).Yield()));
        }

        #endregion

        #region .Add(key, value)

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringAddsStringToEnumerator(string key, byte value)
        {
            bindableStringByteDictionary.Add(key, value);

            Assert.Contains(new KeyValuePair<string, byte>(key, value), bindableStringByteDictionary);
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesSubscriber(string key, byte value)
        {
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Add(key, value);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(new KeyValuePair<string, byte>(key, value).Yield()));
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesSubscriberOnce(string key, byte value)
        {
            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringByteDictionary.Add(key, value);

            Assert.That(triggeredArgs, Has.Count.EqualTo(1));
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesMultipleSubscribers(string key, byte value)
        {
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringByteDictionary.Add(key, value);

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesMultipleSubscribersOnlyAfterTheAdd(string key, byte value)
        {
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsC = args;

            Assert.That(triggeredArgsA, Is.Null);
            Assert.That(triggeredArgsB, Is.Null);
            Assert.That(triggeredArgsC, Is.Null);

            bindableStringByteDictionary.Add(key, value);
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesBoundList(string key, byte value)
        {
            var list = new BindableDictionary<string, byte>();
            list.BindTo(bindableStringByteDictionary);

            bindableStringByteDictionary.Add(key, value);

            Assert.That(list, Contains.Key(key));
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithStringNotifiesBoundLists(string key, byte value)
        {
            var listA = new BindableDictionary<string, byte>();
            var listB = new BindableDictionary<string, byte>();
            var listC = new BindableDictionary<string, byte>();
            listA.BindTo(bindableStringByteDictionary);
            listB.BindTo(bindableStringByteDictionary);
            listC.BindTo(bindableStringByteDictionary);

            bindableStringByteDictionary.Add(key, value);

            Assert.That(listA, Contains.Key(key));
            Assert.That(listB, Contains.Key(key));
            Assert.That(listC, Contains.Key(key));
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithDisabledListThrowsInvalidOperationException(string key, byte value)
        {
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringByteDictionary.Add(key, value); });
        }

        [TestCase("a random string", 0)]
        [TestCase("", 1, Description = "Empty string")]
        public void TestAddWithListContainingItemsDoesNotOverrideItems(string key, byte value)
        {
            const string item = "existing string";
            bindableStringByteDictionary.Add(item, 0);

            bindableStringByteDictionary.Add(key, value);

            Assert.That(bindableStringByteDictionary, Contains.Key(item));
            Assert.That(bindableStringByteDictionary[item], Is.EqualTo(0));
        }

        #endregion

        #region .Remove(key)

        [Test]
        public void TestRemoveWithDisabledListThrowsInvalidOperationException()
        {
            const string item = "hi";
            bindableStringByteDictionary.Add(item, 0);
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringByteDictionary.Remove(item));
        }

        [Test]
        public void TestRemoveWithAnItemThatIsNotInTheListReturnsFalse()
        {
            bool gotRemoved = bindableStringByteDictionary.Remove("hm");

            Assert.IsFalse(gotRemoved);
        }

        [Test]
        public void TestRemoveWhenListIsDisabledThrowsInvalidOperationException()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringByteDictionary.Remove(item); });
        }

        [Test]
        public void TestRemoveWithAnItemThatIsInTheListReturnsTrue()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);

            bool gotRemoved = bindableStringByteDictionary.Remove(item);

            Assert.IsTrue(gotRemoved);
        }

        [Test]
        public void TestRemoveNotifiesSubscriber()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Remove(item);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new KeyValuePair<string, byte>("item", 0).Yield()));
        }

        [Test]
        public void TestRemoveDoesntNotifySubscribersOnNoOp()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);

            NotifyCollectionChangedEventArgs triggeredArgs = null;

            bindableStringByteDictionary.Remove(item);

            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Remove(item);

            Assert.That(triggeredArgs, Is.Null);
        }

        [Test]
        public void TestRemoveNotifiesSubscribers()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);

            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringByteDictionary.Remove(item);

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [Test]
        public void TestRemoveNotifiesBoundList()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);
            var list = new BindableDictionary<string, byte>();
            list.BindTo(bindableStringByteDictionary);

            bindableStringByteDictionary.Remove(item);

            Assert.IsEmpty(list);
        }

        [Test]
        public void TestRemoveNotifiesBoundLists()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);
            var listA = new BindableDictionary<string, byte>();
            listA.BindTo(bindableStringByteDictionary);
            var listB = new BindableDictionary<string, byte>();
            listB.BindTo(bindableStringByteDictionary);
            var listC = new BindableDictionary<string, byte>();
            listC.BindTo(bindableStringByteDictionary);

            bindableStringByteDictionary.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.False(listA.ContainsKey(item));
                Assert.False(listB.ContainsKey(item));
                Assert.False(listC.ContainsKey(item));
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscription()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);
            var list = new BindableDictionary<string, byte>();
            list.BindTo(bindableStringByteDictionary);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Remove(item);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new KeyValuePair<string, byte>(item, 0).Yield()));
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscriptions()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);
            var listA = new BindableDictionary<string, byte>();
            listA.BindTo(bindableStringByteDictionary);

            NotifyCollectionChangedEventArgs triggeredArgsA1 = null;
            NotifyCollectionChangedEventArgs triggeredArgsA2 = null;
            listA.CollectionChanged += (_, args) => triggeredArgsA1 = args;
            listA.CollectionChanged += (_, args) => triggeredArgsA2 = args;

            var listB = new BindableDictionary<string, byte>();
            listB.BindTo(bindableStringByteDictionary);

            NotifyCollectionChangedEventArgs triggeredArgsB1 = null;
            NotifyCollectionChangedEventArgs triggeredArgsB2 = null;
            listB.CollectionChanged += (_, args) => triggeredArgsB1 = args;
            listB.CollectionChanged += (_, args) => triggeredArgsB2 = args;

            bindableStringByteDictionary.Remove(item);

            Assert.That(triggeredArgsA1, Is.Not.Null);
            Assert.That(triggeredArgsA2, Is.Not.Null);
            Assert.That(triggeredArgsB1, Is.Not.Null);
            Assert.That(triggeredArgsB2, Is.Not.Null);
        }

        [Test]
        public void TestRemoveDoesNotNotifySubscribersBeforeItemIsRemoved()
        {
            const string item = "item";
            bindableStringByteDictionary.Add(item, 0);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            Assert.That(triggeredArgs, Is.Null);
        }

        #endregion

        #region .Clear()

        [Test]
        public void TestClear()
        {
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"test{i}", i);

            bindableStringByteDictionary.Clear();

            Assert.IsEmpty(bindableStringByteDictionary);
        }

        [Test]
        public void TestClearWithDisabledListThrowsInvalidOperationException()
        {
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"test{i}", i);
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringByteDictionary.Clear());
        }

        [Test]
        public void TestClearWithEmptyDisabledListThrowsInvalidOperationException()
        {
            bindableStringByteDictionary.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringByteDictionary.Clear());
        }

        [Test]
        public void TestClearUpdatesCountProperty()
        {
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"test{i}", i);

            bindableStringByteDictionary.Clear();

            Assert.AreEqual(0, bindableStringByteDictionary.Count);
        }

        [Test]
        public void TestClearNotifiesSubscriber()
        {
            var items = new[]
            {
                new KeyValuePair<string, byte>("test0", 0),
                new KeyValuePair<string, byte>("test1", 1),
                new KeyValuePair<string, byte>("test2", 2),
                new KeyValuePair<string, byte>("test3", 3),
                new KeyValuePair<string, byte>("test4", 4)
            };

            foreach (var (key, value) in items)
                bindableStringByteDictionary.Add(key, value);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Clear();

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(items));
        }

        [Test]
        public void TestClearDoesNotNotifySubscriberBeforeClear()
        {
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"test{i}", i);

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs = args;

            Assert.That(triggeredArgs, Is.Null);

            bindableStringByteDictionary.Clear();
        }

        [Test]
        public void TestClearNotifiesSubscribers()
        {
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"test{i}", i);

            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringByteDictionary.Clear();

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [Test]
        public void TestClearNotifiesBoundBindable()
        {
            var bindableList = new BindableDictionary<string, byte>();
            bindableList.BindTo(bindableStringByteDictionary);
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"testA{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableList.Add($"testB{i}", i);

            bindableStringByteDictionary.Clear();

            Assert.IsEmpty(bindableList);
        }

        [Test]
        public void TestClearNotifiesBoundBindables()
        {
            var bindableListA = new BindableDictionary<string, byte>();
            bindableListA.BindTo(bindableStringByteDictionary);
            var bindableListB = new BindableDictionary<string, byte>();
            bindableListB.BindTo(bindableStringByteDictionary);
            var bindableListC = new BindableDictionary<string, byte>();
            bindableListC.BindTo(bindableStringByteDictionary);
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"testA{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListA.Add($"testB{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListB.Add($"testC{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListC.Add($"testD{i}", i);

            bindableStringByteDictionary.Clear();

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
            var bindableListA = new BindableDictionary<string, byte>();
            bindableListA.BindTo(bindableStringByteDictionary);
            var bindableListB = new BindableDictionary<string, byte>();
            bindableListB.BindTo(bindableStringByteDictionary);
            var bindableListC = new BindableDictionary<string, byte>();
            bindableListC.BindTo(bindableStringByteDictionary);
            for (byte i = 0; i < 5; i++)
                bindableStringByteDictionary.Add($"testA{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListA.Add($"testB{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListB.Add($"testC{i}", i);
            for (byte i = 0; i < 5; i++)
                bindableListC.Add($"testD{i}", i);

            Assert.Multiple(() =>
            {
                Assert.IsNotEmpty(bindableListA);
                Assert.IsNotEmpty(bindableListB);
                Assert.IsNotEmpty(bindableListC);
            });

            bindableStringByteDictionary.Clear();
        }

        #endregion

        #region .Disabled

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscriber()
        {
            bool? isDisabled = null;
            bindableStringByteDictionary.DisabledChanged += b => isDisabled = b;

            bindableStringByteDictionary.Disabled = true;

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
            bindableStringByteDictionary.DisabledChanged += b => isDisabledA = b;
            bindableStringByteDictionary.DisabledChanged += b => isDisabledB = b;
            bindableStringByteDictionary.DisabledChanged += b => isDisabledC = b;

            bindableStringByteDictionary.Disabled = true;

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
            bindableStringByteDictionary.DisabledChanged += b => Assert.Fail();

            bindableStringByteDictionary.Disabled = bindableStringByteDictionary.Disabled;
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscribers()
        {
            bindableStringByteDictionary.DisabledChanged += b => Assert.Fail();
            bindableStringByteDictionary.DisabledChanged += b => Assert.Fail();
            bindableStringByteDictionary.DisabledChanged += b => Assert.Fail();

            bindableStringByteDictionary.Disabled = bindableStringByteDictionary.Disabled;
        }

        [Test]
        public void TestDisabledNotifiesBoundLists()
        {
            var list = new BindableDictionary<string, byte>();
            list.BindTo(bindableStringByteDictionary);

            bindableStringByteDictionary.Disabled = true;

            Assert.IsTrue(list.Disabled);
        }

        #endregion

        #region .GetEnumberator()

        [Test]
        public void TestGetEnumeratorDoesNotReturnNull()
        {
            Assert.NotNull(bindableStringByteDictionary.GetEnumerator());
        }

        [Test]
        public void TestGetEnumeratorWhenCopyConstructorIsUsedDoesNotReturnTheEnumeratorOfTheInputtedEnumerator()
        {
            var array = new[] { new KeyValuePair<string, byte>("", 0) };

            var list = new BindableDictionary<string, byte>(array);

            var enumerator = list.GetEnumerator();

            Assert.AreNotEqual(array.GetEnumerator(), enumerator);
        }

        #endregion

        #region .Description

        [Test]
        public void TestDescriptionWhenSetReturnsSetValue()
        {
            const string description = "The list used for testing.";

            bindableStringByteDictionary.Description = description;

            Assert.AreEqual(description, bindableStringByteDictionary.Description);
        }

        #endregion

        #region .Parse(obj)

        [Test]
        public void TestParseWithNullClearsList()
        {
            bindableStringByteDictionary.Add("a item", 0);

            bindableStringByteDictionary.Parse(null);

            Assert.IsEmpty(bindableStringByteDictionary);
        }

        [Test]
        public void TestParseWithArray()
        {
            var array = new[]
            {
                new KeyValuePair<string, byte>("testA", 0),
                new KeyValuePair<string, byte>("testB", 1),
            };

            bindableStringByteDictionary.Parse(array);

            CollectionAssert.AreEquivalent(array, bindableStringByteDictionary);
        }

        [Test]
        public void TestParseWithDisabledListThrowsInvalidOperationException()
        {
            bindableStringByteDictionary.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringByteDictionary.Parse(null));
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringByteDictionary.Parse(new[]
                {
                    new KeyValuePair<string, byte>("test", 0),
                    new KeyValuePair<string, byte>("testabc", 1),
                    new KeyValuePair<string, byte>("asdasdasdasd", 1),
                }));
            });
        }

        [Test]
        public void TestParseWithInvalidArgumentTypesThrowsArgumentException()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(""));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(new object()));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(1.1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(1.1f));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse("test123"));
                Assert.Throws(typeof(ArgumentException), () => bindableStringByteDictionary.Parse(29387L));
            });
        }

        [Test]
        public void TestParseWithNullNotifiesClearSubscribers()
        {
            var array = new[]
            {
                new KeyValuePair<string, byte>("testA", 0),
                new KeyValuePair<string, byte>("testB", 1),
                new KeyValuePair<string, byte>("testC", 2),
            };

            foreach (var (key, value) in array)
                bindableStringByteDictionary.Add(key, value);

            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringByteDictionary.Parse(null);

            Assert.That(triggeredArgs, Has.Count.EqualTo(1));
            Assert.That(triggeredArgs.First().Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.First().OldItems, Is.EquivalentTo(array));
        }

        [Test]
        public void TestParseWithItemsNotifiesAddRangeAndClearSubscribers()
        {
            bindableStringByteDictionary.Add("test123", 0);

            var array = new[]
            {
                new KeyValuePair<string, byte>("testA", 0),
                new KeyValuePair<string, byte>("testB", 1),
            };

            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringByteDictionary.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringByteDictionary.Parse(array);

            Assert.That(triggeredArgs, Has.Count.EqualTo(2));
            Assert.That(triggeredArgs.First().Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.First().OldItems, Is.EquivalentTo(new KeyValuePair<string, byte>("test123", 0).Yield()));
            Assert.That(triggeredArgs.ElementAt(1).Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.ElementAt(1).NewItems, Is.EquivalentTo(array));
        }

        #endregion

        #region GetBoundCopy()

        [Test]
        public void TestBoundCopyWithAdd()
        {
            var boundCopy = bindableStringByteDictionary.GetBoundCopy();

            NotifyCollectionChangedEventArgs triggeredArgs = null;
            boundCopy.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringByteDictionary.Add("test", 0);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(new KeyValuePair<string, byte>("test", 0).Yield()));
        }

        #endregion
    }
}
