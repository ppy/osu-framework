using System;
using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Timing;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using System.Collections.Concurrent;
using osu.Framework.Input;
using OpenTK.Input;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Statistics;
using System.Diagnostics;

namespace osu.Framework.Graphics.Performance
{
    class FrameTimeDisplay : Container
    {
        static Vector2 padding = new Vector2(0, 0);

        const int WIDTH = 800;
        const int HEIGHT = 100;

        const float visible_range = 20;
        const float scale = HEIGHT / visible_range;

        private Sprite[] timeBars = new Sprite[2];
        private Container[] timeBarContainers = new Container[2];
        private TextureBufferStack textureBufferStack;

        private static Color4[] garbageCollectColors = new Color4[] { Color4.Green, Color4.Yellow, Color4.Red };
        private PerformanceMonitor monitor;

        private int currentX = 0;

        private int TimeBarIndex => currentX / WIDTH;
        private int TimeBarX => currentX % WIDTH;

        private bool processFrames = true;

        public string Name
        {
            get;
            private set;
        }

        FlowContainer legendContainer;
        Drawable[] legendMapping = new Drawable[(int)PerformanceCollectionType.Empty];

        public FrameTimeDisplay(string name, PerformanceMonitor monitor)
        {
            this.Name = name;
            this.Size = new Vector2(WIDTH, HEIGHT);
            this.monitor = monitor;
            textureBufferStack = new TextureBufferStack(timeBars.Length * WIDTH);
        }

        public override void Load()
        {
            base.Load();

            Container timeBarContainer;

            Add(new MaskingContainer
            {
                Children = new [] {
                    timeBarContainer = new LargeContainer(),
                    legendContainer = new FlowContainer {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Padding = new Vector2(5, 1),
                        Children = new [] {
                            new Box {
                                SizeMode = InheritMode.XY,
                                Colour = Color4.Gray,
                                Alpha = 0.2f,
                            }
                        }
                    },
                    new SpriteText
                    {
                        Text = $@"{visible_range}ms",
                    },
                    new SpriteText
                    {
                        Text = @"0ms",
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft
                    },
                }
            });

            for (int i = 0; i < timeBars.Length; ++i)
            {
                timeBars[i] = new Sprite(new Texture(WIDTH, HEIGHT));
                timeBarContainer.Add(
                    timeBarContainers[i] = new Container
                    {
                        Children = new [] { timeBars[i] },
                        Width = WIDTH,
                        Height = HEIGHT,
                    }
                );
            }

            foreach (PerformanceCollectionType t in Enum.GetValues(typeof(PerformanceCollectionType)))
            {
                if (t >= PerformanceCollectionType.Empty) continue;

                legendContainer.Add(legendMapping[(int)t] = new SpriteText()
                {
                    Colour = getColour(t),
                    Text = t.ToString(),
                    Alpha = 0
                });

                legendContainer.FadeOut(2000, EasingTypes.InExpo);
            }

            // Initialize background
            for (int i = 0; i < WIDTH * timeBars.Length; ++i)
            {
                currentX = i;
                Sprite timeBar = timeBars[TimeBarIndex];

                TextureUpload upload = new TextureUpload(HEIGHT * 4, textureBufferStack)
                {
                    Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                };

                addArea(null, PerformanceCollectionType.Empty, HEIGHT, upload.Data);
                timeBar.Texture.SetData(upload);
            }
        }

        public void AddEvent(int type)
        {
            Box b = new Box()
            {
                Origin = Anchor.TopCentre,
                Position = new Vector2(TimeBarX, 0),
                Colour = garbageCollectColors[type],
                Size = new Vector2(3, 3),
            };

            timeBarContainers[TimeBarIndex].Add(b);
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
            {
                legendContainer.FadeIn(100);
                processFrames = false;
            }
            return base.OnKeyDown(state, args);
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            if (args.Key == Key.ControlLeft)
            {
                legendContainer.FadeOut(100);
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
                if (!processFrames)
                    continue;

                foreach (int gcLevel in frame.GarbageCollections)
                    AddEvent(gcLevel);

                Sprite timeBar = timeBars[TimeBarIndex];
                TextureUpload upload = new TextureUpload(HEIGHT * 4, textureBufferStack)
                {
                    Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                };

                int currentHeight = HEIGHT;

                for (int i = 0; i <= (int)PerformanceCollectionType.Empty; i++)
                    currentHeight = addArea(frame, (PerformanceCollectionType)i, currentHeight, upload.Data);

                timeBar.Texture.SetData(upload);

                timeBarContainers[TimeBarIndex].MoveToX((WIDTH - TimeBarX), 0);
                timeBarContainers[(TimeBarIndex + 1) % timeBars.Length].MoveToX(-TimeBarX, 0);
                currentX = (currentX + 1) % (timeBars.Length * WIDTH);

                foreach (Drawable e in timeBarContainers[(TimeBarIndex + 1) % timeBars.Length].Children)
                    if (e is Box && e.Position.X <= TimeBarX)
                        e.Expire();
            }
        }

        private Color4 getColour(PerformanceCollectionType type)
        {
            Color4 col = default(Color4);

            switch (type)
            {
                default:
                case PerformanceCollectionType.Update:
                    col = Color4.YellowGreen;
                    break;
                case PerformanceCollectionType.Draw:
                    col = Color4.BlueViolet;
                    break;
                case PerformanceCollectionType.SwapBuffer:
                    col = Color4.Red;
                    break;
#if DEBUG
                case PerformanceCollectionType.Debug:
                    col = Color4.Yellow;
                    break;
#endif
                case PerformanceCollectionType.Sleep:
                    col = Color4.DarkBlue;
                    break;
                case PerformanceCollectionType.Scheduler:
                    col = Color4.HotPink;
                    break;
                case PerformanceCollectionType.WndProc:
                    col = Color4.GhostWhite;
                    break;
                case PerformanceCollectionType.Empty:
                    col = new Color4(50, 40, 40, 180);
                    break;
            }

            return col;
        }

        private int addArea(FrameStatistics frame, PerformanceCollectionType frameTimeType, int currentHeight, byte[] textureData)
        {
            Debug.Assert(textureData.Length >= HEIGHT * 4, $"textureData is too small ({textureData.Length}) to hold area data.");

            double elapsedMilliseconds = 0;
            int drawHeight = 0;

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

                int index = i * 4;
                textureData[index] = (byte)(255 * col.R);
                textureData[index + 1] = (byte)(255 * col.G);
                textureData[index + 2] = (byte)(255 * col.B);
                textureData[index + 3] = (byte)(255 * (frameTimeType == PerformanceCollectionType.Empty ? (col.A * (1 - (int)((i * 4) / HEIGHT) / 8f)) : col.A));
                currentHeight--;
            }

            return currentHeight;
        }
    }
}
