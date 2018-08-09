// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class DependencyContainerTest
    {
        [Test]
        public void TestCacheAsMostDerivedType()
        {
            var baseObject = new BaseObject { TestValue = 1 };
            var derivedObject = new DerivedObject { TestValue = 2 };

            var dependencies = new DependencyContainer();
            dependencies.Cache(baseObject);
            dependencies.Cache(derivedObject);

            BaseObject receivedBase = null;
            BaseObject receivedDerived = null;

            var receiver = new Receiver1
            {
                OnLoad = (b, d) =>
                {
                    receivedBase = b;
                    receivedDerived = d;
                }
            };

            dependencies.Inject(receiver);

            Assert.AreEqual(typeof(BaseObject), receivedBase.GetType());
            Assert.AreEqual(typeof(DerivedObject), receivedDerived.GetType());
            Assert.AreEqual(1, receivedBase.TestValue);
            Assert.AreEqual(2, receivedDerived.TestValue);
        }

        [Test]
        public void TestInjectIntoNothing()
        {
            var baseObject = new BaseObject { TestValue = 1 };

            var dependencies = new DependencyContainer();
            dependencies.Cache(baseObject);

            var receiver = new Receiver2();

            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
        }

        [Test]
        public void TestInjectNullIntoNonNull()
        {
            var dependencies = new DependencyContainer();

            var receiver = new Receiver1();

            Assert.Throws<DependencyNotRegisteredException>(() => dependencies.Inject(receiver));
        }

        [Test]
        public void TestInjectNullIntoNullable()
        {
            var dependencies = new DependencyContainer();

            var receiver = new Receiver3();

            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
        }

        [Test]
        public void TestInjectIntoSubClasses()
        {
            var dependencies = new DependencyContainer();

            int count = 0, baseCount = 0, derivedCount = 0;

            var receiver = new Receiver5
            {
                Loaded4 = () => baseCount = ++count,
                Loaded5 = () => derivedCount = ++count
            };

            dependencies.Inject(receiver);

            Assert.AreEqual(1, baseCount);
            Assert.AreEqual(2, derivedCount);
        }

        [Test]
        public void TestOverrideCache()
        {
            var testObject1 = new BaseObject { TestValue = 1 };
            var testObject2 = new BaseObject { TestValue = 2 };

            var dependencies1 = new DependencyContainer();
            dependencies1.Cache(testObject1);

            var dependencies2 = new DependencyContainer(dependencies1);
            dependencies2.Cache(testObject2);

            BaseObject receivedObject = null;

            var receiver = new Receiver3
            {
                OnLoad = o => receivedObject = o
            };

            dependencies1.Inject(receiver);
            Assert.AreEqual(receivedObject, testObject1);

            dependencies2.Inject(receiver);
            Assert.AreEqual(receivedObject, testObject2);
        }

        [Test]
        public void TestMultipleCacheFails()
        {
            var testObject1 = new BaseObject { TestValue = 1 };
            var testObject2 = new BaseObject { TestValue = 2 };

            var dependencies = new DependencyContainer();
            dependencies.Cache(testObject1);

            Assert.Throws<TypeAlreadyCachedException>(() => dependencies.Cache(testObject2));
        }

        /// <summary>
        /// Special case because "where T : class" also allows interfaces.
        /// </summary>
        [Test]
        public void TestAttemptCacheStruct()
        {
            Assert.Throws<ArgumentException>(() => new DependencyContainer().Cache<IBaseInterface>(new BaseStructObject()));
        }

        /// <summary>
        /// Special case because "where T : class" also allows interfaces.
        /// </summary>
        [Test]
        public void TestAttemptCacheAsStruct()
        {
            Assert.Throws<ArgumentException>(() => new DependencyContainer().CacheAs<IBaseInterface>(new BaseStructObject()));
        }

        private interface IBaseInterface
        {
        }

        private class BaseObject
        {
            public int TestValue;
        }

        private struct BaseStructObject : IBaseInterface
        {
        }

        private class DerivedObject : BaseObject
        {
        }

        private class Receiver1
        {
            public Action<BaseObject, DerivedObject> OnLoad;

            [BackgroundDependencyLoader]
            private void load(BaseObject baseObject, DerivedObject derivedObject) => OnLoad?.Invoke(baseObject, derivedObject);
        }

        private class Receiver2
        {
        }

        private class Receiver3
        {
            public Action<BaseObject> OnLoad;

            [BackgroundDependencyLoader(true)]
            private void load(BaseObject baseObject) => OnLoad?.Invoke(baseObject);
        }

        private class Receiver4
        {
            public Action Loaded4;

            [BackgroundDependencyLoader]
            private void load() => Loaded4?.Invoke();
        }

        private class Receiver5 : Receiver4
        {
            public Action Loaded5;

            [BackgroundDependencyLoader]
            private void load() => Loaded5?.Invoke();
        }
    }
}
