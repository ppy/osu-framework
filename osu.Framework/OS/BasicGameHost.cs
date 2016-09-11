//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Threading;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace osu.Framework.OS
{
    public abstract class BasicGameHost : Container
    {
        public abstract BasicGameWindow Window { get; }
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

        internal ThrottledFrameClock UpdateClock = new ThrottledFrameClock();
        internal ThrottledFrameClock DrawClock = new ThrottledFrameClock() { MaximumUpdateHz = 144 };

        private Scheduler updateScheduler;

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

        protected override void Update()
        {
            updateScheduler.Update();

            base.Update();
            UpdateClock.ProcessFrame();
        }

        DrawNode pendingRootNode;

        private void updateLoop()
        {
            while (!exitRequested)
            {

                UpdateSubTree();
                pendingRootNode = GenerateDrawNodeSubtree();
            }
        }

        private void drawLoop()
        {
            GLControl.Initialize();

            while (!exitRequested)
            {
                GLWrapper.Reset(Size);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                pendingRootNode?.DrawSubTree();

                GLControl.SwapBuffers();

                GLControl.Invalidate();

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

            updateScheduler = new Scheduler(updateThread);

            Window.ClientSizeChanged += window_ClientSizeChanged;
            window_ClientSizeChanged(null, null);

            Window.ExitRequested += OnExitRequested;
            Window.Exited += OnExited;

            Exception error = null;

            try
            {
                Application.Idle += delegate { OnApplicationIdle(); };
                Application.Run(Window.Form);
            }
            catch (OutOfMemoryException e)
            {
                error = e;
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
            get
            {
                return base.Size;
            }

            set
            {
                Window.Form.SafeInvoke(delegate
                {
                    //update the underlying window size based on our new set size.
                    //important we do this before the base.Size set otherwise Invalidate logic will overwrite out new setting.
                    Window.Size = new Size((int)value.X, (int)value.Y);
                });

                base.Size = value;
            }
        }

        protected virtual void OnApplicationIdle()
        {
            if (exitRequested)
                Window.Close();
        }

        public void Load(Game game)
        {
            game.SetHost(this);
            Add(game);
        }
    }
}
