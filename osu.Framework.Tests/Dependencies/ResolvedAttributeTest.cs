// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class ResolvedAttributeTest
    {
        [Test]
        public void TestInjectIntoNothing()
        {
            var receiver = new Receiver1();

            createDependencies().Inject(receiver);

            Assert.AreEqual(null, receiver.Obj);
        }

        [Test]
        public void TestInjectIntoDependency()
        {
            var receiver = new Receiver2();

            BaseObject testObject;
            createDependencies(testObject = new BaseObject()).Inject(receiver);

            Assert.AreEqual(testObject, receiver.Obj);
        }

        [Test]
        public void TestInjectNullIntoNonNull()
        {
            var receiver = new Receiver2();

            Assert.Throws<DependencyNotRegisteredException>(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestInjectNullIntoNullable()
        {
            var receiver = new Receiver3();

            Assert.DoesNotThrow(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestInjectIntoSubClasses()
        {
            var receiver = new Receiver4();

            BaseObject testObject;
            createDependencies(testObject = new BaseObject()).Inject(receiver);

            Assert.AreEqual(testObject, receiver.Obj);
            Assert.AreEqual(testObject, receiver.Obj2);
        }

        private DependencyContainer createDependencies(params object[] toCache)
        {
            var dependencies = new DependencyContainer();

            toCache?.ForEach(o => dependencies.Cache(o));

            return dependencies;
        }

        private class BaseObject
        {
        }

        private class Receiver1
        {
#pragma warning disable 649
            private BaseObject obj;
#pragma warning restore 649

            public BaseObject Obj => obj;
        }

        private class Receiver2
        {
            [Resolved]
            private BaseObject obj { get; set; }

            public BaseObject Obj => obj;
        }

        private class Receiver3
        {
            [Resolved(CanBeNull = true)]
            private BaseObject obj { get; set; }
        }

        private class Receiver4 : Receiver2
        {
            [Resolved]
            private BaseObject obj { get; set; }

            public BaseObject Obj2 => obj;
        }

        private class Receiver5
        {
            [Resolved]
            private BaseObject obj { get; set; }

            public BaseObject Obj => obj;
        }
    }
}
