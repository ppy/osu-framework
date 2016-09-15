using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Performance
{
    class FrameTimeDisplay : MaskingContainer
    {
//        static Vector2 padding = new Vector2(0, 0);
//        const int WIDTH = 800;
//        const int HEIGHT = 100;

//        //PerformanceMonitor monitor;

//        const int visible_range = 20;
//        const float scale = HEIGHT / visible_range;

//        private pSprite[] timeBars = new pSprite[2];
//        private byte[] textureData = new byte[HEIGHT * 4];

//        private int currentX = 0;
//        private int TimeBarIndex
//        {
//            get
//            {
//                return currentX / WIDTH;
//            }
//        }

//        private int TimeBarX
//        {
//            get
//            {
//                return currentX % WIDTH;
//            }
//        }

//        List<pSprite> legendSprites = new List<pSprite>();
//        List<pSprite> eventSprites = new List<pSprite>();

//        internal FrameTimeDisplay(PerformanceMonitor monitor)
//            : base(true)
//        {
//            Initialize();
//            GameBase.OnResolutionChange += Initialize;

//            this.monitor = monitor;
//        }

//        internal void Initialize()
//        {
//            spriteManager.Clear();
//            legendSprites.Clear();

//            spriteManager.Add(new pText(@"0ms", 18 / GameBase.WindowManager.Ratio, new Vector2(0, HEIGHT / GameBase.WindowManager.Ratio), 1, true, Color.White)
//            {
//                ScaleToWindowRatio = false,
//                Field = Fields.TopLeft,
//                Origin = Origins.BottomLeft
//            });

//            pText topText = new pText((HEIGHT / scale) + @"ms", 18 / GameBase.WindowManager.Ratio, new Vector2(0, 0), 1, true, Color.White)
//            {
//                ScaleToWindowRatio = false,
//                Field = Fields.TopLeft,
//                Origin = Origins.TopLeft
//            };

//            spriteManager.Add(topText);

//            topText.MeasureText();
//            float x = topText.lastMeasure.X;

//            foreach (FrameTimeType t in Enum.GetValues(typeof(FrameTimeType)))
//            {
//                if (t >= FrameTimeType.Empty) continue;

//                topText = new pText(t.ToString(), 27 / GameBase.WindowManager.Ratio, new Vector2(x / GameBase.WindowManager.Ratio, 0), 1, true, getColour(t))
//                {
//                    Alpha = 0,
//                    ScaleToWindowRatio = false,
//                    Field = Fields.TopLeft,
//                    Origin = Origins.TopLeft
//                };
//                legendSprites.Add(topText);

//                topText.MeasureText();
//                x += topText.lastMeasure.X;
//            }

//            spriteManager.Add(legendSprites);

//            ConfigManager.sFrameTimeDisplay.ValueChanged += delegate { spriteManager.Alpha = ConfigManager.sFrameTimeDisplay ? 1 : 0; };

//            for (int i = 0; i < timeBars.Length; ++i)
//            {
//                timeBars[i] = new pSprite(new pTexture(WIDTH, HEIGHT), Fields.TopLeft, Origins.TopLeft, Clocks.Game, new Vector2(0, 0))
//                {
//                    Depth = 0F,
//                    AlwaysDraw = true,
//                    ScaleToWindowRatio = false,
//                };

//                spriteManager.Add(timeBars[i]);
//            }

//            spriteManager.SetVisibleArea(new RectangleF(GameBase.WindowManager.Width - WIDTH, GameBase.WindowManager.Height - HEIGHT + 1, WIDTH + 1, HEIGHT) / GameBase.WindowManager.Ratio);

//            // Initialize background
//            for (int i = 0; i < WIDTH * timeBars.Length; ++i)
//            {
//                currentX = i;
//                pSprite timeBar = timeBars[TimeBarIndex];

//                addArea(FrameTimeType.Empty, HEIGHT);
//                ((TextureGlSingle)timeBar.Texture.TextureGl).SetData(textureData, new Rectangle(TimeBarX, 0, 1, HEIGHT));
//                timeBar.Texture.TextureGl.Upload();
//            }
//        }

//        internal void AddEvent(Color color, float drawDepth)
//        {
//            pSprite e = new pSprite(GameBase.WhitePixel, Fields.TopLeft, Origins.TopLeft, Clocks.Game, new Vector2((WIDTH - 1) / GameBase.WindowManager.Ratio, 0), 0.1f + drawDepth, true, color)
//            {
//                ScaleToWindowRatio = false,
//                VectorScale = new Vector2(3, 3),
//            };

//            eventSprites.Add(e);
//            spriteManager.Add(e);
//        }


//        private static Color[] garbageCollectColors = new Color[] { Color.Green, Color.Yellow, Color.Red };
//        private int[] lastAmountGarbageCollects = new int[3];

//        internal override unsafe void Update()
//        {
//            base.Update();

//            if (KeyboardHandler.ControlPressed)
//            {
//                legendSprites.ForEach(s => s.Alpha = 1);
//                return;
//            }

//            legendSprites.ForEach(s => s.Alpha = 0);

//            for (int i = 0; i < lastAmountGarbageCollects.Length; ++i)
//            {
//                int amountCollections = GC.CollectionCount(i);
//                if (lastAmountGarbageCollects[i] != amountCollections)
//                {
//                    lastAmountGarbageCollects[i] = amountCollections;
//                    AddEvent(garbageCollectColors[i], i);
//                }
//            }

//            pSprite timeBar = timeBars[TimeBarIndex];

//            int currentHeight = HEIGHT;

//            for (int i = 0; i <= (int)FrameTimeType.Empty; i++)
//                currentHeight = addArea((FrameTimeType)i, currentHeight);

//            ((TextureGlSingle)timeBar.Texture.TextureGl).SetData(textureData, new Rectangle(TimeBarX, 0, 1, HEIGHT));
//            timeBar.Texture.TextureGl.Upload();

//            timeBars[TimeBarIndex].MoveToX((WIDTH - TimeBarX) / GameBase.WindowManager.Ratio, 0);
//            timeBars[(TimeBarIndex + 1) % timeBars.Length].MoveToX(-TimeBarX / GameBase.WindowManager.Ratio, 0);

//            currentX = (currentX + 1) % (timeBars.Length * WIDTH);

//            for (int i = 0; i < eventSprites.Count; ++i)
//            {
//                pSprite e = eventSprites[i];
//                e.Position.X -= 1 / GameBase.WindowManager.Ratio;

//                if (e.Position.X < -2)
//                {
//                    e.AlwaysDraw = false;
//                    e.FadeOut(0);
//                    eventSprites.RemoveAt(i--);
//                }
//            }
//        }

//        private Color getColour(FrameTimeType type)
//        {
//            Color col = default(Color);

//            switch (type)
//            {
//                default:
//                case FrameTimeType.Update:
//                    col = Color.YellowGreen;
//                    break;
//                case FrameTimeType.Draw:
//                    col = Color.BlueViolet;
//                    break;
//                case FrameTimeType.SwapBuffer:
//                    col = Color.Red;
//                    break;
//#if DEBUG
//                case FrameTimeType.Debug:
//                    col = Color.Yellow;
//                    break;
//#endif
//                case FrameTimeType.Sleep:
//                    col = Color.DarkBlue;
//                    break;
//                case FrameTimeType.Scheduler:
//                    col = Color.HotPink;
//                    break;
//                case FrameTimeType.BetweenFrames:
//                    col = Color.GhostWhite;
//                    break;
//                case FrameTimeType.Empty:
//                    col = new Color(50, 40, 40, 180);
//                    break;
//            }

//            return col;
//        }

//        private int addArea(FrameTimeType frameTimeType, int currentHeight)
//        {
//            double elapsedMilliseconds = 0;
//            int drawHeight = 0;

//            if (frameTimeType == FrameTimeType.Empty)
//                drawHeight = currentHeight;
//            else if (monitor.CollectedTimes.TryGetValue(frameTimeType, out elapsedMilliseconds))
//            {
//                drawHeight = (int)(elapsedMilliseconds * scale);
//            }
//            else
//                return currentHeight;

//            Color col = getColour(frameTimeType);

//            for (int i = currentHeight - 1; i >= 0; --i)
//            {
//                if (drawHeight-- == 0) break;

//                int index = i * 4;
//                textureData[index] = col.R;
//                textureData[index + 1] = col.G;
//                textureData[index + 2] = col.B;
//                textureData[index + 3] = frameTimeType == FrameTimeType.Empty ? (byte)(col.A * (1 - (int)((i * 4) / HEIGHT) / 8f)) : col.A;
//                currentHeight--;
//            }

//            return currentHeight;
//        }
    }
}
