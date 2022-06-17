// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using JetBrains.Annotations;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing.Dependencies;

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
            Assert.Throws<ArgumentException>(() => new DependencyContainer().Cache(new BaseStructObject()));
        }

        /// <summary>
        /// Special case because "where T : class" also allows interfaces.
        /// </summary>
        [Test]
        public void TestAttemptCacheAsStruct()
        {
            Assert.Throws<ArgumentException>(() => new DependencyContainer().CacheAs<IBaseInterface>(new BaseStructObject()));
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

            Assert.DoesNotThrow(() => dependencies.CacheValue(token));

            var retrieved = dependencies.GetValue<CancellationToken>();

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

        [TestCase(null)]
        [TestCase(10)]
        public void TestResolveNullableInternal(int? testValue)
        {
            var receiver = new Receiver11();

            var testObject = new CachedNullableProvider();
            testObject.SetValue(testValue);

            var dependencies = DependencyActivator.MergeDependencies(testObject, new DependencyContainer());

            dependencies.Inject(receiver);

            Assert.AreEqual(testValue, receiver.TestObject);
        }

        [Test]
        public void TestCacheNullInternal()
        {
            Assert.DoesNotThrow(() => new DependencyContainer().CacheValue(null));
            Assert.DoesNotThrow(() => new DependencyContainer().CacheValueAs<object>(null));
        }

        [Test]
        public void TestResolveStructWithoutNullPermits()
        {
            Assert.Throws<DependencyNotRegisteredException>(() => new DependencyContainer().Inject(new Receiver12()));
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
            dependencies.CacheValueAs(testObject);

            Assert.AreEqual(testObject, dependencies.GetValue<int>());
            Assert.AreEqual(testObject, dependencies.GetValue<int?>());
        }

        [Test]
        public void TestCacheWithDependencyInfo()
        {
            var cases = new[]
            {
                default,
                new CacheInfo("name"),
                new CacheInfo(parent: typeof(object)),
                new CacheInfo("name", typeof(object))
            };

            var dependencies = new DependencyContainer();

            for (int i = 0; i < cases.Length; i++)
                dependencies.CacheValueAs(i, cases[i]);

            Assert.Multiple(() =>
            {
                for (int i = 0; i < cases.Length; i++)
                    Assert.AreEqual(i, dependencies.GetValue<int>(cases[i]));
            });
        }

        [Test]
        public void TestDependenciesOverrideParent()
        {
            var cases = new[]
            {
                default,
                new CacheInfo("name"),
                new CacheInfo(parent: typeof(object)),
                new CacheInfo("name", typeof(object))
            };

            var dependencies = new DependencyContainer();

            for (int i = 0; i < cases.Length; i++)
                dependencies.CacheValueAs(i, cases[i]);

            dependencies = new DependencyContainer(dependencies);

            for (int i = 0; i < cases.Length; i++)
                dependencies.CacheValueAs(cases.Length + i, cases[i]);

            Assert.Multiple(() =>
            {
                for (int i = 0; i < cases.Length; i++)
                    Assert.AreEqual(cases.Length + i, dependencies.GetValue<int>(cases[i]));
            });
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

        private class Receiver6
        {
            [BackgroundDependencyLoader]
            public void Load()
            {
            }
        }

        private class Receiver7
        {
            [BackgroundDependencyLoader]
            protected void Load()
            {
            }
        }

        private class Receiver8
        {
            [BackgroundDependencyLoader]
            internal void Load()
            {
            }
        }

        private class Receiver9
        {
            [BackgroundDependencyLoader]
            protected internal void Load()
            {
            }
        }

        private class Receiver10
        {
            public CachedStructProvider.Struct TestObject { get; private set; }

            [BackgroundDependencyLoader]
            private void load(CachedStructProvider.Struct testObject) => TestObject = testObject;
        }

        private class Receiver11
        {
            public int? TestObject { get; private set; }

            [BackgroundDependencyLoader]
            private void load(int? testObject) => TestObject = testObject;
        }

        private class Receiver12
        {
            [UsedImplicitly] // param used implicitly
            [BackgroundDependencyLoader]
            private void load(int testObject)
            {
            }
        }

        private class Receiver13
        {
            public int? TestObject { get; private set; } = 1;

            [BackgroundDependencyLoader(true)]
            private void load(int testObject) => TestObject = testObject;
        }

#nullable enable
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private class Receiver14
        {
            [BackgroundDependencyLoader]
            private void load(BaseObject nonNullObject, DerivedObject? nullableObject)
            {
            }
        }
#nullable disable
    }
}
