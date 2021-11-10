// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;
using osuTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Performance
{
    internal class FrameStatisticsDisplay : Container, IStateful<FrameStatisticsMode>
    {
        internal const int HEIGHT = 100;

        protected const int WIDTH = 800;

        private const int amount_count_steps = 5;

        private const int amount_ms_steps = 5;
        private const float visible_ms_range = 20;
        private const float scale = HEIGHT / visible_ms_range;

        private const float alpha_when_active = 0.75f;

        private readonly TimeBar[] timeBars;

        private static readonly Color4[] garbage_collect_colors = { Color4.Green, Color4.Yellow, Color4.Red };
        private readonly PerformanceMonitor monitor;

        private int currentX;

        private int timeBarIndex => currentX / WIDTH;
        private int timeBarX => currentX % WIDTH;

        private readonly Container overlayContainer;
        private readonly Drawable labelText;
        private readonly Sprite counterBarBackground;

        private readonly Container mainContainer;
        private readonly Container timeBarsContainer;

        private readonly ArrayPool<Rgba32> uploadPool;

        private readonly Drawable[] legendMapping = new Drawable[FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES];
        private readonly Dictionary<StatisticsCounterType, CounterBar> counterBars = new Dictionary<StatisticsCounterType, CounterBar>();

        private readonly FrameTimeDisplay frameTimeDisplay;

        private FrameStatisticsMode state;

        public event Action<FrameStatisticsMode> StateChanged;

        public FrameStatisticsMode State
        {
            get => state;
            set
            {
                if (state == value) return;

                state = value;

                switch (state)
                {
                    case FrameStatisticsMode.Minimal:
                        mainContainer.AutoSizeAxes = Axes.Both;

                        timeBarsContainer.Hide();

                        labelText.Origin = Anchor.CentreRight;
                        labelText.Rotation = 0;
                        break;

                    case FrameStatisticsMode.Full:
                        mainContainer.AutoSizeAxes = Axes.None;
                        mainContainer.Size = new Vector2(WIDTH, HEIGHT);

                        timeBarsContainer.Show();

                        labelText.Origin = Anchor.BottomCentre;
                        labelText.Rotation = -90;
                        break;
                }

                Running = state != FrameStatisticsMode.None;
                Expanded = false;

                StateChanged?.Invoke(State);
            }
        }

        public FrameStatisticsDisplay(GameThread thread, ArrayPool<Rgba32> uploadPool)
        {
            Name = thread.Name;
            monitor = thread.Monitor;
            this.uploadPool = uploadPool;

            Origin = Anchor.TopRight;
            AutoSizeAxes = Axes.Both;
            Alpha = alpha_when_active;

            int colour = 0;

            bool hasCounters = monitor.ActiveCounters.Any(b => b);
            Child = new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new[]
                {
                    new Container
                    {
                        Origin = Anchor.TopRight,
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Children = new[]
                        {
                            labelText = new SpriteText
                            {
                                Text = Name,
                                Origin = Anchor.BottomCentre,
                                Anchor = Anchor.CentreLeft,
                                Rotation = -90,
                                Font = FrameworkFont.Regular,
                            },
                            !hasCounters
                                ? new Container { Width = 2 }
                                : new Container
                                {
                                    Masking = true,
                                    CornerRadius = 5,
                                    AutoSizeAxes = Axes.X,
                                    RelativeSizeAxes = Axes.Y,
                                    Margin = new MarginPadding { Right = 2, Left = 2 },
                                    Children = new Drawable[]
                                    {
                                        counterBarBackground = new Sprite
                                        {
                                            Texture = new Texture(1, HEIGHT, true),
                                            RelativeSizeAxes = Axes.Both,
                                            Size = new Vector2(1, 1),
                                        },
                                        new FillFlowContainer
                                        {
                                            Direction = FillDirection.Horizontal,
                                            AutoSizeAxes = Axes.X,
                                            RelativeSizeAxes = Axes.Y,
                                            ChildrenEnumerable =
                                                from StatisticsCounterType t in Enum.GetValues(typeof(StatisticsCounterType))
                                                where monitor.ActiveCounters[(int)t]
                                                select counterBars[t] = new CounterBar
                                                {
                                                    Colour = getColour(colour++),
                                                    Label = t.ToString(),
                                                },
                                        },
                                    }
                                }
                        }
                    },
                    mainContainer = new Container
                    {
                        Size = new Vector2(WIDTH, HEIGHT),
                        Children = new[]
                        {
                            timeBarsContainer = new Container
                            {
                                Masking = true,
                                CornerRadius = 5,
                                RelativeSizeAxes = Axes.Both,
                                Children = timeBars = new[]
                                {
                                    new TimeBar(),
                                    new TimeBar(),
                                },
                            },
                            frameTimeDisplay = new FrameTimeDisplay(monitor.Clock)
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                            },
                            overlayContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.TopRight,
                                        AutoSizeAxes = Axes.Both,
                                        Spacing = new Vector2(5, 1),
                                        Padding = new MarginPadding { Right = 5 },
                                        ChildrenEnumerable =
                                            from PerformanceCollectionType t in Enum.GetValues(typeof(PerformanceCollectionType))
                                            select legendMapping[(int)t] = new SpriteText
                                            {
                                                Colour = getColour(t),
                                                Text = t.ToString(),
                                                Alpha = 0,
                                                Font = FrameworkFont.Regular,
                                            },
                                    },
                                    new SpriteText
                                    {
                                        Padding = new MarginPadding { Left = 4 },
                                        Text = $@"{visible_ms_range}ms",
                                        Font = FrameworkFont.Regular,
                                    },
                                    new SpriteText
                                    {
                                        Padding = new MarginPadding { Left = 4 },
                                        Text = @"0ms",
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        Font = FrameworkFont.Regular,
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            //initialise background
            var columnUpload = new ArrayPoolTextureUpload(1, HEIGHT);
            var fullBackground = new Image<Rgba32>(WIDTH, HEIGHT);

            addArea(null, null, HEIGHT, amount_ms_steps, columnUpload);

            for (int i = 0; i < HEIGHT; i++)
            {
                for (int k = 0; k < WIDTH; k++)
                    fullBackground[k, i] = columnUpload.RawData[i];
            }

            addArea(null, null, HEIGHT, amount_count_steps, columnUpload);

            counterBarBackground?.Texture.SetData(columnUpload);
            Schedule(() =>
            {
                foreach (var t in timeBars)
                    t.Sprite.Texture.SetData(new TextureUpload(fullBackground.Clone()));
            });
        }

        private void addEvent(int type)
        {
            Box b = new Box
            {
                Origin = Anchor.TopCentre,
                Position = new Vector2(timeBarX, type * 3),
                Colour = garbage_collect_colors[type],
                Size = new Vector2(3, 3)
            };

            timeBars[timeBarIndex].Add(b);
        }

        private bool running = true;

        public bool Running
        {
            get => running;
            set
            {
                if (running == value) return;

                running = value;

                frameTimeDisplay.Counting = running;

                // clear all pending frames on state change.
                monitor.PendingFrames.Clear();
            }
        }

        private bool expanded;

        public bool Expanded
        {
            get => expanded;
            set
            {
                value &= state == FrameStatisticsMode.Full;

                if (expanded == value) return;

                expanded = value;

                overlayContainer.FadeTo(expanded ? 1 : 0, 100);
                this.FadeTo(expanded ? 1 : alpha_when_active, 100);

                foreach (CounterBar bar in counterBars.Values)
                    bar.Expanded = expanded;
            }
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            switch (e.Key)
            {
                case Key.ControlLeft:
                    Expanded = true;
                    break;

                case Key.ShiftLeft:
                    Running = false;
                    break;
            }

            return base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            switch (e.Key)
            {
                case Key.ControlLeft:
                    Expanded = false;
                    break;

                case Key.ShiftLeft:
                    Running = true;
                    break;
            }

            base.OnKeyUp(e);
        }

        private void applyFrameGC(FrameStatistics frame)
        {
            foreach (int gcLevel in frame.GarbageCollections)
                addEvent(gcLevel);
        }

        private void applyFrameTime(FrameStatistics frame)
        {
            TimeBar timeBar = timeBars[timeBarIndex];
            var upload = new ArrayPoolTextureUpload(1, HEIGHT, uploadPool)
            {
                Bounds = new RectangleI(timeBarX, 0, 1, HEIGHT)
            };

            int currentHeight = HEIGHT;

            for (int i = 0; i < FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES; i++)
                currentHeight = addArea(frame, (PerformanceCollectionType)i, currentHeight, amount_ms_steps, upload);
            addArea(frame, null, currentHeight, amount_ms_steps, upload);

            timeBar.Sprite.Texture.SetData(upload);

            timeBars[timeBarIndex].X = WIDTH - timeBarX;
            timeBars[(timeBarIndex + 1) % timeBars.Length].X = -timeBarX;
            currentX = (currentX + 1) % (timeBars.Length * WIDTH);

            foreach (Drawable e in timeBars[(timeBarIndex + 1) % timeBars.Length])
            {
                if (e is Box && e.DrawPosition.X <= timeBarX)
                    e.Expire();
            }
        }

        private void applyFrameCounts(FrameStatistics frame)
        {
            foreach (var pair in frame.Counts)
                counterBars[pair.Key].Value = pair.Value;
        }

        private void applyFrame(FrameStatistics frame)
        {
            if (state == FrameStatisticsMode.Full)
            {
                applyFrameGC(frame);
                applyFrameTime(frame);
            }

            applyFrameCounts(frame);
        }

        protected override void Update()
        {
            base.Update();

            if (running)
            {
                while (monitor.PendingFrames.TryDequeue(out FrameStatistics frame))
                {
                    applyFrame(frame);
                    frameTimeDisplay.NewFrame(frame);
                    monitor.FramesPool.Return(frame);
                }
            }
        }

        private Color4 getColour(PerformanceCollectionType type)
        {
            switch (type)
            {
                default:
                    return Color4.YellowGreen;

                case PerformanceCollectionType.SwapBuffer:
                    return Color4.Red;
#if DEBUG
                case PerformanceCollectionType.Debug:
                    return Color4.Yellow;
#endif
                case PerformanceCollectionType.Sleep:
                    return Color4.DarkBlue;

                case PerformanceCollectionType.Scheduler:
                    return Color4.HotPink;

                case PerformanceCollectionType.WndProc:
                    return Color4.GhostWhite;

                case PerformanceCollectionType.GLReset:
                    return Color4.Cyan;
            }
        }

        private Color4 getColour(int index)
        {
            const int colour_count = 7;

            switch (index % colour_count)
            {
                default:
                    return Color4.BlueViolet;

                case 1:
                    return Color4.YellowGreen;

                case 2:
                    return Color4.HotPink;

                case 3:
                    return Color4.Red;

                case 4:
                    return Color4.Cyan;

                case 5:
                    return Color4.Yellow;

                case 6:
                    return Color4.SkyBlue;
            }
        }

        private int addArea(FrameStatistics frame, PerformanceCollectionType? frameTimeType, int currentHeight, int amountSteps, ArrayPoolTextureUpload columnUpload)
        {
            int drawHeight;

            if (!frameTimeType.HasValue)
                drawHeight = currentHeight;
            else if (frame.CollectedTimes.TryGetValue(frameTimeType.Value, out double elapsedMilliseconds))
            {
                legendMapping[(int)frameTimeType].Alpha = 1;
                drawHeight = (int)(elapsedMilliseconds * scale);
            }
            else
                return currentHeight;

            Color4 col = frameTimeType.HasValue ? getColour(frameTimeType.Value) : new Color4(0.1f, 0.1f, 0.1f, 1);

            for (int i = currentHeight - 1; i >= 0; --i)
            {
                if (drawHeight-- == 0) break;

                bool acceptableRange = (float)currentHeight / HEIGHT > 1 - monitor.FrameAimTime / visible_ms_range;

                float brightnessAdjust = 1;

                if (!frameTimeType.HasValue)
                {
                    int step = amountSteps / HEIGHT;
                    brightnessAdjust *= 1 - i * step / 8f;
                }
                else if (acceptableRange)
                    brightnessAdjust *= 0.8f;

                columnUpload.RawData[i] = new Rgba32(col.R * brightnessAdjust, col.G * brightnessAdjust, col.B * brightnessAdjust, col.A);

                currentHeight--;
            }

            return currentHeight;
        }

        private class TimeBar : Container
        {
            public readonly Sprite Sprite;

            public TimeBar()
            {
                Size = new Vector2(WIDTH, HEIGHT);
                Child = Sprite = new Sprite();

                Sprite.Texture = new Texture(WIDTH, HEIGHT, true) { TextureGL = { BypassTextureUploadQueueing = true } };
            }
        }

        private class CounterBar : Container
        {
            private readonly Box box;
            private readonly SpriteText text;

            public string Label;

            private bool expanded;

            public bool Expanded
            {
                get => expanded;
                set
                {
                    if (expanded == value) return;

                    expanded = value;

                    if (expanded)
                    {
                        this.ResizeTo(new Vector2(bar_width + text.Font.Size + 2, 1), 100);
                        text.FadeIn(100);
                    }
                    else
                    {
                        this.ResizeTo(new Vector2(bar_width, 1), 100);
                        text.FadeOut(100);
                    }
                }
            }

            private double height;
            private double velocity;
            private const double acceleration = 0.000001;
            private const float bar_width = 6;

            private long value;

            public long Value
            {
                set
                {
                    this.value = value;
                    height = Math.Log10(value + 1) / amount_count_steps;
                }
            }

            public CounterBar()
            {
                Size = new Vector2(bar_width, 1);
                RelativeSizeAxes = Axes.Y;

                Children = new Drawable[]
                {
                    text = new SpriteText
                    {
                        Origin = Anchor.BottomLeft,
                        Anchor = Anchor.BottomRight,
                        Rotation = -90,
                        Position = new Vector2(-bar_width - 1, 0),
                        Font = FrameworkFont.Regular.With(size: 16),
                    },
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Size = new Vector2(bar_width, 0),
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                double elapsedTime = Time.Elapsed;
                double movement = velocity * Time.Elapsed + 0.5 * acceleration * elapsedTime * elapsedTime;
                double newHeight = Math.Max(height, box.Height - movement);
                box.Height = (float)newHeight;

                if (newHeight <= height)
                    velocity = 0;
                else
                    velocity += Time.Elapsed * acceleration;

                if (expanded)
                    text.Text = $@"{Label}: {NumberFormatter.PrintWithSiSuffix(value)}";
            }
        }
    }
}
