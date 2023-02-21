// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing.Dependencies;

#pragma warning disable IDE0052 // Unread private member

namespace osu.Framework.Tests.Dependencies.SourceGeneration
{
    [TestFixture]
    public partial class CachedAttributeTest
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

            Assert.IsNull(dependencies.Get<object>());
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
            var provider = new PartialCachedStructProvider();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider.CachedObject.Value, dependencies.GetValue<PartialCachedStructProvider.Struct>().Value);
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
            var provider = new PartialCachedNullableProvider();

            provider.SetValue(testValue);

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(testValue, dependencies.GetValue<int?>());
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestInvalidPublicAccessor()
        {
            var provider = new Provider13();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestInvalidProtectedAccessor()
        {
            var provider = new Provider14();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestInvalidInternalAccessor()
        {
            var provider = new Provider15();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
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

            Assert.Throws<NullDependencyException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCacheProperty()
        {
            var provider = new Provider19();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<object>());
        }

        [Test]
        public void TestCachePropertyWithNoSetter()
        {
            var provider = new Provider20();

            Assert.DoesNotThrow(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestCachePropertyWithPublicSetter()
        {
            var provider = new Provider21();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestCachePropertyWithNonAutoSetter()
        {
            var provider = new Provider22();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        [Ignore("Temporarily not analysed.")]
        public void TestCacheWithNonAutoGetter()
        {
            var provider = new Provider24();

            Assert.Throws<AccessModifierNotAllowedForCachedValueException>(() => DependencyActivator.MergeDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCachedViaInterface()
        {
            var provider = new Provider25();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<IProviderInterface3>());
            Assert.IsNotNull(dependencies.Get<IProviderInterface2>());
        }

        [Test]
        public void TestInheritancePreservesCachingViaBaseType()
        {
            var provider = new Provider26();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
            Assert.IsNull(dependencies.Get<Provider26>());
        }

        [Test]
        public void TestImplementationOfDerivedInterfacePreservesCaching()
        {
            var provider = new Provider27();

            var dependencies = DependencyActivator.MergeDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<IProviderInterface2>());
            Assert.IsNull(dependencies.Get<IProviderInterface4>());
            Assert.IsNull(dependencies.Get<Provider27>());
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
        private partial class Provider1 : IDependencyInjectionCandidate
        {
        }

        [Cached(Type = typeof(object))]
        private partial class Provider2 : IDependencyInjectionCandidate
        {
        }

        [Cached(Type = typeof(Provider1))]
        private partial class Provider3 : Provider1
        {
        }

        private partial class Provider4 : IDependencyInjectionCandidate
        {
            [Cached]
#pragma warning disable 0649
            private int fail;
#pragma warning restore 0649
        }

        private partial class Provider5 : IDependencyInjectionCandidate
        {
            [Cached]
            public ProvidedType1 Provided1 { get; } = new ProvidedType1();

            [Cached]
            private ProvidedType2 provided2 = new ProvidedType2();
        }

        private partial class Provider6 : Provider5
        {
            [Cached]
            public ProvidedType1 Provided3 { get; } = new ProvidedType1();
        }

        private partial class Provider7 : IDependencyInjectionCandidate
        {
            [Cached]
            [Cached(Type = typeof(object))]
            private ProvidedType1 provided1 = new ProvidedType1();
        }

        [Cached]
        [Cached(Type = typeof(object))]
        private partial class Provider8 : IDependencyInjectionCandidate
        {
        }

        private partial class Provider9 : IDependencyInjectionCandidate
        {
            [Cached(Type = typeof(ProvidedType1))]
            private object provided1 = new object();
        }

        private partial class Provider10 : IDependencyInjectionCandidate
        {
            [Cached]
            private object provided1 = new ProvidedType1();
        }

        private partial class Provider11 : IDependencyInjectionCandidate
        {
            [Cached]
            [Cached(Type = typeof(IProvidedInterface1))]
            private IProvidedInterface1 provided1 = new ProvidedType1();
        }

        private partial class Provider12 : IDependencyInjectionCandidate
        {
            [Cached(Type = typeof(IProvidedInterface1))]
            private IProvidedInterface1 provided1 = new ProvidedType3();
        }

        private partial class Provider13 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1 = new ProvidedType1();
        }

        private partial class Provider14 : IDependencyInjectionCandidate
        {
            [Cached]
            protected object Provided1 = new ProvidedType1();
        }

        private partial class Provider15 : IDependencyInjectionCandidate
        {
            [Cached]
            internal object Provided1 = new ProvidedType1();
        }

        private partial class Provider16 : IDependencyInjectionCandidate
        {
            [Cached]
            protected internal object Provided1 = new ProvidedType1();
        }

        private partial class Provider17 : IDependencyInjectionCandidate
        {
            [Cached]
            public readonly object Provided1 = new ProvidedType1();
        }

        private partial class Provider18 : IDependencyInjectionCandidate
        {
#pragma warning disable 649
            [Cached]
            public readonly object Provided1;
#pragma warning restore 649
        }

        private partial class Provider19 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1 { get; private set; } = new object();
        }

        private partial class Provider20 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1 { get; } = new object();
        }

        private partial class Provider21 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1 { get; set; }
        }

        private partial class Provider22 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1
            {
                get => null;
                // ReSharper disable once ValueParameterNotUsed
                set
                {
                }
            }
        }

        private partial class Provider24 : IDependencyInjectionCandidate
        {
            [Cached]
            public object Provided1 => null;
        }

        private partial class Provider25 : IProviderInterface3
        {
        }

        private partial class Provider26 : Provider1
        {
        }

        private partial class Provider27 : IProviderInterface4
        {
        }

        [Cached]
        private interface IProviderInterface3 : IProviderInterface2
        {
        }

        [Cached]
        private interface IProviderInterface2 : IDependencyInjectionCandidate
        {
        }

        private interface IProviderInterface4 : IProviderInterface2
        {
        }
    }
}
