// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawableInspector : VisibilityContainer
    {
        [Cached]
        public Bindable<Drawable> InspectedDrawable { get; private set; } = new Bindable<Drawable>();

        private const float width = 600;

        public DrawableInspector()
        {
            Width = width;
            RelativeSizeAxes = Axes.Y;

            Child = new PropertyDisplay();
        }

        protected override void PopIn()
        {
            this.ResizeWidthTo(width, 500, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.ResizeWidthTo(0, 500, Easing.OutQuint);
        }
    }
}
