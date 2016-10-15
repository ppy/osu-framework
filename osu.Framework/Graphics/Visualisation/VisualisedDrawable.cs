// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : AutoSizeContainer
    {
        public Drawable Target;

        private SpriteText text;
        private Drawable previewBox;
        private Drawable activityInvalidate;
        private Drawable activityAutosize;
        private Drawable activityLayout;

        const int line_height = 12;

        public FlowContainer Flow = new FlowContainer
        {
            Direction = FlowDirection.VerticalOnly,
            Position = new Vector2(10, 14)
        };

        public override void Load(BaseGame game)
        {
            base.Load(game);

            Target.OnInvalidate += onInvalidate;

            AutoSizeContainer da = Target as AutoSizeContainer;
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
                //FontFace = FontFace.FixedWidth
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

        public VisualisedDrawable(Drawable d)
        {
            Target = d;
        }

        protected override bool OnClick(InputState state)
        {
            Flow.Alpha = Flow.Alpha > 0 ? 0 : 1;
            updateSpecifics();
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
            text.Text = Target + (!Flow.IsVisible ? $@" ({(Target as Container)?.Children.Count()} children)" : string.Empty);
        }

        protected override void Update()
        {
            text.Colour = !Flow.IsVisible ? Color4.LightBlue : Color4.White;
            //text.BackgroundColour = Target.Pinpoint ? Color4.Purple : Color4.Transparent;

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
