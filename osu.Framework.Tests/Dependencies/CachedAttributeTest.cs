// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing.Dependencies;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class CachedAttributeTest
    {
        [Test]
        public void TestCacheType()
        {
            var provider = new Provider1();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
        }

        [Test]
        public void TestCacheTypeAsParentType()
        {
            var provider = new Provider2();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<object>());
        }

        [Test]
        public void TestCacheTypeOverrideParentCache()
        {
            var provider = new Provider3();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
            Assert.AreEqual(null, dependencies.Get<Provider3>());
        }

        [Test]
        public void TestAttemptToCacheStruct()
        {
            var provider = new Provider4();

            Assert.Throws<ArgumentException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCacheMultipleFields()
        {
            var provider = new Provider5();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
            Assert.IsNotNull(dependencies.Get<ProvidedType2>());
        }

        [Test]
        public void TestCacheFieldsOverrideBaseFields()
        {
            var provider = new Provider6();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider.Provided3, dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheFieldsAsMultipleTypes()
        {
            var provider = new Provider7();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<object>());
            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheTypeAsMultipleTypes()
        {
            var provider = new Provider8();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<object>());
            Assert.IsNotNull(dependencies.Get<Provider8>());
        }

        [Test]
        public void TestAttemptToCacheBaseAsDerived()
        {
            var provider = new Provider9();

            Assert.Throws<ArgumentException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCacheMostDerivedType()
        {
            var provider = new Provider10();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheClassAsInterface()
        {
            var provider = new Provider11();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<IProvidedInterface1>());
            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheStructAsInterface()
        {
            var provider = new Provider12();

            Assert.Throws<ArgumentException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        /// <summary>
        /// Tests caching a struct, where the providing type is within the osu.Framework assembly.
        /// </summary>
        [Test]
        public void TestCacheStructInternal()
        {
            var provider = new CachedStructProvider();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider.CachedObject.Value, dependencies.GetValue<CachedStructProvider.Struct>().Value);
        }

        [Test]
        public void TestGetValueNullInternal()
        {
            Assert.AreEqual(default(int), new DependencyContainer().GetValue<int>());
        }

        /// <summary>
        /// Test caching a nullable, where the providing type is within the osu.Framework assembly.
        /// </summary>
        [TestCase(null)]
        [TestCase(10)]
        public void TestCacheNullableInternal(int? testValue)
        {
            var provider = new CachedNullableProvider();

            provider.SetValue(testValue);

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(testValue, dependencies.GetValue<int?>());
        }

        [Test]
        public void TestInvalidPublicAccessor()
        {
            var provider = new Provider13();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestInvalidProtectedAccessor()
        {
            var provider = new Provider14();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestInvalidInternalAccessor()
        {
            var provider = new Provider15();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestInvalidProtectedInternalAccessor()
        {
            var provider = new Provider16();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestValidPublicAccessor()
        {
            var provider = new Provider17();

            Assert.DoesNotThrow(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCacheNullReferenceValue()
        {
            var provider = new Provider18();

            Assert.Throws<NullReferenceException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        private interface IProvidedInterface1
        {
        }

        private class ProvidedType1 : IProvidedInterface1
        {
        }

        private class ProvidedType2
        {
        }

        private struct ProvidedType3 : IProvidedInterface1
        {
        }

        [Cached]
        private class Provider1
        {
        }

        [Cached(Type = typeof(object))]
        private class Provider2
        {
        }

        [Cached(Type = typeof(Provider1))]
        private class Provider3 : Provider1
        {
        }

        private class Provider4
        {
            [Cached]
#pragma warning disable 169
            private int fail;
#pragma warning restore 169
        }

        private class Provider5
        {
            [Cached]
            private ProvidedType1 provided1 = new ProvidedType1();

            public ProvidedType1 Provided1 => provided1;

            [Cached]
            private ProvidedType2 provided2 = new ProvidedType2();
        }

        private class Provider6 : Provider5
        {
            [Cached]
            private ProvidedType1 provided3 = new ProvidedType1();

            public ProvidedType1 Provided3 => provided3;
        }

        private class Provider7
        {
            [Cached]
            [Cached(Type = typeof(object))]
            private ProvidedType1 provided1 = new ProvidedType1();
        }

        [Cached]
        [Cached(Type = typeof(object))]
        private class Provider8
        {
        }

        private class Provider9
        {
            [Cached(Type = typeof(ProvidedType1))]
            private object provided1 = new object();
        }

        private class Provider10
        {
            [Cached]
            private object provided1 = new ProvidedType1();
        }

        private class Provider11
        {
            [Cached]
            [Cached(Type = typeof(IProvidedInterface1))]
            private IProvidedInterface1 provided1 = new ProvidedType1();
        }

        private class Provider12
        {
            [Cached(Type = typeof(IProvidedInterface1))]
            private IProvidedInterface1 provided1 = new ProvidedType3();
        }

        private class Provider13
        {
            [Cached]
            public object Provided1 = new ProvidedType1();
        }

        private class Provider14
        {
            [Cached]
            protected object Provided1 = new ProvidedType1();
        }

        private class Provider15
        {
            [Cached]
            internal object Provided1 = new ProvidedType1();
        }

        private class Provider16
        {
            [Cached]
            protected internal object Provided1 = new ProvidedType1();
        }

        private class Provider17
        {
            [Cached]
            public readonly object Provided1 = new ProvidedType1();
        }

        private class Provider18
        {
#pragma warning disable 649
            [Cached]
            public readonly object Provided1;
#pragma warning restore 649
        }
    }
}
