// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Framework.Statistics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Performance
{
    class FrameStatisticsDisplay : Container, IStateful<FrameStatisticsMode>
    {
        private const int width = 800;
        private const int height = 100;

        const int amount_count_steps = 5;

        const int amount_ms_steps = 5;
        const float visible_ms_range = 20;
        const float scale = height / visible_ms_range;

        const float alpha_when_inactive = 0.6f;

        private TimeBar[] timeBars;
        private BufferStack<byte> textureBufferStack;

        private static Color4[] garbageCollectColors = { Color4.Green, Color4.Yellow, Color4.Red };
        private PerformanceMonitor monitor;

        private int currentX;

        private int timeBarIndex => currentX / width;
        private int timeBarX => currentX % width;

        private bool processFrames = true;

        Container overlayContainer;
        Drawable labelText;
        Sprite counterBarBackground;

        Container mainContainer;
        Container timeBarsContainer;

        Drawable[] legendMapping = new Drawable[(int)PerformanceCollectionType.Empty];
        Dictionary<StatisticsCounterType, CounterBar> counterBars = new Dictionary<StatisticsCounterType, CounterBar>();

        private FpsDisplay fpsDisplay;

        public override string Name { get; }

        private FrameStatisticsMode state;
        public FrameStatisticsMode State
        {
            get
            {
                return state;
            }

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
                        mainContainer.Size = new Vector2(width, height);

                        timeBarsContainer.Show();

                        labelText.Origin = Anchor.BottomCentre;
                        labelText.Rotation = -90;
                        break;
                }

                Active = true;
            }
        }

        public FrameStatisticsDisplay(GameThread thread, TextureAtlas atlas)
        {
            Name = thread.Thread.Name;
            monitor = thread.Monitor;

            Origin = Anchor.TopRight;
            AutoSizeAxes = Axes.Both;
            Alpha = alpha_when_inactive;

            bool hasCounters = false;
            for (int i = 0; i < (int)StatisticsCounterType.AmountTypes; ++i)
                if (monitor.Counters[i] != null)
                    hasCounters = true;

            Children = new Drawable[]
            {
                new Container
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
                                !hasCounters ? new Container() { Width = 2 } : new Container
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
                                            Texture = atlas.Add(1, height),
                                            RelativeSizeAxes = Axes.Both,
                                            Size = new Vector2(1, 1),
                                        },
                                        new FlowContainer
                                        {
                                            Direction = FlowDirection.HorizontalOnly,
                                            AutoSizeAxes = Axes.X,
                                            RelativeSizeAxes = Axes.Y,
                                            Children = from StatisticsCounterType t in Enum.GetValues(typeof(StatisticsCounterType))
                                                       where t < StatisticsCounterType.AmountTypes && monitor.Counters[(int)t] != null
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
                            Size = new Vector2(width, height),
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
                                    Children = new []
                                    {
                                        new FlowContainer
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            AutoSizeAxes = Axes.Both,
                                            Spacing = new Vector2(5, 1),
                                            Padding = new MarginPadding { Right = 5 },
                                            Children = from PerformanceCollectionType t in Enum.GetValues(typeof(PerformanceCollectionType))
                                                       where t < PerformanceCollectionType.Empty
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
                },
            };

            textureBufferStack = new BufferStack<byte>(timeBars.Length * width);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            //initialise background
            byte[] column = new byte[height * 4];
            byte[] fullBackground = new byte[width * height * 4];

            addArea(null, PerformanceCollectionType.Empty, height, column, amount_ms_steps);

            for (int i = 0; i < height; i++)
                for (int k = 0; k < width; k++)
                    Buffer.BlockCopy(column, i * 4, fullBackground, i * width * 4 + k * 4, 4);

            addArea(null, PerformanceCollectionType.Empty, height, column, amount_count_steps);

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
                Colour = garbageCollectColors[type],
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

                if (active)
                {
                    overlayContainer.FadeOut(100);
                    FadeTo(alpha_when_inactive, 100);
                    fpsDisplay.Counting = true;
                    processFrames = true;

                    foreach (CounterBar bar in counterBars.Values)
                        bar.Active = true;
                }
                else
                {
                    overlayContainer.FadeIn(100);
                    FadeIn(100);
                    fpsDisplay.Counting = false;
                    processFrames = false;

                    foreach (CounterBar bar in counterBars.Values)
                        bar.Active = false;
                }
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
            TextureUpload upload = new TextureUpload(height * 4, textureBufferStack)
            {
                Bounds = new Rectangle(timeBarX, 0, 1, height)
            };

            int currentHeight = height;

            for (int i = 0; i <= (int)PerformanceCollectionType.Empty; i++)
                currentHeight = addArea(frame, (PerformanceCollectionType)i, currentHeight, upload.Data, amount_ms_steps);

            timeBar.Sprite.Texture.SetData(upload);

            timeBars[timeBarIndex].MoveToX((width - timeBarX));
            timeBars[(timeBarIndex + 1) % timeBars.Length].MoveToX(-timeBarX);
            currentX = (currentX + 1) % (timeBars.Length * width);

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

            FrameStatistics frame;
            while (monitor.PendingFrames.TryDequeue(out frame))
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
                case PerformanceCollectionType.Work:
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
                case PerformanceCollectionType.Empty:
                    return new Color4(0.1f, 0.1f, 0.1f, 1);
            }
        }

        private Color4 getColour(StatisticsCounterType type)
        {
            switch (type)
            {
                default:
                case StatisticsCounterType.VBufOverflow:
                    return Color4.Yellow;

                case StatisticsCounterType.Invalidations:
                case StatisticsCounterType.TextureBinds:
                    return Color4.BlueViolet;

                case StatisticsCounterType.DrawCalls:
                case StatisticsCounterType.Refreshes:
                    return Color4.YellowGreen;

                case StatisticsCounterType.DrawNodeCtor:
                case StatisticsCounterType.VerticesDraw:
                    return Color4.HotPink;

                case StatisticsCounterType.DrawNodeAppl:
                case StatisticsCounterType.VerticesUpl:
                    return Color4.Red;

                case StatisticsCounterType.ScheduleInvk:
                case StatisticsCounterType.KiloPixels:
                    return Color4.Cyan;
            }
        }

        private int addArea(FrameStatistics frame, PerformanceCollectionType frameTimeType, int currentHeight, byte[] textureData, int amountSteps)
        {
            Debug.Assert(textureData.Length >= height * 4, $"textureData is too small ({textureData.Length}) to hold area data.");

            double elapsedMilliseconds;
            int drawHeight;

            if (frameTimeType == PerformanceCollectionType.Empty)
                drawHeight = currentHeight;
            else if (frame.CollectedTimes.TryGetValue(frameTimeType, out elapsedMilliseconds))
            {
                legendMapping[(int)frameTimeType].Alpha = 1;
                drawHeight = (int)(elapsedMilliseconds * scale);
            }
            else
                return currentHeight;

            Color4 col = getColour(frameTimeType);

            for (int i = currentHeight - 1; i >= 0; --i)
            {
                if (drawHeight-- == 0) break;

                bool acceptableRange = (float)currentHeight / height > 1 - monitor.FrameAimTime / visible_ms_range;

                float brightnessAdjust = 1;
                if (frameTimeType == PerformanceCollectionType.Empty)
                    brightnessAdjust *= 1 - i * amountSteps / height / 8f;
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

        class TimeBar : Container
        {
            public Sprite Sprite;

            public TimeBar(TextureAtlas atlas)
            {
                Size = new Vector2(width, height);
                Children = new[]
                {
                    Sprite = new Sprite()
                };

                Sprite.Texture = atlas.Add(width, height);
            }

            public override bool HandleInput => false;
        }

        class CounterBar : Container
        {
            private Box box;
            private SpriteText text;

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
                        ResizeTo(new Vector2(bar_width, 1), 100);
                        text.FadeOut(100);
                    }
                    else
                    {
                        ResizeTo(new Vector2(bar_width + text.TextSize + 2, 1), 100);
                        text.FadeIn(100);
                        text.Text = string.Format(@"{0}: {1}", Label, (long)Math.Round(Math.Pow(10, box.Height * amount_count_steps) - 1));
                    }
                }
            }

            private double height;
            private double velocity;
            private const double acceleration = 0.000001;
            private const float bar_width = 6;

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

            public long Value
            {
                set
                {
                    height = Math.Log10(value + 1) / amount_count_steps;
                }
            }

            protected override void Update()
            {
                base.Update();

                if (!Active)
                    return;

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
