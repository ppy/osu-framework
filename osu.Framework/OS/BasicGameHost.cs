//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;
using System.Windows.Forms;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
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
        public event EventHandler Exiting;
        public event EventHandler Idle;

        public override bool IsVisible => true;

        private static Thread updateThread;
        private static Thread drawThread;
        private static Thread startupThread = Thread.CurrentThread;

        internal static Thread DrawThread => drawThread;
        internal static Thread UpdateThread => updateThread?.IsAlive ?? false ? updateThread : startupThread;

        internal ThrottledFrameClock UpdateClock = new ThrottledFrameClock();
        internal ThrottledFrameClock DrawClock = new ThrottledFrameClock() { MaximumUpdateHz = 60 };

        protected override IFrameBasedClock Clock => UpdateClock;

        protected int MaximumFramesPerSecond
        {
            get { return UpdateClock.MaximumUpdateHz; }
            set { UpdateClock.MaximumUpdateHz = value; }
        }

        public override bool Invalidate(bool affectsSize = true, bool affectsPosition = true, Drawable source = null)
        {
            //update out size based on the underlying window
            if (!Window.IsMinimized)
                Size = new Vector2(Window.Size.Width, Window.Size.Height);

            return base.Invalidate(affectsSize, affectsPosition, source);
        }

        public override Vector2 Size
        {
            get
            {
                return base.Size;
            }

            set
            {
                //update the underlying window size based on our new set size.
                //important we do this before the base.Size set otherwise Invalidate logic will overwrite out new setting.
                Window.Size = new System.Drawing.Size((int)value.X, (int)value.Y);

                base.Size = value;
            }
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

        protected virtual void OnExiting(object sender, EventArgs args)
        {
            Exiting?.Invoke(this, EventArgs.Empty);
        }

        protected override void Update()
        {
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

        protected virtual void OnIdle(object sender, EventArgs args)
        {
            Idle?.Invoke(this, EventArgs.Empty);
        }

        private bool exitRequested;
        private bool threadsRunning => (updateThread?.IsAlive ?? false) && (drawThread?.IsAlive ?? false);

        public void Exit()
        {
            exitRequested = true;
        }

        public virtual void Run()
        {
            Window.ClientSizeChanged += delegate { Invalidate(); };

            drawThread = new Thread(drawLoop) { IsBackground = true };
            drawThread.Start();

            updateThread = new Thread(updateLoop) { IsBackground = true };
            updateThread.Start();

            Exception error = null;

            try
            {
                Application.Idle += OnApplicationIdle;
                Application.Run(Window.Form);
            }
            catch (OutOfMemoryException e)
            {
                error = e;
            }
            finally
            {
                Application.Idle -= OnApplicationIdle;

                if (error == null || !(error is OutOfMemoryException))
                    //we don't want to attempt a safe shutdown is memory is low; it may corrupt database files.
                    OnExiting(this, null);
            }
        }

        protected virtual void OnApplicationIdle(object sender, EventArgs e)
        {
            if (exitRequested && !threadsRunning)
                Window.Close();
            else
                OnIdle(sender, e);
        }

        public void Load(Game game)
        {
            game.SetHost(this);
            Add(game);
        }
    }
}
