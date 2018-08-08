// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Globalization;
using NUnit.Framework;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Layout.TextFlowContainerTests
{
    [TestFixture]
    public class ValidationTest : LayoutTest
    {
        /// <summary>
        /// Tests that flow and text layout are validated together.
        /// </summary>
        [Test]
        public void TestFlowLayoutAndTextLayoutValidated()
        {
            var text = new TestTextFlow
            {
                AutoSizeAxes = Axes.Both,
                Text = "a b c\n\nd e f\n\ng h i j"
            };

            Run(text, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(text.FlowLayout.IsValid, "flow layout is valid");
                Assert.IsTrue(text.TextLayout.IsValid, "text layout is valid");
                return true;
            });
        }

        /// <summary>
        /// Tests that flow and text layout validate even when the text is changed in UpdateAfterChildren.
        /// </summary>
        [Test]
        public void TestFlowLayoutAndTextLayoutValidateWhenChangedInUpdate()
        {
            var text = new UpdatingTextFlow { AutoSizeAxes = Axes.Both };

            Run(text, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(text.FlowLayout.IsValid, "flow layout is valid");
                Assert.IsTrue(text.TextLayout.IsValid, "text layout is valid");
                return true;
            });
        }

        private class UpdatingTextFlow : TestTextFlow
        {
            protected override void UpdateAfterChildren()
            {
                base.UpdateAfterChildren();
                Text = Time.Current.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
