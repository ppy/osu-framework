// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation that switches the displayed drawable when a new frame is displayed.
    /// </summary>
    public class DrawableAnimation : Animation<Drawable>
    {
        private Container container;

        protected override void DisplayFrame(Drawable content)
        {
            // don't dispose previous frames as they may be displayed again.
            container.Clear(false);

            container.Child = content;
        }

        protected override void ClearDisplay() => container.Clear(false);

        public override Drawable CreateContent() => container = new Container { RelativeSizeAxes = Axes.Both };

        protected override Vector2 GetCurrentDisplaySize() => container.Children.FirstOrDefault()?.DrawSize ?? Vector2.Zero;

        protected override float GetFillAspectRatio() => container.Children.FirstOrDefault()?.FillAspectRatio ?? 1;
    }
}
