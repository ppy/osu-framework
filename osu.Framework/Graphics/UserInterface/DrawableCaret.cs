// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class DrawableCaret : CompositeDrawable
    {
        public DrawableCaret()
        {
            Alpha = 0;
            Width = 3;
            Colour = Color4.Transparent;

            Masking = true;
            CornerRadius = 1;

            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
            };
        }

        /// <summary>
        /// Shows this caret and starts blinking.
        /// </summary>
        public override void Show()
        {
            base.Show();

            this.FadeColour(Color4.White, 200, Easing.Out)
                .Loop(c => c.FadeTo(0.7f).FadeTo(0.2f, 500, Easing.InOutSine));
        }

        /// <summary>
        /// Stops the blinking and fades out this caret.
        /// </summary>
        public override void Hide()
        {
            ClearTransforms(targetMember: nameof(Alpha));

            base.Hide();
        }
    }
}
