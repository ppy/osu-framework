// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableWithCurrentTest
    {
        [Test]
        public void TestBindableWithCurrentReceivesBoundValue()
        {
            const string expected_value = "test";

            var bindable = new Bindable<string>(expected_value);
            var bindableWithCurrent = new BindableWithCurrent<string> { Current = bindable };

            Assert.That(bindable.Value, Is.EqualTo(expected_value));
            Assert.That(bindableWithCurrent.Value, Is.EqualTo(expected_value));
        }

        [Test]
        public void TestBindableWithCurrentReceivesValueChanges()
        {
            const string expected_value = "test2";

            var bindable = new Bindable<string>();
            var bindableWithCurrent = new BindableWithCurrent<string> { Current = bindable };

            bindable.Value = expected_value;

            Assert.That(bindableWithCurrent.Value, Is.EqualTo(expected_value));
        }

        [Test]
        public void TestChangeCurrentDoesNotUnbindOthers()
        {
            const string expected_value = "test2";

            var bindable1 = new Bindable<string>();
            var bindable2 = bindable1.GetBoundCopy();
            var bindableWithCurrent = new BindableWithCurrent<string> { Current = bindable1 };

            bindableWithCurrent.Current = new Bindable<string>();
            bindable1.Value = expected_value;

            Assert.That(bindable2.Value, Is.EqualTo(expected_value));
            Assert.That(bindableWithCurrent.Value, Is.Not.EqualTo(expected_value));
        }

        [Test]
        public void TestChangeCurrentBindsToNewBindable()
        {
            const string expected_value = "test3";

            var bindable1 = new Bindable<string>();
            var bindable2 = new Bindable<string>();
            var bindableWithCurrent = new BindableWithCurrent<string> { Current = bindable1 };

            bindableWithCurrent.Current = bindable2;
            bindableWithCurrent.Value = "test3";

            Assert.That(bindable1.Value, Is.Not.EqualTo(expected_value));
            Assert.That(bindable2.Value, Is.EqualTo(expected_value));
        }

        [Test]
        public void TestUIControlsUsingCurrent()
        {
            Assert.That(new BasicCheckbox().Current, Is.TypeOf<BindableWithCurrent<bool>>());
            Assert.That(new BasicSliderBar<double>().Current, Is.TypeOf<BindableNumberWithCurrent<double>>());
            Assert.That(new BasicTextBox().Current, Is.TypeOf<BindableWithCurrent<string>>());
            Assert.That(new BasicDropdown<object>().Current, Is.TypeOf<BindableWithCurrent<object>>());
        }

        [Test]
        public void TestCreateBindableWithCurrentViaFactory()
        {
            Assert.That(IBindableWithCurrent<string>.Create(), Is.TypeOf<BindableWithCurrent<string>>());
            Assert.That(IBindableWithCurrent<bool>.Create(), Is.TypeOf<BindableWithCurrent<bool>>());
            Assert.That(IBindableWithCurrent<int>.Create(), Is.TypeOf<BindableNumberWithCurrent<int>>());
        }

        [Test]
        public void TestGetBoundCopy()
        {
            Assert.That(new BindableWithCurrent<string>().GetBoundCopy(), Is.TypeOf<BindableWithCurrent<string>>());
            Assert.That(new BindableNumberWithCurrent<int>().GetBoundCopy(), Is.TypeOf<BindableNumberWithCurrent<int>>());
        }
    }
}
