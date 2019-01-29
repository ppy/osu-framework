// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBindingContainer : TestCase
    {
        private BindingContainer<TestModel> container;
        private TestResolver resolver;

        [SetUp]
        public void Setup() => Schedule(() => Child = container = new BindingContainer<TestModel> { Child = resolver = new TestResolver() });

        [Test]
        public void TestNoModel()
        {
            AddAssert("has default value", () => resolver.Cached.Value == 0);
        }

        [Test]
        public void TestSingleModel()
        {
            var model = new TestModel { Cached = { Value = 1 } };

            AddStep("add model", () => container.Model = model);
            AddAssert("bindable resolved", () => resolver.Cached.Value == model.Cached.Value);

            AddStep("change model value", () => model.Cached.Value = 2);
            AddAssert("bindable changed", () => resolver.Cached.Value == model.Cached.Value);
        }

        [Test]
        public void TestChangeModel()
        {
            TestModel model1 = new TestModel { Cached = { Value = 1 } };
            TestModel model2 = new TestModel { Cached = { Value = 2 } };

            AddStep("add model", () => container.Model = model1);
            AddAssert("bindable resolved", () => resolver.Cached.Value == model1.Cached.Value);

            AddStep("change model", () => container.Model = model2);
            AddAssert("bindable changed", () => resolver.Cached.Value == model2.Cached.Value);

            AddStep("change first model", () => model1.Cached.Value = 3);
            AddAssert("bindable didn't change", () => resolver.Cached.Value != 3);

            AddStep("change second model", () => model2.Cached.Value = 3);
            AddAssert("bindable did change", () => resolver.Cached.Value == 3);
        }

        [Test]
        public void TestNullModel()
        {
            TestModel model = new TestModel { Cached = { Value = 1 } };

            AddStep("add model", () => container.Model = model);
            AddAssert("bindable resolved", () => resolver.Cached.Value == model.Cached.Value);

            AddStep("set null model", () => container.Model = null);
            AddAssert("bindable didn't change", () => resolver.Cached.Value == model.Cached.Value);

            AddStep("change model value", () => model.Cached.Value = 2);
            AddAssert("bindable didn't change", () => resolver.Cached.Value == 1);
        }

        private class TestModel
        {
            [Cached]
            public readonly Bindable<int> Cached = new Bindable<int>();
        }

        private class TestResolver : CompositeDrawable
        {
            [Resolved(Parent = typeof(TestModel))]
            public Bindable<int> Cached { get; private set; }
        }
    }
}
