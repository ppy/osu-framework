// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCachedModelContainer : TestCase
    {
        [Test]
        public void TestNoModel()
        {
            TestDerivedResolver resolver = null;

            AddStep("setup", () => Child = new CachedModelContainer<TestDerivedModel> { Child = resolver = new TestDerivedResolver() });
            AddAssert("resolver has default values", () => allEqual(0, resolver));
        }

        [Test]
        public void TestSingleModel()
        {
            var model = new TestModel { Value = 1 };
            TestResolver resolver = null;

            AddStep("setup", () => Child = new CachedModelContainer<TestModel>
            {
                Model = model,
                Child = resolver = new TestResolver()
            });

            AddAssert("all values == 1", () => allEqual(1, resolver));
            AddStep("set model value = 2", () => model.Value = 2);
            AddAssert("all values == 2 (change)", () => allEqual(2, resolver));
        }

        [Test]
        public void TestSingleDerivedModel()
        {
            var model = new TestDerivedModel { Value = 1 };
            TestDerivedResolver resolver = null;

            AddStep("setup", () => Child = new CachedModelContainer<TestDerivedModel>
            {
                Model = model,
                Child = resolver = new TestDerivedResolver()
            });

            AddAssert("all values == 1", () => allEqual(1, resolver));
            AddStep("set model value = 2", () => model.Value = 2);
            AddAssert("all values == 2 (change)", () => allEqual(2, resolver));
        }

        [Test]
        public void TestChangeModel()
        {
            var model1 = new TestDerivedModel { Value = 1 };
            var model2 = new TestDerivedModel { Value = 2 };

            CachedModelContainer<TestDerivedModel> container = null;
            TestDerivedResolver resolver = null;

            AddStep("setup", () => Child = container = new CachedModelContainer<TestDerivedModel>
            {
                Model = model1,
                Child = resolver = new TestDerivedResolver()
            });

            AddAssert("all values == 1", () => allEqual(1, resolver));

            AddStep("change model", () => container.Model = model2);
            AddAssert("all bindable values == 2", () => allBindablesEqual(2, resolver));
            AddAssert("all field values == 1", () => allFieldsEqual(1, resolver));

            AddStep("set first model value = 3", () => model1.Value = 3);
            AddAssert("all bindable values == 2 (no change)", () => allBindablesEqual(2, resolver));
            AddAssert("all field values == 3 (change)", () => allFieldsEqual(3, resolver));

            AddStep("set second model value = 3", () => model2.Value = 3);
            AddAssert("all bindable values == 3 (change)", () => allBindablesEqual(3, resolver));
            AddAssert("all field values == 3 (change)", () => allFieldsEqual(3, resolver));
        }

        [Test]
        public void TestNullModel()
        {
            var model = new TestDerivedModel { Value = 1 };

            CachedModelContainer<TestDerivedModel> container = null;
            TestDerivedResolver resolver = null;

            AddStep("setup", () => Child = container = new CachedModelContainer<TestDerivedModel>
            {
                Model = model,
                Child = resolver = new TestDerivedResolver()
            });

            AddAssert("all values == 1", () => allEqual(1, resolver));

            AddStep("change model to null", () => container.Model = null);
            AddAssert("all values == 1", () => allEqual(1, resolver));

            AddStep("set model value = 2", () => model.Value = 2);
            AddAssert("all bindable values == 1", () => allBindablesEqual(1, resolver));
            AddAssert("all fields values == 2", () => allFieldsEqual(2, resolver));
        }

        private bool allBindablesEqual(int value, TestResolver resolver)
            => resolver.CachedBindable1.Value == value && resolver.CachedBindable2.Value == value;

        private bool allFieldsEqual(int value, TestResolver resolver)
            => resolver.CachedField1.Value == value && resolver.CachedField2.Value == value;

        private bool allBindablesEqual(int value, TestDerivedResolver resolver)
            => resolver.CachedBindable1.Value == value && resolver.CachedBindable2.Value == value && resolver.CachedBindable3.Value == value;

        private bool allFieldsEqual(int value, TestDerivedResolver resolver)
            => resolver.CachedField1.Value == value && resolver.CachedField2.Value == value && resolver.CachedField3.Value == value;

        private bool allEqual(int value, TestResolver resolver) => allBindablesEqual(value, resolver) && allFieldsEqual(value, resolver);

        private bool allEqual(int value, TestDerivedResolver resolver) => allBindablesEqual(value, resolver) && allFieldsEqual(value, resolver);

        private class TestModel
        {
            private int value;

            public virtual int Value
            {
                get => value;
                set
                {
                    this.value = value;

                    CachedBindable1.Value = value;
                    CachedBindable2.Value = value;
                    CachedField1.Value = value;
                    CachedField2.Value = value;
                }
            }

            [Cached]
            public readonly Bindable<int> CachedBindable1 = new Bindable<int>();

            [Cached]
            private Bindable<int> cachedBindable2 = new Bindable<int>();

            public Bindable<int> CachedBindable2 => cachedBindable2;

            [Cached]
            public FieldWrapper CachedField1 { get; private set; } = new FieldWrapper();

            [Cached]
            private FieldWrapper cachedField2 { get; set; } = new FieldWrapper();

            public FieldWrapper CachedField2
            {
                get => cachedField2;
                set => cachedField2 = value;
            }
        }

        private class TestDerivedModel : TestModel
        {
            public override int Value
            {
                get => base.Value;
                set
                {
                    base.Value = value;

                    CachedBindable3.Value = value;
                    CachedField3.Value = value;
                }
            }

            [Cached]
            public readonly Bindable<int> CachedBindable3 = new Bindable<int>();

            [Cached]
            public FieldWrapper CachedField3 { get; private set; } = new FieldWrapper();
        }

        private class TestResolver : CompositeDrawable
        {
            [Resolved(typeof(TestModel))]
            public Bindable<int> CachedBindable1 { get; private set; }

            [Resolved(typeof(TestModel), "cachedBindable2")]
            public Bindable<int> CachedBindable2 { get; private set; }

            [Resolved(typeof(TestModel))]
            public FieldWrapper CachedField1 { get; private set; }

            [Resolved(typeof(TestModel), "cachedField2")]
            public FieldWrapper CachedField2 { get; private set; }
        }

        private class TestDerivedResolver : CompositeDrawable
        {
            [Resolved(typeof(TestDerivedModel))]
            public Bindable<int> CachedBindable1 { get; private set; }

            [Resolved(typeof(TestDerivedModel), "cachedBindable2")]
            public Bindable<int> CachedBindable2 { get; private set; }

            [Resolved(typeof(TestDerivedModel), canBeNull: true)]
            public Bindable<int> CachedBindable3 { get; private set; }

            [Resolved(typeof(TestDerivedModel))]
            public FieldWrapper CachedField1 { get; private set; }

            [Resolved(typeof(TestDerivedModel), "cachedField2")]
            public FieldWrapper CachedField2 { get; private set; }

            [Resolved(typeof(TestDerivedModel))]
            public FieldWrapper CachedField3 { get; private set; }
        }

        private class FieldWrapper
        {
            public int Value;
        }
    }
}
