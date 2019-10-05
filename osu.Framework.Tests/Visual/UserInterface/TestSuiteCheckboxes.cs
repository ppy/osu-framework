// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSuiteCheckboxes : FrameworkTestSuite<TestSceneCheckboxes>
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Checkbox),
            typeof(BasicCheckbox)
        };

        public TestSuiteCheckboxes()
        {
            TestScene.Swap.Current.ValueChanged += check => TestScene.Swap.RightHandedCheckbox = check.NewValue;
            TestScene.Rotate.Current.ValueChanged += e => TestScene.Rotate.RotateTo(e.NewValue ? 45 : 0, 100);
        }

        /// <summary>
        /// Test safety of <see cref="IHasCurrentValue{T}"/> implementation.
        /// This is shared across all UI elements.
        /// </summary>
        [Test]
        public void TestDirectToggle()
        {
            var testBindable = TestScene.Basic.Current.GetBoundCopy();

            AddAssert("is unchecked", () => !TestScene.Basic.Current.Value);
            AddAssert("bindable unchecked", () => !testBindable.Value);

            AddStep("switch bindable directly", () => TestScene.Basic.Current.Value = true);

            AddAssert("is checked", () => TestScene.Basic.Current.Value);
            AddAssert("bindable checked", () => testBindable.Value);

            AddStep("change bindable", () => TestScene.Basic.Current = new Bindable<bool>());

            AddAssert("is unchecked", () => !TestScene.Basic.Current.Value);
            AddAssert("bindable unchecked", () => !testBindable.Value);
        }
    }
}
