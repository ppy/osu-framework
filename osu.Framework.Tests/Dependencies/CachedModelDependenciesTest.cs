// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Dependencies
{
    [TestFixture]
    public class CachedModelDependenciesTest
    {
        [Test]
        public void TestModelWithNonBindableFieldsFails()
        {
            IReadOnlyDependencyContainer unused;

            Assert.Throws<TypeInitializationException>(() => unused = new CachedModelDependencyContainer<NonBindablePublicFieldModel>(null));
            Assert.Throws<TypeInitializationException>(() => unused = new CachedModelDependencyContainer<NonBindablePrivateFieldModel>(null));
        }

        [Test]
        public void TestSettingNoModelResolvesDefault()
        {
            var resolver = new FieldModelResolver();
            var dependencies = new CachedModelDependencyContainer<FieldModel>(null);

            dependencies.Inject(resolver);

            Assert.AreEqual(1, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestModelWithBindableFieldsPropagatesToChildren()
        {
            var resolver = new FieldModelResolver();
            var dependencies = new CachedModelDependencyContainer<FieldModel>(null)
            {
                Model = { Value = new FieldModel { Bindable = { Value = 2 } } }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestModelWithBindablePropertiesPropagatesToChildren()
        {
            var resolver = new PropertyModelResolver();
            var dependencies = new CachedModelDependencyContainer<PropertyModel>(null)
            {
                Model = { Value = new PropertyModel { Bindable = { Value = 2 } } }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestChangeModelValuePropagatesToChildren()
        {
            var resolver = new PropertyModelResolver();
            var dependencies = new CachedModelDependencyContainer<PropertyModel>(null)
            {
                Model = { Value = new PropertyModel { Bindable = { Value = 2 } } }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);

            dependencies.Model.Value.Bindable.Value = 3;

            Assert.AreEqual(3, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestSubClassedModelCachedAllSuperClasses()
        {
            var resolver = new DerivedFieldModelResolver();
            var dependencies = new CachedModelDependencyContainer<DerivedFieldModel>(null)
            {
                Model = { Value = new DerivedFieldModel
                {
                    Bindable = { Value = 2 },
                    BindableString = { Value = "2" }
                } }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);
            Assert.AreEqual("2", resolver.Model.BindableString.Value);

            dependencies.Model.Value.Bindable.Value = 3;
            dependencies.Model.Value.BindableString.Value = "3";

            Assert.AreEqual(3, resolver.Model.Bindable.Value);
            Assert.AreEqual("3", resolver.Model.BindableString.Value);
        }

        [Test]
        public void TestChangeModelPropagatesAllChanges()
        {
            var resolver = new FieldModelResolver();

            var model1 = new FieldModel { Bindable = { Value = 2 } };
            var model2 = new FieldModel { Bindable = { Value = 3 } };

            var dependencies = new CachedModelDependencyContainer<FieldModel>(null)
            {
                Model = { Value = model1 }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);

            dependencies.Model.Value = model2;

            Assert.AreEqual(3, resolver.Model.Bindable.Value);

            model1.Bindable.Value = 4;

            Assert.AreEqual(3, resolver.Model.Bindable.Value);

            model2.Bindable.Value = 4;

            Assert.AreEqual(4, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestSetModelToNullAfterResolved()
        {
            var resolver = new FieldModelResolver();

            var model = new FieldModel { Bindable = { Value = 2 } };

            var dependencies = new CachedModelDependencyContainer<FieldModel>(null)
            {
                Model = { Value = model }
            };

            dependencies.Inject(resolver);

            Assert.AreEqual(2, resolver.Model.Bindable.Value);

            dependencies.Model.Value = null;

            // Todo: This is probably not what we want going forward
            Assert.AreEqual(2, resolver.Model.Bindable.Value);

            model.Bindable.Value = 3;

            Assert.AreEqual(2, resolver.Model.Bindable.Value);
        }

        [Test]
        public void TestInjectionResolvesDifferingShadowModels()
        {
            var resolver1 = new FieldModelResolver();
            var resolver2 = new FieldModelResolver();

            var dependencies = new CachedModelDependencyContainer<FieldModel>(null)
            {
                Model = { Value = new FieldModel() }
            };

            dependencies.Inject(resolver1);
            dependencies.Inject(resolver2);

            Assert.AreNotSame(resolver1.Model, resolver2.Model);
        }

        private class NonBindablePublicFieldModel
        {
#pragma warning disable 649
            public int FailingField;
#pragma warning restore 649
        }

        private class NonBindablePrivateFieldModel
        {
#pragma warning disable 169
            private int failingField;
#pragma warning restore 169
        }

        private class FieldModel
        {
            [Cached]
            public readonly Bindable<int> Bindable = new Bindable<int>(1);
        }

        private class PropertyModel
        {
            [Cached]
            public Bindable<int> Bindable { get; private set; } = new Bindable<int>(1);
        }

        private class DerivedFieldModel : FieldModel
        {
            [Cached]
            public readonly Bindable<string> BindableString = new Bindable<string>();
        }

        private class FieldModelResolver : Drawable
        {
            [Resolved]
            public FieldModel Model { get; private set; }
        }

        private class PropertyModelResolver : Drawable
        {
            [Resolved]
            public PropertyModel Model { get; private set; }
        }

        private class DerivedFieldModelResolver : Drawable
        {
            [Resolved]
            public DerivedFieldModel Model { get; private set; }
        }

        private class WholeFieldModelResolver : Drawable
        {
            [Resolved]
            public FieldModel FieldModel { get; private set; }
        }
    }
}
