// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Logging;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    class LogOverlay : OverlayContainer
    {
        private FlowContainer flow;

        public LogOverlay()
        {
            //todo: use Input as font

            Width = 700;
            AutoSizeAxes = Axes.Y;
            Origin = Anchor.BottomLeft;
            Anchor = Anchor.BottomLeft;

            Margin = new MarginPadding(1);

            Masking = true;

            Children = new Drawable[]
            {
                flow = new FlowContainer
                {
                    LayoutDuration = 150,
                    LayoutEasing = EasingTypes.OutQuart,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                }

            };

            Logger.NewEntry += logger_NewEntry;

            Scheduler.AddDelayed(() =>
            {
                Logger.Log($@"testing debug overlay {Time}!");
            }, 500, true);
        }

        private void logger_NewEntry(LogEntry entry)
        {
            Schedule(() =>
            {
                var drawEntry = new DrawableLogEntry(entry) { Depth = -(float)Time };

                flow.Add(drawEntry);

                drawEntry.Position = new Vector2(-drawEntry.DrawWidth, 0);

                drawEntry.FadeInFromZero(200);
                drawEntry.Delay(200);
                drawEntry.FadeOut(entry.Message.Length * 100, EasingTypes.InQuint);
                drawEntry.Expire();
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Show();
        }

        protected override void PopIn()
        {
            FadeIn(500);
        }

        protected override void PopOut()
        {
            FadeOut(500);
        }
    }

    class DrawableLogEntry : Container
    {
        private readonly LogEntry entry;

        public DrawableLogEntry(LogEntry entry)
        {
            this.entry = entry;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;

            Margin = new MarginPadding(1);

            Color4 col = Color4.Orange;

            CornerRadius = 5;
            Masking = true;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.6f,
                },
                new Container
                {
                    Margin = new MarginPadding(3),
                    Size = new Vector2(80, 20),
                    CornerRadius = 5,
                    Masking = true,

                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = col,
                        },
                        new SpriteText
                        {

                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Margin = new MarginPadding { Left = 5, Right = 5 },
                            Text = entry.Target.ToString(),
                        }
                    }
                },
                new Container
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding { Left = 88 },

                    Children = new Drawable[]
                    {
                    new SpriteText
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        //
                        Text = entry.Message
                    }
                    }
                }
            };
        }
    }
}
