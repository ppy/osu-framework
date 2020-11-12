// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawableInspector : VisibilityContainer
    {
        private const float width = 600;

        private readonly PropertyDisplay propertyDisplay;

        public DrawableInspector()
        {
            Width = width;
            RelativeSizeAxes = Axes.Y;

            Child = propertyDisplay = new PropertyDisplay();
        }

        protected override void PopIn()
        {
            this.ResizeWidthTo(width, 500, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.ResizeWidthTo(0, 500, Easing.OutQuint);
        }

        public void UpdateFrom(Drawable source)
        {
            propertyDisplay.UpdateFrom(source);
        }
    }
}
