using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Drawing;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using System.Collections.Concurrent;

namespace osu.Framework.Graphics.Performance
{
    class FrameTimeDisplay : Container
    {
        static Vector2 padding = new Vector2(0, 0);
        const int WIDTH = 800;
        const int HEIGHT = 100;

        //PerformanceMonitor monitor;

        const float visible_range = 20;
        const float scale = HEIGHT / visible_range;

        private Sprite[] timeBars = new Sprite[2];

        private AutoSizeContainer timeBarContainer;

        private byte[] textureData = new byte[HEIGHT * 4];

        private static Color4[] garbageCollectColors = new Color4[] { Color4.Green, Color4.Yellow, Color4.Red };
        private int[] lastAmountGarbageCollects = new int[3];
        private PerformanceMonitor monitor;

        private int currentX = 0;
        private int TimeBarIndex
        {
            get
            {
                return currentX / WIDTH;
            }
        }

        private int TimeBarX
        {
            get
            {
                return currentX % WIDTH;
            }
        }

        FlowContainer legendSprites;
        List<Drawable> eventSprites = new List<Drawable>();

        public FrameTimeDisplay(string name, PerformanceMonitor monitor)
        {
            Size = new Vector2(WIDTH, HEIGHT);
            this.monitor = monitor;
        }

        public override void Load()
        {
            base.Load();

            Add(new MaskingContainer
            {
                Children = new Drawable[] {
                    timeBarContainer = new AutoSizeContainer(),
                    legendSprites = new FlowContainer {
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
                        TextSize = 14
                    },
                    new SpriteText
                    {
                        Text = @"0ms",
                        TextSize = 14,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft
                    },
                }
            });

            for (int i = 0; i < timeBars.Length; ++i)
            {
                timeBars[i] = new Sprite(new Texture(WIDTH, HEIGHT));
                timeBarContainer.Add(timeBars[i]);
            }

            foreach (FrameTimeType t in Enum.GetValues(typeof(FrameTimeType)))
            {
                if (t >= FrameTimeType.Empty) continue;

                SpriteText text = new SpriteText()
                {
                    Colour = getColour(t),
                    Text = t.ToString(),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft
                };
                legendSprites.Add(text);
            }

            //Add(legendSprites);

            //SetVisibleArea(new RectangleF(GameBase.WindowManager.Width - WIDTH, GameBase.WindowManager.Height - HEIGHT + 1, WIDTH + 1, HEIGHT));

            // Initialize background
            for (int i = 0; i < WIDTH * timeBars.Length; ++i)
            {
                currentX = i;
                Sprite timeBar = timeBars[TimeBarIndex];

                addArea(null, FrameTimeType.Empty, HEIGHT);
                timeBar.Texture.SetData(new TextureUpload(textureData)
                {
                    Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                });
            }
        }

        public void AddEvent(Color4 colour, float drawDepth)
        {
            Box b = new Box()
            {
                Size = new Vector2((WIDTH - 1), 0),
                Colour = colour,
                Scale = new Vector2(3, 3),
            };

            eventSprites.Add(b);
            Add(b);
        }

        protected override void Update()
        {
            base.Update();

            //if (KeyboardHandler.ControlPressed)
            //{
            //    legendSprites.ForEach(s => s.Alpha = 1);
            //    return;
            //}
            FrameStatistics frame;
            while (monitor.PendingFrames.TryDequeue(out frame))
            {
                //legendSprites.ForEach(s => s.Alpha = 0);

                for (int i = 0; i < lastAmountGarbageCollects.Length; ++i)
                {
                    int amountCollections = GC.CollectionCount(i);
                    if (lastAmountGarbageCollects[i] != amountCollections)
                    {
                        lastAmountGarbageCollects[i] = amountCollections;
                        AddEvent(garbageCollectColors[i], i);
                    }
                }

                Sprite timeBar = timeBars[TimeBarIndex];

                int currentHeight = HEIGHT;

                for (int i = 0; i <= (int)FrameTimeType.Empty; i++)
                    currentHeight = addArea(frame, (FrameTimeType)i, currentHeight);

                timeBar.Texture.SetData(new TextureUpload(textureData)
                {
                    Bounds = new Rectangle(TimeBarX, 0, 1, HEIGHT)
                });

                timeBars[TimeBarIndex].MoveToX((WIDTH - TimeBarX), 0);
                timeBars[(TimeBarIndex + 1) % timeBars.Length].MoveToX(-TimeBarX, 0);

                currentX = (currentX + 1) % (timeBars.Length * WIDTH);

                for (int i = 0; i < eventSprites.Count; ++i)
                {
                    Drawable e = eventSprites[i];
                    e.MoveToRelative(new Vector2(-1, 0));

                    if (e.Position.X < -2)
                    {
                        e.FadeOut(0);
                        eventSprites.RemoveAt(i--);
                    }
                }
            }
        }

        private Color4 getColour(FrameTimeType type)
        {
            Color4 col = default(Color4);

            switch (type)
            {
                default:
                case FrameTimeType.Update:
                    col = Color4.YellowGreen;
                    break;
                case FrameTimeType.Draw:
                    col = Color4.BlueViolet;
                    break;
                case FrameTimeType.SwapBuffer:
                    col = Color4.Red;
                    break;
#if DEBUG
                case FrameTimeType.Debug:
                    col = Color4.Yellow;
                    break;
#endif
                case FrameTimeType.Sleep:
                    col = Color4.DarkBlue;
                    break;
                case FrameTimeType.Scheduler:
                    col = Color4.HotPink;
                    break;
                case FrameTimeType.BetweenFrames:
                    col = Color4.GhostWhite;
                    break;
                case FrameTimeType.Empty:
                    col = new Color4(50, 40, 40, 180);
                    break;
            }

            return col;
        }

        private int addArea(FrameStatistics frame, FrameTimeType frameTimeType, int currentHeight)
        {
            double elapsedMilliseconds = 0;
            int drawHeight = 0;

            if (frameTimeType == FrameTimeType.Empty)
                drawHeight = currentHeight;
            else if (frame.CollectedTimes.TryGetValue(frameTimeType, out elapsedMilliseconds))
            {
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
                textureData[index + 3] = (byte)(255 * (frameTimeType == FrameTimeType.Empty ? (col.A * (1 - (int)((i * 4) / HEIGHT) / 8f)) : col.A));
                currentHeight--;
            }

            return currentHeight;
        }
    }

    public enum FrameTimeType
    {
        Update,
        Draw,
        SwapBuffer,
        BetweenFrames,
        Debug,
        Sleep,
        Scheduler,
        IPC,
        Empty
    }

    public class FrameStatistics
    {
        internal Dictionary<FrameTimeType, double> CollectedTimes = new Dictionary<FrameTimeType, double>();
        internal Dictionary<CounterType, int> CollectedCounters = new Dictionary<CounterType, int>();
    }

    public class PerformanceMonitor
    {
        internal StopwatchClock clock = new StopwatchClock(true);

        internal Stack<FrameTimeType> CurrentCollectionTypeStack = new Stack<FrameTimeType>();

        internal FrameStatistics currentFrame;

        internal ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();

        private double consumptionTime;

        internal double FramesPerSecond;
        internal double AverageFrameTime;

        double timeUntilNextCalculation;
        double timeSinceLastCalculation;
        int framesSinceLastCalculation;

        const int fps_calculation_interval = 250;

        internal void ReportCount(CounterType type)
        {
            //OsuGame.Scheduler.Add(delegate
            //{
            if (!currentFrame.CollectedCounters.ContainsKey(type))
                currentFrame.CollectedCounters[type] = 1;
            else
                currentFrame.CollectedCounters[type]++;
            //});
        }

        /// <summary>
        /// Start collecting a type of passing time.
        /// </summary>
        internal void BeginCollecting(FrameTimeType type)
        {
            if (CurrentCollectionTypeStack.Count > 0)
            {
                FrameTimeType t = CurrentCollectionTypeStack.Peek();

                if (!currentFrame.CollectedTimes.ContainsKey(t)) currentFrame.CollectedTimes[t] = 0;
                currentFrame.CollectedTimes[t] += consumeStopwatchElapsedTime();
            }

            CurrentCollectionTypeStack.Push(type);
        }

        private double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;
            consumptionTime = clock.CurrentTime;
            return consumptionTime - last;
        }

        /// <summary>
        /// End collecting a type of passing time (that was previously started).
        /// </summary>
        /// <param name="type"></param>
        internal void EndCollecting(FrameTimeType type)
        {
            CurrentCollectionTypeStack.Pop();

            if (!currentFrame.CollectedTimes.ContainsKey(type)) currentFrame.CollectedTimes[type] = 0;
            currentFrame.CollectedTimes[type] += consumeStopwatchElapsedTime();
        }

        public int TargetFrameRate;

        /// <summary>
        /// Resets all frame statistics. Run exactly once per frame.
        /// </summary>
        internal void NewFrame(IFrameBasedClock clock)
        {
            if (currentFrame != null)
                PendingFrames.Enqueue(currentFrame);
            currentFrame = new FrameStatistics();

            //update framerate
            double decay = Math.Pow(0.05, clock.ElapsedFrameTime);

            framesSinceLastCalculation++;
            timeUntilNextCalculation -= clock.ElapsedFrameTime;
            timeSinceLastCalculation += clock.ElapsedFrameTime;

            if (timeUntilNextCalculation <= 0)
            {
                timeUntilNextCalculation += fps_calculation_interval;

                FramesPerSecond = framesSinceLastCalculation == 0 ? 0 : (int)Math.Ceiling(Math.Min(framesSinceLastCalculation * 1000f / timeSinceLastCalculation, TargetFrameRate));
                timeSinceLastCalculation = framesSinceLastCalculation = 0;
            }

            //check for dropped (stutter) frames
            //if (clock.ElapsedFrameTime > spikeTime)
            //NewDroppedFrame();

            AverageFrameTime = decay * AverageFrameTime + (1 - decay) * clock.ElapsedFrameTime;

            //reset frame totals
            CurrentCollectionTypeStack.Clear();
            //backgroundMonitorStackTrace = null;
            consumeStopwatchElapsedTime();
        }
    }
}
