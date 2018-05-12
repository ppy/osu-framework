// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Framework.Graphics.Performance
{
    internal class FrameStatisticsDisplay : Container, IStateful<FrameStatisticsMode>
    {
        protected const int WIDTH = 800;
        protected const int HEIGHT = 100;

        private const int amount_count_steps = 5;

        private const int amount_ms_steps = 5;
        private const float visible_ms_range = 20;
        private const float scale = HEIGHT / visible_ms_range;

        private const float alpha_when_active = 0.75f;

        private readonly TimeBar[] timeBars;
        private readonly BufferStack<byte> textureBufferStack;

        private static readonly Color4[] garbage_collect_colors = { Color4.Green, Color4.Yellow, Color4.Red };
        private readonly PerformanceMonitor monitor;

        private int currentX;

        private int timeBarIndex => currentX / WIDTH;
        private int timeBarX => currentX % WIDTH;

        private bool processFrames = true;

        private readonly Container overlayContainer;
        private readonly Drawable labelText;
        private readonly Sprite counterBarBackground;

        private readonly Container mainContainer;
        private readonly Container timeBarsContainer;

        private readonly Drawable[] legendMapping = new Drawable[FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES];
        private readonly Dictionary<StatisticsCounterType, CounterBar> counterBars = new Dictionary<StatisticsCounterType, CounterBar>();

        private readonly FpsDisplay fpsDisplay;

        private FrameStatisticsMode state;

        public event Action<FrameStatisticsMode> StateChanged;

        public FrameStatisticsMode State
        {
            get { return state; }

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

                Active = true;

                StateChanged?.Invoke(State);
            }
        }

        public FrameStatisticsDisplay(GameThread thread, TextureAtlas atlas)
        {
            Name = thread.Name;
            monitor = thread.Monitor;

            Origin = Anchor.TopRight;
            AutoSizeAxes = Axes.Both;
            Alpha = alpha_when_active;

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
                                            Texture = atlas.Add(1, HEIGHT),
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
                                                    Colour = getColour(t),
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
                                    new TimeBar(atlas),
                                    new TimeBar(atlas),
                                },
                            },
                            fpsDisplay = new FpsDisplay(monitor.Clock)
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                            },
                            overlayContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0,
                                Children = new[]
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
                                                Alpha = 0
                                            },
                                    },
                                    new SpriteText
                                    {
                                        Padding = new MarginPadding { Left = 4 },
                                        Text = $@"{visible_ms_range}ms"
                                    },
                                    new SpriteText
                                    {
                                        Padding = new MarginPadding { Left = 4 },
                                        Text = @"0ms",
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft
                                    }
                                }
                            }
                        }
                    }
                }
            };

            textureBufferStack = new BufferStack<byte>(timeBars.Length * WIDTH);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            //initialise background
            byte[] column = new byte[HEIGHT * 4];
            byte[] fullBackground = new byte[WIDTH * HEIGHT * 4];

            addArea(null, null, HEIGHT, column, amount_ms_steps);

            for (int i = 0; i < HEIGHT; i++)
                for (int k = 0; k < WIDTH; k++)
                    Buffer.BlockCopy(column, i * 4, fullBackground, i * WIDTH * 4 + k * 4, 4);

            addArea(null, null, HEIGHT, column, amount_count_steps);

            counterBarBackground?.Texture.SetData(new TextureUpload(column));
            Schedule(() =>
            {
                foreach (var t in timeBars)
                    t.Sprite.Texture.SetData(new TextureUpload(fullBackground));
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

        private bool active = true;

        public bool Active
        {
            get { return active; }

            set
            {
                if (active == value) return;

                active = value || state != FrameStatisticsMode.Full;

                overlayContainer.FadeTo(active ? 0 : 1, 100);
                this.FadeTo(active ? alpha_when_active : 1, 100);
                fpsDisplay.Counting = active;
                processFrames = active;
                foreach (CounterBar bar in counterBars.Values)
                    bar.Active = active;
            }
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
                Active = false;
            return base.OnKeyDown(state, args);
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
                Active = true;
            return base.OnKeyUp(state, args);
        }

        private void applyFrameGC(FrameStatistics frame)
        {
            foreach (int gcLevel in frame.GarbageCollections)
                addEvent(gcLevel);
        }

        private void applyFrameTime(FrameStatistics frame)
        {
            TimeBar timeBar = timeBars[timeBarIndex];
            TextureUpload upload = new TextureUpload(HEIGHT * 4, textureBufferStack)
            {
                Bounds = new RectangleI(timeBarX, 0, 1, HEIGHT)
            };

            int currentHeight = HEIGHT;

            for (int i = 0; i < FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES; i++)
                currentHeight = addArea(frame, (PerformanceCollectionType)i, currentHeight, upload.Data, amount_ms_steps);
            addArea(frame, null, currentHeight, upload.Data, amount_ms_steps);

            timeBar.Sprite.Texture.SetData(upload);

            timeBars[timeBarIndex].MoveToX(WIDTH - timeBarX);
            timeBars[(timeBarIndex + 1) % timeBars.Length].MoveToX(-timeBarX);
            currentX = (currentX + 1) % (timeBars.Length * WIDTH);

            foreach (Drawable e in timeBars[(timeBarIndex + 1) % timeBars.Length].Children)
                if (e is Box && e.DrawPosition.X <= timeBarX)
                    e.Expire();
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

            while (monitor.PendingFrames.TryDequeue(out FrameStatistics frame))
            {
                if (processFrames)
                    applyFrame(frame);

                monitor.FramesHeap.FreeObject(frame);
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

        private Color4 getColour(StatisticsCounterType type)
        {
            switch (type)
            {
                default:
                    return Color4.Yellow;

                case StatisticsCounterType.VBufBinds:
                    return Color4.SkyBlue;

                case StatisticsCounterType.Invalidations:
                case StatisticsCounterType.TextureBinds:
                case StatisticsCounterType.TasksRun:
                case StatisticsCounterType.MouseEvents:
                    return Color4.BlueViolet;

                case StatisticsCounterType.DrawCalls:
                case StatisticsCounterType.Refreshes:
                case StatisticsCounterType.Tracks:
                case StatisticsCounterType.KeyEvents:
                    return Color4.YellowGreen;

                case StatisticsCounterType.DrawNodeCtor:
                case StatisticsCounterType.VerticesDraw:
                case StatisticsCounterType.Samples:
                case StatisticsCounterType.JoystickEvents:
                    return Color4.HotPink;

                case StatisticsCounterType.DrawNodeAppl:
                case StatisticsCounterType.VerticesUpl:
                case StatisticsCounterType.SChannels:
                    return Color4.Red;

                case StatisticsCounterType.ScheduleInvk:
                case StatisticsCounterType.Pixels:
                case StatisticsCounterType.Components:
                    return Color4.Cyan;
            }
        }

        private int addArea(FrameStatistics frame, PerformanceCollectionType? frameTimeType, int currentHeight, byte[] textureData, int amountSteps)
        {
            Trace.Assert(textureData.Length >= HEIGHT * 4, $"textureData is too small ({textureData.Length}) to hold area data.");

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

                int index = i * 4;
                textureData[index] = (byte)(255 * col.R * brightnessAdjust);
                textureData[index + 1] = (byte)(255 * col.G * brightnessAdjust);
                textureData[index + 2] = (byte)(255 * col.B * brightnessAdjust);
                textureData[index + 3] = (byte)(255 * col.A);

                currentHeight--;
            }

            return currentHeight;
        }

        private class TimeBar : Container
        {
            public readonly Sprite Sprite;

            public TimeBar(TextureAtlas atlas)
            {
                Size = new Vector2(WIDTH, HEIGHT);
                Child = Sprite = new Sprite();

                Sprite.Texture = atlas.Add(WIDTH, HEIGHT);
            }

            public override bool HandleKeyboardInput => false;
            public override bool HandleMouseInput => false;
        }

        private class CounterBar : Container
        {
            private readonly Box box;
            private readonly SpriteText text;

            public string Label;

            private bool active;

            public bool Active
            {
                get { return active; }
                set
                {
                    if (active == value)
                        return;

                    active = value;

                    if (active)
                    {
                        this.ResizeTo(new Vector2(bar_width, 1), 100);
                        text.FadeOut(100);
                    }
                    else
                    {
                        this.ResizeTo(new Vector2(bar_width + text.TextSize + 2, 1), 100);
                        text.FadeIn(100);
                        text.Text = $@"{Label}: {NumberFormatter.PrintWithSiSuffix(this.value)}";
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
                        TextSize = 16,
                    },
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Size = new Vector2(bar_width, 0),
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                    }
                };

                Active = true;
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
            }
        }
    }
}
