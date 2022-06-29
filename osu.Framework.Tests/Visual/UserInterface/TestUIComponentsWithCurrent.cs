// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    [HeadlessTest]
    public class TestUIComponentsWithCurrent : FrameworkTestScene
    {
        [Test]
        public void TestUnbindDoesntUnbindBound()
        {
            Bindable<string> bindable = new Bindable<string>("test");
            Bindable<string> boundBindable = bindable.GetBoundCopy();

            Assert.That(boundBindable.Value, Is.EqualTo(bindable.Value));

            var dropdown = new BasicDropdown<string> { Current = bindable };

            AddStep("add dropdown", () => Add(dropdown));
            AddStep("expire", () => dropdown.Expire());
            AddUntilStep("wait for dispose", () => dropdown.IsDisposed);

            AddStep("update unrelated bindable", () => bindable.Value = "test2");

            AddAssert("ensure current unbound", () => dropdown.Current.Value != bindable.Value);
            AddAssert("ensure externals still bound", () => boundBindable.Value == bindable.Value);
        }

        [Test]
        public void TestChangeCurrent()
        {
            Bindable<string> bindable = new Bindable<string>("test");
            Bindable<string> bindable2 = new Bindable<string>("test2");

            var dropdown = new BasicDropdown<string> { Current = bindable };

            AddStep("add dropdown", () => Add(dropdown));
            AddAssert("ensure current bound", () => dropdown.Current.Value == bindable.Value);

            AddStep("change target", () => dropdown.Current = bindable2);
            AddAssert("ensure current switched", () => dropdown.Current.Value == bindable2.Value);
            AddAssert("ensure original intact", () => dropdown.Current.Value != bindable.Value);

            AddStep("change value", () => bindable2.Value = "test3");
            AddAssert("ensure current bound", () => dropdown.Current.Value == bindable2.Value);
            AddAssert("ensure original intact", () => dropdown.Current.Value != bindable.Value);
        }

        // TODO: add tests for other components
    }
}
