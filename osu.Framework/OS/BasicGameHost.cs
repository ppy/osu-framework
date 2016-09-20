// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Runtime;
using System.Threading;
using System.Windows.Forms;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace osu.Framework.OS
{
    public abstract class BasicGameHost : Container
    {
        public BasicGameWindow Window;

        public abstract GLControl GLControl { get; }
        public abstract bool IsActive { get; }

        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event Func<bool> ExitRequested;
        public event Action Exited;

        public override bool IsVisible => true;

        private static Thread updateThread;
        private static Thread drawThread;
        private static Thread startupThread = Thread.CurrentThread;

        internal static Thread DrawThread => drawThread;
        internal static Thread UpdateThread => updateThread?.IsAlive ?? false ? updateThread : startupThread;

        internal FramedClock InputClock = new FramedClock();
        internal ThrottledFrameClock UpdateClock = new ThrottledFrameClock();

        internal ThrottledFrameClock DrawClock = new ThrottledFrameClock
        {
            MaximumUpdateHz = 144
        };

        public int MaximumUpdateHz
        {
            get { return UpdateClock.MaximumUpdateHz; }
            set { UpdateClock.MaximumUpdateHz = value; }
        }

        public int MaximumDrawHz
        {
            get { return DrawClock.MaximumUpdateHz; }
            set { DrawClock.MaximumUpdateHz = value; }
        }

        internal PerformanceMonitor InputMonitor = new PerformanceMonitor();
        internal PerformanceMonitor UpdateMonitor = new PerformanceMonitor();
        internal PerformanceMonitor DrawMonitor = new PerformanceMonitor();

        //null here to construct early but bind to thread late.
        internal Scheduler InputScheduler = new Scheduler(null);
        private Scheduler updateScheduler = new Scheduler(null);

        protected override IFrameBasedClock Clock => UpdateClock;

        protected int MaximumFramesPerSecond
        {
            get { return UpdateClock.MaximumUpdateHz; }
            set { UpdateClock.MaximumUpdateHz = value; }
        }

        public abstract TextInputSource TextInput { get; }

        protected virtual void OnActivated(object sender, EventArgs args)
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool OnExitRequested()
        {
            if (ExitRequested?.Invoke() == true)
                return true;

            exitRequested = true;
            while (threadsRunning)
                Thread.Sleep(1);

            return false;
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke();
        }

        DrawNode pendingRootNode;

        private void updateLoop()
        {
            //this was added due to the dependency on GLWrapper.MaxTextureSize begin initialised.
            while (!GLWrapper.IsInitialized)
                Thread.Sleep(1);

            while (!exitRequested)
            {
                UpdateMonitor.NewFrame(UpdateClock);

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                {
                    updateScheduler.Update();
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Update))
                {
                    UpdateSubTree();
                    pendingRootNode = GenerateDrawNodeSubtree();
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                {
                    UpdateClock.ProcessFrame();
                }
            }
        }

        private void drawLoop()
        {
            GLControl.Initialize();
            GLWrapper.Initialize();

            while (!exitRequested)
            {
                DrawMonitor.NewFrame(DrawClock);

                using (DrawMonitor.BeginCollecting(PerformanceCollectionType.Draw))
                {
                    GLWrapper.Reset(Size);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    pendingRootNode?.DrawSubTree();
                }

                using (DrawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
                {
                    GLControl.SwapBuffers();
                    GLControl.Invalidate();
                }

                using (DrawMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                    DrawClock.ProcessFrame();
            }
        }

        private bool exitRequested;

        private bool threadsRunning => (updateThread?.IsAlive ?? false) && (drawThread?.IsAlive ?? false);

        public void Exit()
        {
            exitRequested = true;
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

            updateScheduler.SetCurrentThread(updateThread);

            Window.ClientSizeChanged += window_ClientSizeChanged;
            window_ClientSizeChanged(null, null);

            Window.ExitRequested += OnExitRequested;
            Window.Exited += OnExited;

            InputScheduler.SetCurrentThread(Thread.CurrentThread);

            try
            {
                Application.Idle += delegate { OnApplicationIdle(); };
                Application.Run(Window.Form);
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
            if (Window.IsMinimized) return;

            Rectangle rect = Window.ClientBounds;
            updateScheduler.Add(delegate
            {
                //set base.Size here to avoid the override below, which would cause a recursive loop.
                base.Size = new Vector2(rect.Width, rect.Height);
            });
        }

        public override Vector2 Size
        {
            get { return base.Size; }

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

            InputMonitor.NewFrame(InputClock);

            using (InputMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                InputScheduler.Update();

            using (InputMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                InputClock.ProcessFrame();

            inputPerformanceCollectionPeriod = InputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);

            if (exitRequested)
                Window.Close();
        }

        public void Load(Game game)
        {
            game.SetHost(this);
            updateScheduler.Add(delegate { Add(game); });
        }
    }
}
