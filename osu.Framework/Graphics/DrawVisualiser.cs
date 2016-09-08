//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics
{
    public class DrawVisualiser : LargeContainer
    {
        Box background = new Box()
        {
            Colour = new Color4(30, 30, 30, 240),
            SizeMode = InheritMode.XY,
            Depth = 0
        };

        ScrollContainer scroll = new ScrollContainer();

        FlowContainer flow = new FlowContainer()
        {
            Direction = FlowDirection.VerticalOnly
        };

        Drawable target;
        private ScheduledDelegate scheduledUpdater;
        private SpriteText loadMessage;

        public override void Load()
        {
            base.Load();

            AddProcessingContainer(new MaskingContainer());

            scroll.Add(flow);

            Size = new Vector2(0.3f, 1);

            Add(background);
            Add(scroll);

            /*OnHover += delegate
            {
                FadeTo(1, 100);
                return true;
            };

            OnHoverLost += delegate
            {
                if (!ContainsHoveredDrawable)
                    FadeTo(0.2f, 100);
                return true;
            };
            Alpha = 0.2f;*/

#if DEBUG
            Add(loadMessage = new SpriteText()
            {
                Text = @"Click to load DrawVisualiser",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
#else
            Add(new SpriteText()
            {
                Text = @"DrawVisualiser only available in debug mode!",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
#endif
        }

        public DrawVisualiser(Drawable target)
        {
            this.target = target;
        }

        protected override void Dispose(bool isDisposing)
        {
            scheduledUpdater?.Cancel();
            base.Dispose(isDisposing);
        }

#if DEBUG
        protected override bool OnClick(InputState state)
        {
            if (loadMessage == null)
                return false;

            Remove(loadMessage);
            loadMessage = null;

            scheduledUpdater = Game.Scheduler.AddDelayed(runUpdate, 20, true);

            return true;
        }

        private void runUpdate()
        {
            visualise(target, flow);
        }


        private void visualise(Drawable d, FlowContainer container, int depth = 0)
        {
            if (d == this) return;

            var drawables = container.Children.ConvertAll<VisualisedDrawable>(o => o as VisualisedDrawable);

            drawables.ForEach(dd => dd.CheckExpiry());
            
            VisualisedDrawable vd = drawables.Find(dd => dd.Drawable == d);
            if (vd == null)
            {
                vd = new VisualisedDrawable(d) { Depth = depth };
                container.Add(vd);
            }

            d.Children.ForEach(c => { if (c.IsAlive) visualise(c, vd.Flow, depth++); });
        }

        class VisualisedDrawable : AutoSizeContainer
        {
            public Drawable Drawable;

            private SpriteText text;
            private Drawable previewBox;
            private Drawable activityInvalidate;
            private Drawable activityAutosize;
            private Drawable activityLayout;

            const int line_height = 12;

            public FlowContainer Flow = new FlowContainer()
            {
                Direction = FlowDirection.VerticalOnly,
                Position = new Vector2(10, 14)
            };

            public override void Load()
            {
                base.Load();

                Drawable.OnInvalidate += onInvalidate;
                

                AutoSizeContainer da = Drawable as AutoSizeContainer;
                if (da != null) da.OnAutoSize += onAutoSize;

                FlowContainer df = Drawable as FlowContainer;
                if (df != null) df.OnLayout += onLayout;

                activityAutosize = new Box()
                {
                    Colour = Color4.Red,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(0, 0),
                    Alpha = 0
                };

                activityLayout = new Box()
                {
                    Colour = Color4.Orange,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(3, 0),
                    Alpha = 0
                };

                activityInvalidate = new Box()
                {
                    Colour = Color4.Yellow,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(6, 0),
                    Alpha = 0
                };

                var sprite = Drawable as Sprite;
                if (sprite != null)
                    previewBox = new Sprite(sprite.Texture);
                else
                    previewBox = new Box() { Colour = Color4.White };

                previewBox.Position = new Vector2(9, 0);
                previewBox.Size = new Vector2(line_height, line_height);

                text = new SpriteText()
                {
                    Position = new Vector2(24, -3),
                    Scale = 0.9f,
                    //FontFace = FontFace.FixedWidth
                };

                Flow.Alpha = Drawable.Children.Count > 64 ? 0 : 1;

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
                Drawable = d;
            }

            protected override bool OnHover(InputState state)
            {
                //d.Pinpoint = false;
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                //d.Pinpoint = false;
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                Flow.Alpha = Flow.Alpha > 0 ? 0 : 1;
                updateSpecifics();
                return true;
            }

            private void onAutoSize()
            {
                activityAutosize.FadeOutFromOne(1);
                updateSpecifics();
            }

            private void onLayout()
            {
                activityLayout.FadeOutFromOne(1);
                updateSpecifics();
            }

            private void onInvalidate()
            {
                activityInvalidate.FadeOutFromOne(1);
                updateSpecifics();
            }

            private void updateSpecifics()
            {
                previewBox.Alpha = Math.Max(0.2f, Drawable.Alpha);
                previewBox.Colour = Drawable.Colour;
                text.Text = Drawable.ToString() + (!Flow.IsVisible ? $@" ({Drawable.Children.Count} hidden children)" : string.Empty);
            }

            protected override void Update()
            {
                text.Colour = !Flow.IsVisible ? Color4.LightBlue : Color4.White;
                //text.BackgroundColour = Drawable.Pinpoint ? Color4.Purple : Color4.Transparent;

                base.Update();
            }

            public bool CheckExpiry()
            {
                if (!Drawable.IsAlive || Drawable.Parent == null)
                {
                    Expire();
                    return true;
                }

                Alpha = Drawable.IsVisible ? 1 : 0.3f;
                return false;
            }
        }
#endif
    }
}
