// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Allocation;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class DependencyAttributeTest
    {
        [Test]
        public void TestInjectIntoNothing()
        {
            var dependencies = new DependencyContainer();

            var receiver = new Receiver1();

            dependencies.Inject(receiver);

            Assert.AreEqual(null, receiver.Obj);
        }

        [Test]
        public void TestInjectIntoDependency()
        {
            var testObject = new BaseObject();

            var dependencies = new DependencyContainer();
            dependencies.Cache(testObject);

            var receiver = new Receiver2();

            dependencies.Inject(receiver);

            Assert.AreEqual(testObject, receiver.Obj);
        }

        [Test]
        public void TestInjectNullIntoNonNull()
        {
            var dependencies = new DependencyContainer();

            var receiver = new Receiver2();

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
            var testObject = new BaseObject();

            var dependencies = new DependencyContainer();
            dependencies.Cache(testObject);

            var receiver = new Receiver4();

            dependencies.Inject(receiver);

            Assert.AreEqual(testObject, receiver.Obj);
            Assert.AreEqual(testObject, receiver.Obj2);
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
            [Dependency]
            private BaseObject obj;

            public BaseObject Obj => obj;
        }

        private class Receiver3
        {
            [Dependency(true)]
            private BaseObject obj;

            public BaseObject Obj => obj;
        }

        private class Receiver4 : Receiver2
        {
            [Dependency]
            private BaseObject obj;

            public BaseObject Obj2 => obj;
        }
    }
}
