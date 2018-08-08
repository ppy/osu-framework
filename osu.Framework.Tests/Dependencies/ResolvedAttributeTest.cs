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

        [Test]
        public void TestInvalidPublicAccessor()
        {
            var receiver = new Receiver5();

            Assert.Throws<AccessModifierNotAllowedForPropertySetterException>(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestInvalidExplicitProtectedAccessor()
        {
            var receiver = new Receiver6();

            Assert.Throws<AccessModifierNotAllowedForPropertySetterException>(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestInvalidExplicitPrivateAccessor()
        {
            var receiver = new Receiver7();

            Assert.Throws<AccessModifierNotAllowedForPropertySetterException>(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestExplicitPrivateAccessor()
        {
            var receiver = new Receiver8();

            Assert.DoesNotThrow(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestExplicitInvalidProtectedInternalAccessor()
        {
            var receiver = new Receiver9();

            Assert.Throws<AccessModifierNotAllowedForPropertySetterException>(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestNoSetter()
        {
            var receiver = new Receiver10();

            Assert.Throws<PropertyNotWritableException>(() => createDependencies().Inject(receiver));
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
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; set; }
        }

        private class Receiver6
        {
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; protected set; }
        }

        private class Receiver7
        {
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; internal set; }
        }

        private class Receiver8
        {
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; private set; }
        }

        private class Receiver9
        {
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; protected internal set; }
        }

        private class Receiver10
        {
            [Resolved(CanBeNull = true)]
            public BaseObject Obj { get; }
        }
    }
}
