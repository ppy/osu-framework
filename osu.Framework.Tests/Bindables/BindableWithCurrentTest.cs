// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableWithCurrentTest
    {
        [Test]
        public void TestChangeCurrentDoesntUnbindOthers()
        {
            Bindable<string> bindable = new Bindable<string>("test");
            Bindable<string> boundBindable = bindable.GetBoundCopy();

            Assert.That(boundBindable.Value, Is.EqualTo(bindable.Value));

            var bindableWithCurrent = new BindableWithCurrent<string> { Current = bindable };

            Assert.That(boundBindable.Value, Is.EqualTo(bindable.Value));
            Assert.That(bindableWithCurrent.Value, Is.EqualTo(bindable.Value));

            bindable.Value = "test2";

            Assert.That(bindable.Value, Is.EqualTo("test2"));
            Assert.That(boundBindable.Value, Is.EqualTo(bindable.Value));
            Assert.That(bindableWithCurrent.Value, Is.EqualTo(bindable.Value));

            bindableWithCurrent.Current = new Bindable<string>();

            bindable.Value = "test3";

            Assert.That(bindable.Value, Is.EqualTo("test3"));
            Assert.That(boundBindable.Value, Is.EqualTo(bindable.Value));
            Assert.That(bindableWithCurrent.Value, Is.Not.EqualTo(bindable.Value));
        }
    }
}
