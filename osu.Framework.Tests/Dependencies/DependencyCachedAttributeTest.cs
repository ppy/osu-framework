// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class DependencyCachedAttributeTest
    {
        [Test]
        public void TestCacheType()
        {
            var provider = new Provider1();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
        }

        [Test]
        public void TestCacheTypeAsParentType()
        {
            var provider = new Provider2();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<object>());
        }

        [Test]
        public void TestCacheTypeOverrideParentCache()
        {
            var provider = new Provider3();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
            Assert.AreEqual(null, dependencies.Get<Provider3>());
        }

        [Test]
        public void TestAttemptToCacheStruct()
        {
            var provider = new Provider4();

            Assert.Throws<ArgumentException>(() => DependencyActivator.BuildDependencies(provider, new DependencyContainer()));
        }

        [Test]
        public void TestCacheMultipleFields()
        {
            var provider = new Provider5();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
            Assert.IsNotNull(dependencies.Get<ProvidedType2>());
        }

        [Test]
        public void TestCacheFieldsOverrideBaseFields()
        {
            var provider = new Provider6();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.AreEqual(provider.Provided3, dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheFieldsAsMultipleTypes()
        {
            var provider = new Provider7();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<object>());
            Assert.IsNotNull(dependencies.Get<ProvidedType1>());
        }

        [Test]
        public void TestCacheTypeAsMultipleTypes()
        {
            var provider = new Provider8();

            var dependencies = DependencyActivator.BuildDependencies(provider, new DependencyContainer());

            Assert.IsNotNull(dependencies.Get<object>());
            Assert.IsNotNull(dependencies.Get<Provider8>());
        }

        [Test]
        public void TestAttemptToCacheBaseAsDerived()
        {
            var provider = new Provider9();

            Assert.Throws<ArgumentException>(() => DependencyActivator.BuildDependencies(provider, new DependencyContainer()));
        }

        private class ProvidedType1
        {
        }

        private class ProvidedType2
        {
        }

        [DependencyCached]
        private class Provider1
        {
        }

        [DependencyCached(Type = typeof(object))]
        private class Provider2
        {
        }

        [DependencyCached(Type = typeof(Provider1))]
        private class Provider3 : Provider1
        {
        }

        private class Provider4
        {
            [DependencyCached]
            private int fail;
        }

        private class Provider5
        {
            [DependencyCached]
            private ProvidedType1 provided1 = new ProvidedType1();

            public ProvidedType1 Provided1 => provided1;

            [DependencyCached]
            private ProvidedType2 provided2 = new ProvidedType2();
        }

        private class Provider6 : Provider5
        {
            [DependencyCached]
            private ProvidedType1 provided3 = new ProvidedType1();

            public ProvidedType1 Provided3 => provided3;
        }

        private class Provider7
        {
            [DependencyCached]
            [DependencyCached(Type = typeof(object))]
            private ProvidedType1 provided1 = new ProvidedType1();
        }

        [DependencyCached]
        [DependencyCached(Type = typeof(object))]
        private class Provider8
        {
        }

        private class Provider9
        {
            [DependencyCached(Type = typeof(ProvidedType1))]
            private object provided1 = new object();
        }
    }
}
