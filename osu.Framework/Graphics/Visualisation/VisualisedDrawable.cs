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

        public FlowContainer Flow;

        public VisualisedDrawable(Drawable d)
        {
            Target = d;
            Target.OnInvalidate += onInvalidate;

            var da = Target as Container<Drawable>;
            if (da != null) da.OnAutoSize += onAutoSize;

            var df = Target as FlowContainer<Drawable>;
            if (df != null) df.OnLayout += onLayout;

            var sprite = Target as Sprite;

            AutoSizeAxes = Axes.Both;
            Add(new Drawable[]
            {
                activityInvalidate = new Box
                {
                    Colour = Color4.Yellow,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(6, 0),
                    Alpha = 0
                },
                activityLayout = new Box
                {
                    Colour = Color4.Orange,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(3, 0),
                    Alpha = 0
                },
                activityAutosize = new Box
                {
                    Colour = Color4.Red,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(0, 0),
                    Alpha = 0
                },
                previewBox = sprite?.Texture == null ? previewBox = new Box { Colour = Color4.White } : new Sprite
                {
                    Texture = sprite.Texture,
                    Scale = new Vector2((float)sprite.Texture.Width / sprite.Texture.Height, 1),
                },
                text = new SpriteText
                {
                    Position = new Vector2(24, -3),
                    Scale = new Vector2(0.9f),
                },
                Flow = new FlowContainer
                {
                    Direction = FlowDirection.VerticalOnly,
                    AutoSizeAxes = Axes.Both,
                    Position = new Vector2(10, 14)
                },
            });

            previewBox.Position = new Vector2(9, 0);
            previewBox.Size = new Vector2(line_height, line_height);

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
            previewBox.ColourInfo = Target.ColourInfo;

            int childCount = (Target as IContainerEnumerable<Drawable>)?.Children.Count() ?? 0;

            text.Text = Target + (!Flow.IsPresent && childCount > 0 ? $@" ({childCount} children)" : string.Empty);
        }

        protected override void Update()
        {
            text.Colour = !Flow.IsPresent ? Color4.LightBlue : Color4.White;
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

            Alpha = Target.IsPresent ? 1 : 0.3f;
            return true;
        }
    }
}
