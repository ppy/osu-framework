//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseTextBox : TestCase
    {
        private TextBox tb;

        internal override string Name => @"TextBox";

        internal override string Description => @"Text entry evolved";

        internal override int DisplayOrder => -1;

        internal override void Reset()
        {
            base.Reset();

            FlowContainer textBoxes = new FlowContainer(FlowDirection.VerticalOnly)
            {
                Padding = new Vector2(0, 50),
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                SizeMode = InheritMode.Inherit,
                Size = new Vector2(0.8f, 1)
            };

            Add(textBoxes);

            textBoxes.Add(tb = new SearchBox(14, new Vector2(0, Vector2.Zero.Y), 20, Graphics.Renderers.TextAlignment.Centre));

            textBoxes.Add(tb = new TextBox(@"", 14, Vector2.Zero, 100));

            textBoxes.Add(tb = new TextBox(@"Limited length", 14, Vector2.Zero, 200)
            {
                LengthLimit = 20
            });

            textBoxes.Add(tb = new TextBox(@"Box with some more text", 14, Vector2.Zero, 300));

            textBoxes.Add(tb = new PasswordTextBox(@"", 14, Vector2.Zero, 300));
        }
    }
}
