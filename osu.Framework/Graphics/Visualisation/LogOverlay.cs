// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Development;
using osu.Framework.Timing;
using osuTK.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    internal class LogOverlay : OverlayContainer
    {
        private readonly FillFlowContainer flow;

        protected override bool BlockPositionalInput => false;

        private StopwatchClock clock;

        private readonly Box box;

        private const float background_alpha = 0.6f;

        public LogOverlay()
        {
            //todo: use Input as font

            Width = 700;
            AutoSizeAxes = Axes.Y;

            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;

            Margin = new MarginPadding(1);

            Masking = true;

            Children = new Drawable[]
            {
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = background_alpha,
                },
                flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                }
            };
        }

        protected override void LoadComplete()
        {
            // custom clock is used to adjust log display speed (to freeze log display with a key).
            Clock = new FramedClock(clock = new StopwatchClock(true));

            base.LoadComplete();

            addEntry(new LogEntry
            {
                Level = LogLevel.Important,
                Message = "The debug log overlay is currently being displayed. You can toggle with Ctrl+F10 at any point.",
                Target = LoggingTarget.Information,
            });
        }

        private int logPosition;

        private void addEntry(LogEntry entry)
        {
            if (!DebugUtils.IsDebugBuild && entry.Level <= LogLevel.Verbose)
                return;

            int pos = Interlocked.Increment(ref logPosition);

            Schedule(() =>
            {
                const int display_length = 4000;

                LoadComponentAsync(new DrawableLogEntry(entry), drawEntry =>
                {
                    flow.Insert(pos, drawEntry);

                    drawEntry.FadeInFromZero(800, Easing.OutQuint).Delay(display_length).FadeOut(800, Easing.InQuint);
                    drawEntry.Expire();
                });
            });
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!e.Repeat)
                setHoldState(e.Key == Key.ControlLeft || e.Key == Key.ControlRight);

            return base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            if (!e.ControlPressed)
                setHoldState(false);
            base.OnKeyUp(e);
        }

        private void setHoldState(bool controlPressed)
        {
            box.Alpha = controlPressed ? 1 : background_alpha;
            if (clock != null) clock.Rate = controlPressed ? 0 : 1;
        }

        protected override void PopIn()
        {
            Logger.NewEntry += addEntry;
            this.FadeIn(100);
        }

        protected override void PopOut()
        {
            Logger.NewEntry -= addEntry;
            setHoldState(false);
            this.FadeOut(100);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            Logger.NewEntry -= addEntry;
        }
    }

    internal class DrawableLogEntry : Container
    {
        private const float target_box_width = 65;

        private const float font_size = 14;

        public DrawableLogEntry(LogEntry entry)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Color4 col = getColourForEntry(entry);

            Children = new Drawable[]
            {
                new Container
                {
                    //log target coloured box
                    Margin = new MarginPadding(3),
                    Size = new Vector2(target_box_width, font_size),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
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
                            Shadow = true,
                            ShadowColour = Color4.Black,
                            Margin = new MarginPadding { Left = 5, Right = 5 },
                            Font = FrameworkFont.Regular.With(size: font_size),
                            Text = entry.Target?.ToString() ?? entry.LoggerName,
                        }
                    }
                },
                new Container
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding { Left = target_box_width + 10 },
                    Child = new SpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = FrameworkFont.Regular.With(size: font_size),
                        Text = entry.Message
                    }
                }
            };
        }

        private Color4 getColourForEntry(LogEntry entry)
        {
            switch (entry.Target)
            {
                case LoggingTarget.Runtime:
                    return Color4.YellowGreen;

                case LoggingTarget.Network:
                    return Color4.BlueViolet;

                case LoggingTarget.Performance:
                    return Color4.HotPink;

                case LoggingTarget.Information:
                    return Color4.CadetBlue;

                default:
                    return Color4.Cyan;
            }
        }
    }
}
