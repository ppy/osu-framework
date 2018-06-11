// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Displays a blinking Caret
    /// </summary>
    public class DrawableCaret : CompositeDrawable
    {
        public DrawableCaret()
        {
            Alpha = 0;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
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
        /// Fades out this caret over a short duration after clearing all transforms.
        /// </summary>
        public override void Hide()
        {
            ClearTransforms();
            this.FadeOut(200);
        }

        /// <summary>
        /// Clears all transforms and moves this caret to the specified location.
        /// </summary>
        public void ResetTo(Vector2 position, double duration)
        {
            ClearTransforms();
            this.MoveTo(position, duration, Easing.Out);
        }
    }
}
