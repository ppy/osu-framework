// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Visualisation
{
    internal class LogOverlay : OverlayContainer
    {
        private readonly FillFlowContainer flow;

        private Bindable<bool> enabled;

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
                flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                }
            };

            Logger.NewEntry += logger_NewEntry;
        }

        private void logger_NewEntry(LogEntry entry)
        {
#if !DEBUG
            if (entry.Level <= LogLevel.Verbose)
                return;
#endif

            Schedule(() =>
            {
                var drawEntry = new DrawableLogEntry(entry);

                flow.Add(drawEntry);

                drawEntry.Position = new Vector2(-drawEntry.DrawWidth, 0);

                drawEntry.FadeInFromZero(200);
                using (drawEntry.Delay(200))
                {
                    drawEntry.FadeOut(entry.Message.Length * 100, EasingTypes.InQuint);
                    drawEntry.Expire();
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            enabled = config.GetBindable<bool>(FrameworkSetting.ShowLogOverlay);
            State = enabled.Value ? Visibility.Visible : Visibility.Hidden;
        }

        protected override void PopIn()
        {
            enabled.Value = true;
            FadeIn(500);
        }

        protected override void PopOut()
        {
            enabled.Value = false;
            FadeOut(500);
        }
    }

    internal class DrawableLogEntry : Container
    {
        private const float target_box_width = 90;

        public DrawableLogEntry(LogEntry entry)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Margin = new MarginPadding(1);

            Color4 col = getColourForEntry(entry);

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
                    //log target coloured box
                    Margin = new MarginPadding(3),
                    Size = new Vector2(target_box_width, 20),
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
                    Padding = new MarginPadding { Left = target_box_width + 10 },
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Text = entry.Message
                        }
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
                case LoggingTarget.Tournament:
                    return Color4.Yellow;
                case LoggingTarget.Performance:
                    return Color4.HotPink;
                case LoggingTarget.Debug:
                    return Color4.DarkBlue;
                default:
                    return Color4.Cyan;
            }
        }
    }
}
