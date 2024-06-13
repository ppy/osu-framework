// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing.Dependencies;

namespace osu.Framework.Tests.Dependencies.Reflection
{
    [TestFixture]
    [SuppressMessage("Performance", "OFSG001:Class contributes to dependency injection and should be partial")]
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

        [Test]
        public void TestCacheStruct()
        {
            var dependencies = new DependencyContainer();
            dependencies.Cache(new BaseStructObject());

            Assert.IsNotNull(dependencies.Get<BaseStructObject?>());
        }

        [Test]
        public void TestCacheAsStruct()
        {
            var dependencies = new DependencyContainer();
            dependencies.CacheAs<IBaseInterface>(new BaseStructObject());

            Assert.IsNotNull(dependencies.Get<IBaseInterface>());
        }

        /// <summary>
        /// Special value type that remains internally consistent through copies.
        /// </summary>
        [Test]
        public void TestCacheCancellationToken()
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var dependencies = new DependencyContainer();

            Assert.DoesNotThrow(() => dependencies.Cache(token));

            var retrieved = dependencies.Get<CancellationToken>();

            source.Cancel();

            Assert.IsTrue(token.IsCancellationRequested);
            Assert.IsTrue(retrieved.IsCancellationRequested);
        }

        [Test]
        public void TestInvalidPublicAccessor()
        {
            var receiver = new Receiver6();

            Assert.Throws<AccessModifierNotAllowedForLoaderMethodException>(() => new DependencyContainer().Inject(receiver));
        }

        [Test]
        public void TestInvalidProtectedAccessor()
        {
            var receiver = new Receiver7();

            Assert.Throws<AccessModifierNotAllowedForLoaderMethodException>(() => new DependencyContainer().Inject(receiver));
        }

        [Test]
        public void TestInvalidInternalAccessor()
        {
            var receiver = new Receiver8();

            Assert.Throws<AccessModifierNotAllowedForLoaderMethodException>(() => new DependencyContainer().Inject(receiver));
        }

        [Test]
        public void TestInvalidProtectedInternalAccessor()
        {
            var receiver = new Receiver9();

            Assert.Throws<AccessModifierNotAllowedForLoaderMethodException>(() => new DependencyContainer().Inject(receiver));
        }

        [Test]
        public void TestReceiveStructInternal()
        {
            var receiver = new Receiver10();

            var testObject = new CachedStructProvider();

            var dependencies = DependencyActivator.MergeDependencies(testObject, new DependencyContainer());

            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
            Assert.AreEqual(testObject.CachedObject.Value, receiver.TestObject.Value);
        }

        [Test]
        public void TestAttemptCacheNullInternal()
        {
            Assert.Throws<ArgumentNullException>(() => new DependencyContainer().Cache(null!));
            Assert.Throws<ArgumentNullException>(() => new DependencyContainer().CacheAs<object>(null!));
        }

        [Test]
        public void TestResolveStructWithoutNullPermits()
        {
            var receiver = new Receiver12();

            Assert.DoesNotThrow(() => new DependencyContainer().Inject(receiver));
            Assert.AreEqual(0, receiver.TestObject);
        }

        [Test]
        public void TestResolveStructWithNullPermits()
        {
            var receiver = new Receiver13();

            Assert.DoesNotThrow(() => new DependencyContainer().Inject(receiver));
            Assert.AreEqual(0, receiver.TestObject);
        }

        [Test]
        public void TestCacheAsNullableInternal()
        {
            int? testObject = 5;

            var dependencies = new DependencyContainer();
            dependencies.CacheAs(testObject);

            Assert.AreEqual(testObject, dependencies.Get<int>());
            Assert.AreEqual(testObject, dependencies.Get<int?>());
        }

        [TestCase(null, null)]
        [TestCase("name", null)]
        [TestCase(null, typeof(object))]
        [TestCase("name", typeof(object))]
        public void TestCacheWithDependencyInfo(string name, Type parent)
        {
            CacheInfo info = new CacheInfo(name, parent);

            var dependencies = new DependencyContainer();
            dependencies.CacheAs(1, info);

            Assert.AreEqual(1, dependencies.Get<int>(info));
        }

        [TestCase(null, null)]
        [TestCase("name", null)]
        [TestCase(null, typeof(object))]
        [TestCase("name", typeof(object))]
        public void TestDependenciesOverrideParent(string name, Type parent)
        {
            CacheInfo info = new CacheInfo(name, parent);

            var dependencies = new DependencyContainer();
            dependencies.CacheAs(1, info);

            dependencies = new DependencyContainer(dependencies);
            dependencies.CacheAs(2, info);

            Assert.Multiple(() => { Assert.AreEqual(2, dependencies.Get<int>(info)); });
        }

        [Test]
        public void TestResolveWithNullableReferenceTypes()
        {
            var dependencies = new DependencyContainer();
            var receiver = new Receiver14();

            // Throws with missing non-nullable dependency.
            Assert.Throws<DependencyNotRegisteredException>(() => dependencies.Inject(receiver));

            // Cache the non-nullable dependency.
            dependencies.CacheAs(new BaseObject());
            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
        }

        [Test]
        public void TestResolveDefaultStruct()
        {
            Assert.That(new DependencyContainer().Get<CancellationToken>(), Is.EqualTo(default(CancellationToken)));
        }

        [Test]
        public void TestResolveNullStruct()
        {
            Assert.That(new DependencyContainer().Get<CancellationToken?>(), Is.Null);
        }

        [Test]
        public void TestModifyBoxedStruct()
        {
            var dependencies = new DependencyContainer();
            dependencies.CacheAs<IBaseInterface>(new BaseStructObject { TestValue = 1 });
            dependencies.Get<IBaseInterface>().TestValue = 2;

            Assert.That(dependencies.Get<IBaseInterface>().TestValue, Is.EqualTo(2));
        }

        private interface IBaseInterface
        {
            int TestValue { get; set; }
        }

        private class BaseObject
        {
            public int TestValue;
        }

        private struct BaseStructObject : IBaseInterface
        {
            public int TestValue { get; set; }
        }

        private class DerivedObject : BaseObject
        {
        }

        private class Receiver1 : IDependencyInjectionCandidate
        {
            public Action<BaseObject, DerivedObject> OnLoad;

            [BackgroundDependencyLoader]
            private void load(BaseObject baseObject, DerivedObject derivedObject) => OnLoad?.Invoke(baseObject, derivedObject);
        }

        private class Receiver2 : IDependencyInjectionCandidate
        {
        }

        private class Receiver3 : IDependencyInjectionCandidate
        {
            public Action<BaseObject> OnLoad;

            [BackgroundDependencyLoader(true)]
            private void load(BaseObject baseObject) => OnLoad?.Invoke(baseObject);
        }

        private class Receiver4 : IDependencyInjectionCandidate
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

        private class Receiver6 : IDependencyInjectionCandidate
        {
            [BackgroundDependencyLoader]
            public void Load()
            {
            }
        }

        private class Receiver7 : IDependencyInjectionCandidate
        {
            [BackgroundDependencyLoader]
            protected void Load()
            {
            }
        }

        private class Receiver8 : IDependencyInjectionCandidate
        {
            [BackgroundDependencyLoader]
            internal void Load()
            {
            }
        }

        private class Receiver9 : IDependencyInjectionCandidate
        {
            [BackgroundDependencyLoader]
            protected internal void Load()
            {
            }
        }

        private class Receiver10 : IDependencyInjectionCandidate
        {
            public CachedStructProvider.Struct TestObject { get; private set; }

            [BackgroundDependencyLoader]
            private void load(CachedStructProvider.Struct testObject) => TestObject = testObject;
        }

        private class Receiver12 : IDependencyInjectionCandidate
        {
            public int TestObject { get; private set; } = 1;

            [BackgroundDependencyLoader]
            private void load(int testObject) => TestObject = testObject;
        }

        private class Receiver13 : IDependencyInjectionCandidate
        {
            public int? TestObject { get; private set; } = 1;

            [BackgroundDependencyLoader(true)]
            private void load(int testObject) => TestObject = testObject;
        }

#nullable enable
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private class Receiver14 : IDependencyInjectionCandidate
        {
            [BackgroundDependencyLoader]
            private void load(BaseObject nonNullObject, DerivedObject? nullableObject)
            {
            }
        }
    }
}
