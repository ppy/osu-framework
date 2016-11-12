// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : Container
    {
        public Drawable Target;

        private SpriteText text;
        private Drawable previewBox;
        private Drawable activityInvalidate;
        private Drawable activityAutosize;
        private Drawable activityLayout;

        public Action HoverGained;
        public Action HoverLost;

        public Action RequestTarget;

        const int line_height = 12;

        public FlowContainer Flow = new FlowContainer
        {
            Direction = FlowDirection.VerticalOnly,
            AutoSizeAxes = Axes.Both,
            Position = new Vector2(10, 14)
        };

        public VisualisedDrawable(Drawable d)
        {
            Target = d;
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Target.OnInvalidate += onInvalidate;

            Container da = Target as Container;
            if (da != null) da.OnAutoSize += onAutoSize;

            FlowContainer df = Target as FlowContainer;
            if (df != null) df.OnLayout += onLayout;

            activityAutosize = new Box
            {
                Colour = Color4.Red,
                Size = new Vector2(2, line_height),
                Position = new Vector2(0, 0),
                Alpha = 0
            };

            activityLayout = new Box
            {
                Colour = Color4.Orange,
                Size = new Vector2(2, line_height),
                Position = new Vector2(3, 0),
                Alpha = 0
            };

            activityInvalidate = new Box
            {
                Colour = Color4.Yellow,
                Size = new Vector2(2, line_height),
                Position = new Vector2(6, 0),
                Alpha = 0
            };

            var sprite = Target as Sprite;
            if (sprite?.Texture != null)
                previewBox = new Sprite
                {
                    Texture = sprite.Texture,
                    Scale = new Vector2((float)sprite.Texture.Width / sprite.Texture.Height, 1)
                };
            else
                previewBox = new Box
                {
                    Colour = Color4.White
                };

            previewBox.Position = new Vector2(9, 0);
            previewBox.Size = new Vector2(line_height, line_height);

            text = new SpriteText
            {
                Position = new Vector2(24, -3),
                Scale = new Vector2(0.9f),
            };

            Flow.Alpha = 1;

            Add(activityInvalidate);
            Add(activityLayout);
            Add(activityAutosize);
            Add(previewBox);
            Add(text);
            Add(Flow);

            updateSpecifics();
        }

        protected override bool OnHover(InputState state)
        {
            HoverGained?.Invoke();
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            HoverLost?.Invoke();
            base.OnHoverLost(state);
        }

        protected override bool OnClick(InputState state)
        {
            Flow.Alpha = Flow.Alpha > 0 ? 0 : 1;
            updateSpecifics();
            return true;
        }

        protected override bool OnDoubleClick(InputState state)
        {
            RequestTarget?.Invoke();
            return true;
        }

        private void onAutoSize()
        {
            Scheduler.Add(() => activityAutosize.FadeOutFromOne(1));
            updateSpecifics();
        }

        private void onLayout()
        {
            Scheduler.Add(() => activityLayout.FadeOutFromOne(1));
            updateSpecifics();
        }

        private void onInvalidate()
        {
            Scheduler.Add(() => activityInvalidate.FadeOutFromOne(1));
            updateSpecifics();
        }

        private void updateSpecifics()
        {
            previewBox.Alpha = Math.Max(0.2f, Target.Alpha);
            previewBox.Colour = Target.Colour;

            int childCount = (Target as Container)?.Children.Count() ?? 0;

            text.Text = Target + (!Flow.IsVisible && childCount > 0 ? $@" ({childCount} children)" : string.Empty);
        }

        protected override void Update()
        {
            text.Colour = !Flow.IsVisible ? Color4.LightBlue : Color4.White;
            base.Update();
        }

        public bool CheckExpiry()
        {
            if (!IsAlive) return false;

            if (!Target.IsAlive || Target.Parent == null || Target.Alpha == 0)
            {
                Expire();
                return false;
            }

            Alpha = Target.IsVisible ? 1 : 0.3f;
            return true;
        }
    }
}
