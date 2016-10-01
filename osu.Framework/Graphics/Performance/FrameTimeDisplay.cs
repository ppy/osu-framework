// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Framework.Statistics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Graphics.Performance
{
    class FrameTimeDisplay : Container
    {
        const int WIDTH = 800;
        const int HEIGHT = 100;

        const float visible_range = 20;
        const float scale = HEIGHT / visible_range;

        const float alpha_when_inactive = 0.6f;

        private TimeBar[] timeBars = new TimeBar[2];
        private BufferStack<byte> textureBufferStack;

        private static Color4[] garbageCollectColors = { Color4.Green, Color4.Yellow, Color4.Red };
        private PerformanceMonitor monitor;

        private int currentX;

        private int TimeBarIndex => currentX / WIDTH;
        private int TimeBarX => currentX % WIDTH;

        private bool processFrames = true;

        Container overlayContainer;

        FlowContainer legendContainer;
        Drawable[] legendMapping = new Drawable[(int)PerformanceCollectionType.Empty];

        private FpsDisplay fpsDisplay;

        public override string Name { get; }

        public FrameTimeDisplay(string name, PerformanceMonitor monitor)
        {
            Name = name;
            this.monitor = monitor;
            textureBufferStack = new BufferStack<byte>(timeBars.Length * WIDTH);
        }

        public override void Load()
        {
            base.Load();

            Size = new Vector2(WIDTH, HEIGHT);
            Alpha = alpha_when_inactive;

            for (int i = 0; i < timeBars.Length; ++i)
                timeBars[i] = new TimeBar();

            Children = new Drawable[]
            {
                new Container
                {
                    Masking = true,
                    RelativeSizeAxes = Axis.Both,
                    Children = timeBars
                },
                fpsDisplay = new FpsDisplay(monitor.Clock)
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                },
                overlayContainer = new Container
                {
                    RelativeSizeAxes = Axis.Both,
                    Children = new []
                    {
                        new SpriteText
                        {
                            Text = Name,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.CentreLeft,
                            Rotation = -90
                        },
                        legendContainer = new FlowContainer
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Padding = new Vector2(5, 1),
                            Children = new[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axis.Both,
                                    Colour = Color4.Black,
                                    Alpha = 0.2f
                                }
                            }
                        },
                        new SpriteText
                        {
                            Text = $@"{visible_range}ms"
                        },
                        new SpriteText
                        {
                            Text = @"0ms",
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        }
                    }
                }
            };

            foreach (PerformanceCollectionType t in Enum.GetValues(typeof(PerformanceCollectionType)))
            {
                if (t >= PerformanceCollectionType.Empty) continue;

                legendContainer.Add(legendMapping[(int)t] = new SpriteText
                {
                    Colour = getColour(t),
                    Text = t.ToString(),
                    Alpha = 0
                });
            }

            overlayContainer.FadeOut(2000, EasingTypes.InExpo);

            // Initialize background
            for (int i = 0; i < WIDTH * timeBars.Length; ++i)
            {
                currentX = i;

                TextureUpload upload = new TextureUpload(HEIGHT * 4, textureBufferStack)
                {
                    Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                };

                addArea(null, PerformanceCollectionType.Empty, HEIGHT, upload.Data);
                timeBars[TimeBarIndex].Sprite.Texture.SetData(upload);
            }
        }

        class TimeBar : Container
        {
            public Sprite Sprite;

            public override void Load()
            {
                base.Load();

                Width = WIDTH;
                Height = HEIGHT;

                Children = new[]
                {
                    Sprite = new Sprite
                    {
                        Texture = new Texture(WIDTH, HEIGHT)
                    }
                };
            }

            public override bool HandleInput => false;
        }

        private void addEvent(int type)
        {
            Box b = new Box
            {
                Origin = Anchor.TopCentre,
                Position = new Vector2(TimeBarX, type * 3),
                Colour = garbageCollectColors[type],
                Size = new Vector2(3, 3)
            };

            timeBars[TimeBarIndex].Add(b);
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
            {
                overlayContainer.FadeIn(100);
                FadeTo(1, 100);
                fpsDisplay.Counting = false;
                processFrames = false;
            }
            return base.OnKeyDown(state, args);
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
            {
                overlayContainer.FadeOut(100);
                FadeTo(alpha_when_inactive, 100);
                fpsDisplay.Counting = true;
                processFrames = true;
            }
            return base.OnKeyUp(state, args);
        }

        protected override void Update()
        {
            base.Update();

            FrameStatistics frame;
            while (monitor.PendingFrames.TryDequeue(out frame))
            {
                if (processFrames)
                {
                    foreach (int gcLevel in frame.GarbageCollections)
                        addEvent(gcLevel);

                    TimeBar timeBar = timeBars[TimeBarIndex];
                    TextureUpload upload = new TextureUpload(HEIGHT * 4, textureBufferStack)
                    {
                        Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                    };

                    int currentHeight = HEIGHT;

                    for (int i = 0; i <= (int)PerformanceCollectionType.Empty; i++)
                        currentHeight = addArea(frame, (PerformanceCollectionType)i, currentHeight, upload.Data);

                    timeBar.Sprite.Texture.SetData(upload);

                    timeBars[TimeBarIndex].MoveToX((WIDTH - TimeBarX));
                    timeBars[(TimeBarIndex + 1) % timeBars.Length].MoveToX(-TimeBarX);
                    currentX = (currentX + 1) % (timeBars.Length * WIDTH);

                    foreach (Drawable e in timeBars[(TimeBarIndex + 1) % timeBars.Length].Children)
                        if (e is Box && e.Position.X <= TimeBarX)
                            e.Expire();
                }

                monitor.FramesHeap.FreeObject(frame);
            }
        }

        private Color4 getColour(PerformanceCollectionType type)
        {
            switch (type)
            {
                default:
                case PerformanceCollectionType.Update:
                    return Color4.YellowGreen;
                case PerformanceCollectionType.Draw:
                    return Color4.BlueViolet;
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
                case PerformanceCollectionType.Empty:
                    return new Color4(0.1f, 0.1f, 0.1f, 1);
            }
        }

        private int addArea(FrameStatistics frame, PerformanceCollectionType frameTimeType, int currentHeight, byte[] textureData)
        {
            Debug.Assert(textureData.Length >= HEIGHT * 4, $"textureData is too small ({textureData.Length}) to hold area data.");

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

                bool acceptableRange = (float)currentHeight / HEIGHT > 1 - monitor.FrameAimTime / visible_range;

                float brightnessAdjust = 1;
                if (frameTimeType == PerformanceCollectionType.Empty)
                    brightnessAdjust *= (1 - i * 4 / HEIGHT / 8f);
                else if (acceptableRange)
                    brightnessAdjust *= 0.8f;

                int index = i * 4;
                textureData[index] = (byte)(255 * col.R * brightnessAdjust);
                textureData[index + 1] = (byte)(255 * col.G * brightnessAdjust);
                textureData[index + 2] = (byte)(255 * col.B * brightnessAdjust);
                textureData[index + 3] = 255;

                currentHeight--;
            }

            return currentHeight;
        }
    }
}
