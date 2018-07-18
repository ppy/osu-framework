// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
            IReadOnlyDependencyContainer dependencies = new DependencyContainer();

            var provider = new Provider1();

            dependencies = DependencyActivator.BuildDependencies(provider, dependencies);

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
        }

        [Test]
        public void TestCacheTypeAsParentClass()
        {
            IReadOnlyDependencyContainer dependencies = new DependencyContainer();

            var provider = new Provider2();

            dependencies = DependencyActivator.BuildDependencies(provider, dependencies);

            Assert.AreEqual(provider, dependencies.Get<object>());
        }

        [Test]
        public void TestCacheTypeOverrideParent()
        {
            IReadOnlyDependencyContainer dependencies = new DependencyContainer();

            var provider = new Provider3();

            dependencies = DependencyActivator.BuildDependencies(provider, dependencies);

            Assert.AreEqual(provider, dependencies.Get<Provider1>());
            Assert.AreEqual(null, dependencies.Get<Provider3>());
        }

        [DependencyCached]
        private class Provider1
        {
        }

        [DependencyCached(typeof(object))]
        private class Provider2
        {
        }

        [DependencyCached(typeof(Provider1))]
        private class Provider3 : Provider1
        {
        }
    }
}
