// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Testing.Dependencies;

// ReSharper disable UnusedAutoPropertyAccessor.Local

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

        [Test]
        public void TestWriteToBaseClassWithPublicProperty()
        {
            var receiver = new Receiver11();

            BaseObject testObject;

            var dependencies = createDependencies(testObject = new BaseObject());

            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
            Assert.AreEqual(testObject, receiver.Obj);
        }

        [Test]
        public void TestResolveInternalStruct()
        {
            var receiver = new Receiver12();

            var testObject = new CachedStructProvider();

            var dependencies = DependencyActivator.MergeDependencies(testObject, new DependencyContainer());

            Assert.DoesNotThrow(() => dependencies.Inject(receiver));
            Assert.AreEqual(testObject.CachedObject.Value, receiver.Obj.Value);
        }

        [TestCase(null)]
        [TestCase(10)]
        public void TestResolveNullableInternal(int? testValue)
        {
            var receiver = new Receiver13();

            var testObject = new CachedNullableProvider();
            testObject.SetValue(testValue);

            var dependencies = DependencyActivator.MergeDependencies(testObject, new DependencyContainer());

            dependencies.Inject(receiver);

            Assert.AreEqual(testValue, receiver.Obj);
        }

        [Test]
        public void TestResolveStructWithoutNullPermits()
        {
            Assert.Throws<DependencyNotRegisteredException>(() => new DependencyContainer().Inject(new Receiver14()));
        }

        [Test]
        public void TestResolveStructWithNullPermits()
        {
            var receiver = new Receiver15();

            Assert.DoesNotThrow(() => new DependencyContainer().Inject(receiver));
            Assert.AreEqual(0, receiver.Obj);
        }

        [Test]
        public void TestResolveBindable()
        {
            var receiver = new Receiver16();

            var bindable = new Bindable<int>(10);
            var dependencies = createDependencies(bindable);
            dependencies.CacheAs<IBindable<int>>(bindable);

            dependencies.Inject(receiver);

            Assert.AreNotSame(bindable, receiver.Obj);
            Assert.AreNotSame(bindable, receiver.Obj2);
            Assert.AreEqual(bindable.Value, receiver.Obj.Value);
            Assert.AreEqual(bindable.Value, receiver.Obj2.Value);

            bindable.Value = 5;
            Assert.AreEqual(bindable.Value, receiver.Obj.Value);
            Assert.AreEqual(bindable.Value, receiver.Obj2.Value);
        }

        [Test]
        public void TestResolveNullableWithNullableReferenceTypes()
        {
            var receiver = new Receiver17();
            Assert.DoesNotThrow(() => createDependencies().Inject(receiver));
        }

        [Test]
        public void TestResolveNonNullWithNullableReferenceTypes()
        {
            var receiver = new Receiver18();

            // Throws with non-nullable dependency not cached.
            Assert.Throws<DependencyNotRegisteredException>(() => createDependencies().Inject(receiver));

            // Does not throw with the non-nullable dependency cached.
            Assert.DoesNotThrow(() => createDependencies(new Bindable<int>(10)).Inject(receiver));
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
#pragma warning disable 649, IDE0032
            private BaseObject obj;
#pragma warning restore 649, IDE0032

            // ReSharper disable once ConvertToAutoProperty
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

        private class Receiver11 : Receiver8
        {
        }

        private class Receiver12
        {
            [Resolved]
            public CachedStructProvider.Struct Obj { get; private set; }
        }

        private class Receiver13
        {
            [Resolved]
            public int? Obj { get; private set; }
        }

        private class Receiver14
        {
            [Resolved]
            public int Obj { get; private set; }
        }

        private class Receiver15
        {
            [Resolved(CanBeNull = true)]
            public int Obj { get; private set; } = 1;
        }

        private class Receiver16
        {
            [Resolved]
            public Bindable<int> Obj { get; private set; }

            [Resolved]
            public IBindable<int> Obj2 { get; private set; }
        }

#nullable enable
        private class Receiver17
        {
            [Resolved]
            public Bindable<int>? Obj { get; private set; }
        }

        private class Receiver18
        {
            [Resolved]
            public Bindable<int> Obj { get; private set; } = null!;
        }
#nullable disable
    }
}
