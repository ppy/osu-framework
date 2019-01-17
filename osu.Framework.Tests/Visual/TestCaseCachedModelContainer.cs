// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCachedModelContainer : TestCase
    {
        [Test]
        public void TestModelWithNonBindableFieldsFails()
        {
            Assert.Throws<TypeInitializationException>(() => Child = new CachedModelComposite<NonBindablePublicFieldModel>());
            Assert.Throws<TypeInitializationException>(() => Child = new CachedModelComposite<NonBindablePrivateFieldModel>());
        }

        [Test]
        public void TestSettingNoModelResolvesDefault()
        {
            FieldModelResolver resolver = null;

            AddStep("initialise", () => Child = new CachedModelContainer<FieldModel> { Child = resolver = new FieldModelResolver() });
            AddAssert("resolved default bindable", () => resolver.Bindable.Value == 1);
        }

        [Test]
        public void TestModelWithBindableFieldsPropagatesToChildren()
        {
            FieldModelResolver resolver = null;

            AddStep("initialise", () => Child = new CachedModelContainer<FieldModel>
            {
                Model = new FieldModel { Bindable = { Value = 2 } },
                Child = resolver = new FieldModelResolver()
            });

            AddAssert("resolved bindable value = 2", () => resolver.Bindable.Value == 2);
        }

        [Test]
        public void TestModelWithBindablePropertiesPropagatesToChildren()
        {
            PropertyModelResolver resolver = null;

            AddStep("initialise", () => Child = new CachedModelContainer<PropertyModel>
            {
                Model = new PropertyModel { Bindable = { Value = 2 } },
                Child = resolver = new PropertyModelResolver()
            });

            AddAssert("resolved bindable value = 2", () => resolver.Bindable.Value == 2);
        }

        [Test]
        public void TestChangeModelValuePropagatesToChildren()
        {
            CachedModelContainer<FieldModel> container = null;
            FieldModelResolver resolver = null;

            AddStep("initialise", () => Child = container = new CachedModelContainer<FieldModel>
            {
                Model = new FieldModel { Bindable = { Value = 2 } },
                Child = resolver = new FieldModelResolver()
            });

            AddStep("change model value to 3", () => container.Model.Bindable.Value = 3);
            AddAssert("resolved bindable value = 3", () => resolver.Bindable.Value == 3);
        }

        [Test]
        public void TestSubClassedModelCachedAllSuperClasses()
        {
            CachedModelContainer<DerivedFieldModel> container = null;
            DerivedFieldModelResolver resolver = null;

            AddStep("initialise", () => Child = container = new CachedModelContainer<DerivedFieldModel>
            {
                Model = new DerivedFieldModel { Bindable = { Value = 2 } },
                Child = resolver = new DerivedFieldModelResolver()
            });

            AddStep("change model value to 3", () =>
            {
                container.Model.Bindable.Value = 3;
                container.Model.BindableString.Value = "3";
            });

            AddAssert("resolved bindable value = 3", () => resolver.Bindable.Value == 3 && resolver.BindableString.Value == "3");
        }

        [Test]
        public void TestChangeModelPropagatesAllChanges()
        {
            CachedModelContainer<FieldModel> container = null;
            FieldModelResolver resolver = null;

            var model1 = new FieldModel { Bindable = { Value = 2 } };
            var model2 = new FieldModel { Bindable = { Value = 3 } };

            AddStep("initialise", () => Child = container = new CachedModelContainer<FieldModel>
            {
                Model = model1,
                Child = resolver = new FieldModelResolver()
            });

            AddStep("change model", () => container.Model = model2);
            AddAssert("resolved bindable value = 3", () => resolver.Bindable.Value == 3 );

            AddStep("change model1 value to 4", () => model1.Bindable.Value = 4);
            AddAssert("resolved bindable value = 3", () => resolver.Bindable.Value == 3 );

            AddStep("change model2 value to 4", () => model2.Bindable.Value = 4);
            AddAssert("resolved bindable value = 4", () => resolver.Bindable.Value == 4 );
        }

        [Test]
        public void TestSetModelToNullAfterResolved()
        {
            CachedModelContainer<FieldModel> container = null;
            FieldModelResolver resolver = null;

            var model = new FieldModel { Bindable = { Value = 2 } };

            AddStep("initialise", () => Child = container = new CachedModelContainer<FieldModel>
            {
                Model = model,
                Child = resolver = new FieldModelResolver()
            });

            AddStep("set model to null", () => container.Model = null);
            AddAssert("resolved bindable value = 2", () => resolver.Bindable.Value == 2);

            AddStep("change model value to 3", () => model.Bindable.Value = 3);
            AddAssert("resolved bindable value = 2", () => resolver.Bindable.Value == 2);
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
            [Resolved(typeof(FieldModel))]
            public Bindable<int> Bindable { get; private set; }
        }

        private class PropertyModelResolver : Drawable
        {
            [Resolved(typeof(PropertyModel))]
            public Bindable<int> Bindable { get; private set; }
        }

        private class DerivedFieldModelResolver : Drawable
        {
            [Resolved(typeof(DerivedFieldModel))]
            public Bindable<int> Bindable { get; private set; }

            [Resolved(typeof(DerivedFieldModel))]
            public Bindable<string> BindableString { get; private set; }
        }
    }
}
