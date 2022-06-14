// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TransformDisplay : Container
    {
        private readonly FillFlowContainer<DrawableTransform> flow;
        private Bindable<Drawable> inspectedDrawable;

        public TransformDisplay()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.GreenDarker,
                    RelativeSizeAxes = Axes.Both
                },
                new BasicScrollContainer
                {
                    Padding = new MarginPadding(10),
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarOverlapsContent = false,
                    Child = flow = new FillFlowContainer<DrawableTransform>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2)
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(Bindable<Drawable> inspected)
        {
            inspectedDrawable = inspected.GetBoundCopy();
        }

        protected override void Update()
        {
            base.Update();
            flow.Clear();

            if (inspectedDrawable.Value == null)
                return;

            foreach (var t in inspectedDrawable.Value.Transforms)
                flow.Add(new DrawableTransform(t));
        }
    }
}
