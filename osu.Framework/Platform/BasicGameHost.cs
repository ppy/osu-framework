// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Windows.Forms;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using osu.Framework.Cached;
using osu.Framework.Graphics.Primitives;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public abstract class BasicGameHost : Container
    {
        public BasicGameWindow Window;

        private bool isActive;
        public bool IsActive
        {
            get
            {
                return isActive;
            }

            protected set
            {
                if (isActive == value)
                    return;

                isActive = value;

                if (isActive)
                {
                    MaximumUpdateHz = ActiveUpdateHz;
                    MaximumDrawHz = ActiveDrawHz;

                    Activated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MaximumUpdateHz = InactiveUpdateHz;
                    MaximumDrawHz = InactiveDrawHz;

                    Deactivated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        public bool IsPrimaryInstance { get; protected set; }

        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event Func<bool> Exiting;
        public event Action Exited;
        
        protected internal event Action<IpcMessage> MessageReceived;

        protected void OnMessageReceived(IpcMessage message)
        {
            MessageReceived?.Invoke(message);
        }

        protected internal virtual Task SendMessage(IpcMessage message)
        {
            throw new NotImplementedException("This platform does not implement IPC.");
        }

        public virtual BasicStorage Storage { get; protected set; }

        public override bool IsVisible => true;

        private static Thread updateThread;
        private static Thread drawThread;
        private static Thread startupThread = Thread.CurrentThread;

        internal static Thread DrawThread => drawThread;
        internal static Thread UpdateThread => updateThread?.IsAlive ?? false ? updateThread : startupThread;

        internal FramedClock InputClock = new FramedClock();

        protected internal ThrottledFrameClock UpdateClock = new ThrottledFrameClock();

        private int activeUpdateHz = 1000;
        public int ActiveUpdateHz
        {
            get
            {
                return activeUpdateHz;
            }

            set
            {
                activeUpdateHz = value;
                if (IsActive)
                    MaximumUpdateHz = activeUpdateHz;
            }
        }

        private int inactiveUpdateHz = 30;
        public int InactiveUpdateHz
        {
            get
            {
                return inactiveUpdateHz;
            }

            set
            {
                inactiveUpdateHz = value;
                if (!IsActive)
                    MaximumUpdateHz = inactiveUpdateHz;
            }
        }

        public int MaximumUpdateHz
        {
            get { return UpdateClock.MaximumUpdateHz; }
            set { UpdateClock.MaximumUpdateHz = value; }
        }

        internal ThrottledFrameClock DrawClock = new ThrottledFrameClock();

        private int activeDrawHz = 144;
        public int ActiveDrawHz
        {
            get
            {
                return activeDrawHz;
            }

            set
            {
                activeDrawHz = value;
                if (IsActive)
                    MaximumDrawHz = activeDrawHz;
            }
        }

        private int inactiveDrawHz = 30;
        public int InactiveDrawHz
        {
            get
            {
                return inactiveDrawHz;
            }

            set
            {
                inactiveDrawHz = value;
                if (!IsActive)
                    MaximumDrawHz = inactiveDrawHz;
            }
        }

        public int MaximumDrawHz
        {
            get { return DrawClock.MaximumUpdateHz; }
            set { DrawClock.MaximumUpdateHz = value; }
        }

        protected internal PerformanceMonitor InputMonitor;
        protected internal PerformanceMonitor UpdateMonitor;
        protected internal PerformanceMonitor DrawMonitor;

        //null here to construct early but bind to thread late.
        public Scheduler InputScheduler = new Scheduler(null);
        protected Scheduler UpdateScheduler = new Scheduler(null);

        protected override IFrameBasedClock Clock => UpdateClock;

        protected int MaximumFramesPerSecond
        {
            get { return UpdateClock.MaximumUpdateHz; }
            set { UpdateClock.MaximumUpdateHz = value; }
        }

        public Cached<string> fullPathBacking = new Cached<string>();
        public string FullPath => fullPathBacking.EnsureValid() ? fullPathBacking.Value : fullPathBacking.Refresh(() =>
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        });

        private UserInputManager inputManager;

        protected override Container Content => inputManager;

        public BasicGameHost()
        {
            InputMonitor = new PerformanceMonitor(InputClock) { HandleGC = false };
            UpdateMonitor = new PerformanceMonitor(UpdateClock);
            DrawMonitor = new PerformanceMonitor(DrawClock);

            Environment.CurrentDirectory = Path.GetDirectoryName(FullPath);

            IsActive = true;

            AddInternal(inputManager = new UserInputManager(this));
        }

        protected virtual void OnActivated(object sender, EventArgs args)
        {
            UpdateScheduler.Add(delegate
            {
                IsActive = true;
            });
        }

        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
            UpdateScheduler.Add(delegate
            {
                IsActive = false;
            });
        }

        protected virtual bool OnExitRequested()
        {
            if (ExitRequested) return false;

            bool? response = null;

            UpdateScheduler.Add(delegate
            {
                response = Exiting?.Invoke() == true;
            });

            //wait for a potentially blocking response
            while (!response.HasValue)
                Thread.Sleep(1);

            if (response.Value)
                return true;

            ExitRequested = true;
            while (threadsRunning)
                Thread.Sleep(1);

            return false;
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke();
        }

        protected TripleBuffer<DrawNode> DrawRoots = new TripleBuffer<DrawNode>();

        private void updateLoop()
        {
            //this was added due to the dependency on GLWrapper.MaxTextureSize begin initialised.
            while (!GLWrapper.IsInitialized)
                Thread.Sleep(1);

            while (!ExitRequested)
            {
                UpdateMonitor.NewFrame();

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                {
                    UpdateScheduler.Update();
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Update))
                {
                    UpdateSubTree();
                    using (var buffer = DrawRoots.Get(UsageType.Write))
                        buffer.Object = GenerateDrawNodeSubtree(buffer.Object);
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                {
                    UpdateClock.ProcessFrame();
                }
            }
        }

        private void drawLoop()
        {
            Window.MakeCurrent();
            GLWrapper.Initialize();

            while (!ExitRequested)
            {
                DrawMonitor.NewFrame();

                DrawFrame();

                using (DrawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
                    Window.SwapBuffers();

                using (DrawMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                    DrawClock.ProcessFrame();
            }
        }

        protected virtual void DrawFrame()
        {
            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
            {
                GLWrapper.Reset(DrawSize);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }

            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.Draw))
            {
                using (var buffer = DrawRoots.Get(UsageType.Read))
                    buffer?.Object?.DrawSubTree();
            }
        }

        protected bool ExitRequested;

        private bool threadsRunning => (updateThread?.IsAlive ?? false) && (drawThread?.IsAlive ?? false);

        public void Exit()
        {
            ExitRequested = true;
            while (threadsRunning)
                Thread.Sleep(1);
            Window?.Close();
        }

        public virtual void Run()
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            drawThread = new Thread(drawLoop)
            {
                Name = @"DrawThread",
                IsBackground = true
            };
            drawThread.Start();

            updateThread = new Thread(updateLoop)
            {
                Name = @"UpdateThread",
                IsBackground = true
            };
            updateThread.Start();

            UpdateScheduler.SetCurrentThread(updateThread);

            if (Window != null)
            {
                Window.Resize += window_ClientSizeChanged;
                Window.ExitRequested += OnExitRequested;
                Window.Exited += OnExited;
                window_ClientSizeChanged(null, null);
            }

            InputScheduler.SetCurrentThread(Thread.CurrentThread);

            try
            {
                Window.RenderFrame += delegate { OnApplicationIdle(); };
                Window.Run();
            }
            catch (OutOfMemoryException)
            {
            }
            finally
            {
                //if (!(error is OutOfMemoryException))
                //    //we don't want to attempt a safe shutdown is memory is low; it may corrupt database files.
                //    OnExiting();
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.WindowState == WindowState.Minimized) return;

            Rectangle rect = Window.Bounds;
            UpdateScheduler.Add(delegate
            {
                //set base.Size here to avoid the override below, which would cause a recursive loop.
                base.Size = new Vector2(rect.Width, rect.Height);
            });
        }

        public override Vector2 Size
        {
            set
            {
                InputScheduler.Add(delegate
                {
                    //update the underlying window size based on our new set size.
                    //important we do this before the base.Size set otherwise Invalidate logic will overwrite out new setting.
                    Window.Size = new Size((int)value.X, (int)value.Y);
                });

                base.Size = value;
            }
        }

        InvokeOnDisposal inputPerformanceCollectionPeriod;

        protected virtual void OnApplicationIdle()
        {
            inputPerformanceCollectionPeriod?.Dispose();

            InputMonitor.NewFrame();

            using (InputMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                InputScheduler.Update();

            using (InputMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                InputClock.ProcessFrame();

            inputPerformanceCollectionPeriod = InputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);
        }

        public override void Add(Drawable drawable)
        {
            BaseGame game = drawable as BaseGame;
            Debug.Assert(game != null, @"Make sure to load a Game in a Host");

            game.SetHost(this);
            UpdateScheduler.Add(delegate
            {
                Children = new[] { game };
                Load(game);
            });
        }

        public abstract IEnumerable<InputHandler> GetInputHandlers();
    }
}
