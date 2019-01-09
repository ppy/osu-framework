// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBindingContainer : TestCase
    {
        [Test]
        public void TestCacheModel()
        {
            var model = new TestModel();
            BindingContainer<TestModel> container = null;

            TestResolver resolver = null;

            AddStep("initialise", () =>
            {
                Child = container = new BindingContainer<TestModel>
                {
                    Model = model,
                    Child = resolver = new TestResolver()
                };
            });

            AddAssert("bindable resolved", () => resolver.Cached.Value == model.Cached.Value);

            AddStep("change model", () => container.Model = model = new TestModel { Cached = { Value = 2 } });

            AddAssert("bindable resolved", () => resolver.Cached.Value == model.Cached.Value);
        }

        private class TestModel
        {
            [Cached]
            public readonly Bindable<int> Cached = new Bindable<int>(1);
        }

        private class TestResolver : CompositeDrawable
        {
            [Resolved(Parent = typeof(TestModel))]
            public Bindable<int> Cached { get; private set; }
        }
    }
}
