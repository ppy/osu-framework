// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Threading;
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
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Sprites;
using OpenTK.Input;
using OpenTK.Graphics;

namespace osu.Framework.Platform
{
    public abstract class BasicGameHost : Container, IIpcHost
    {
        public static BasicGameHost Instance;

        public BasicGameWindow Window;

        private void setActive(bool isActive)
        {
            threads.ForEach(t => t.IsActive = isActive);

            if (isActive)
                Activated?.Invoke();
            else
                Deactivated?.Invoke();
        }

        public bool IsActive => InputThread.IsActive;

        public bool IsPrimaryInstance { get; protected set; } = true;

        public event Action Activated;
        public event Action Deactivated;
        public event Func<bool> Exiting;
        public event Action Exited;

        public event Action<Exception> ExceptionThrown;

        public event Action<IpcMessage> MessageReceived;

        protected void OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessage(IpcMessage message)
        {
            throw new NotImplementedException("This platform does not implement IPC.");
        }

        public virtual Clipboard GetClipboard() => null;

        public virtual BasicStorage Storage { get; protected set; } //public set currently required for visualtests setup.

        public override bool IsPresent => true;

        private GameThread[] threads;

        public GameThread DrawThread;
        public GameThread UpdateThread;
        public InputThread InputThread;

        private double maximumUpdateHz;

        public double MaximumUpdateHz
        {
            get
            {
                return maximumUpdateHz;
            }

            set
            {
                UpdateThread.ActiveHz = maximumUpdateHz = value;
            }
        }

        private double maximumDrawHz;

        public double MaximumDrawHz
        {
            get
            {
                return maximumDrawHz;
            }

            set
            {
                DrawThread.ActiveHz = maximumDrawHz = value;
            }
        }

        public double MaximumInactiveHz
        {
            get
            {
                return DrawThread.InactiveHz;
            }

            set
            {
                DrawThread.InactiveHz = value;
                UpdateThread.InactiveHz = value;
            }
        }

        protected internal PerformanceMonitor InputMonitor => InputThread.Monitor;
        protected internal PerformanceMonitor UpdateMonitor => UpdateThread.Monitor;
        protected internal PerformanceMonitor DrawMonitor => DrawThread.Monitor;

        private Cached<string> fullPathBacking = new Cached<string>();
        public string FullPath => fullPathBacking.EnsureValid() ? fullPathBacking.Value : fullPathBacking.Refresh(() =>
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        });

        private UserInputManager inputManager;

        protected override Container<Drawable> Content => inputManager;

        private string name;
        public override string Name => name;

        public DependencyContainer Dependencies { get; private set; } = new DependencyContainer();

        protected BasicGameHost(string gameName = @"")
        {
            Instance = this;

            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;

            Dependencies.Cache(this);
            name = gameName;

            threads = new[]
            {
                DrawThread = new GameThread(DrawFrame, @"Draw")
                {
                    OnThreadStart = DrawInitialize,
                },
                UpdateThread = new GameThread(UpdateFrame, @"Update")
                {
                    OnThreadStart = UpdateInitialize,
                    Monitor = { HandleGC = true }
                },
                InputThread = new InputThread(null, @"Input") //never gets started.
            };

            Clock = UpdateThread.Clock;

            MaximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;
            MaximumDrawHz = (DisplayDevice.Default?.RefreshRate ?? 0) * 4;

            // Note, that RegisterCounters only has an effect for the first
            // BasicGameHost to be passed into it; i.e. the first BasicGameHost
            // to be instantiated.
            FrameStatistics.RegisterCounters(this);

            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(FullPath);

            setActive(true);

            AddInternal(inputManager = new UserInputManager(this));

            Dependencies.Cache(inputManager);
        }

        private void exceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (ExceptionThrown != null)
                ExceptionThrown.Invoke(exception);
            else
            {
                AppDomain.CurrentDomain.UnhandledException -= exceptionHandler;
                throw exception;
            }
        }

        protected virtual void OnActivated() => Schedule(() => setActive(true));

        protected virtual void OnDeactivated() => Schedule(() => setActive(false));

        /// <returns>true to cancel</returns>
        protected virtual bool OnExitRequested()
        {
            if (ExitRequested) return false;

            bool? response = null;

            UpdateThread.Scheduler.Add(delegate
            {
                response = Exiting?.Invoke() == true;
            });

            //wait for a potentially blocking response
            while (!response.HasValue)
                Thread.Sleep(1);

            if (response.Value)
                return true;

            Exit();
            return false;
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke();
        }

        protected TripleBuffer<DrawNode> DrawRoots = new TripleBuffer<DrawNode>();

        protected virtual void UpdateInitialize()
        {
            //this was added due to the dependency on GLWrapper.MaxTextureSize begin initialised.
            DrawThread.WaitUntilInitialized();
        }

        protected void UpdateFrame()
        {
            UpdateSubTree();
            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = GenerateDrawNodeSubtree(buffer.Index, ScreenSpaceDrawQuad.AABBFloat);
        }

        protected virtual void DrawInitialize()
        {
            Window.MakeCurrent();
            GLWrapper.Initialize(this);

            if (Window != null)
                Window.VSync = VSyncMode.Off;
        }

        long lastDrawFrameId;

        protected virtual void DrawFrame()
        {
            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
            {
                GLWrapper.Reset(DrawSize);
                GLWrapper.ClearColour(Color4.Black);
            }

            while (true)
            {
                using (var buffer = DrawRoots.Get(UsageType.Read))
                {
                    if (buffer?.Object != null && buffer.FrameId != lastDrawFrameId)
                    {
                        buffer.Object.Draw(null);
                        lastDrawFrameId = buffer.FrameId;
                        break;
                    }
                }

                Thread.Sleep(1);
            }

            GLWrapper.FlushCurrentBatch();

            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
                Window.SwapBuffers();
        }

        protected volatile bool ExitRequested;

        public void Exit()
        {
            InputThread.Scheduler.Add(delegate
            {
                ExitRequested = true;

                threads.ForEach(t => t.Exit());
                threads.Where(t => t.Running).ForEach(t => t.Thread.Join());
                Window?.Close();
            }, false);
        }

        public virtual void Run()
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            DrawThread.Start();
            UpdateThread.Start();

            if (Window != null)
            {
                Window.KeyDown += window_KeyDown;
                Window.Resize += window_ClientSizeChanged;
                Window.ExitRequested += OnExitRequested;
                Window.Exited += OnExited;
                Window.Title = $@"osu.Framework (running ""{Name}"")";
                Window.FocusedChanged += delegate { setActive(Window.Focused); };
                window_ClientSizeChanged(null, null);

                try
                {
                    Window.UpdateFrame += delegate
                    {
                        inputPerformanceCollectionPeriod?.Dispose();
                        InputThread.RunUpdate();
                        inputPerformanceCollectionPeriod = InputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);
                    };
                    Window.Run();
                }
                catch (OutOfMemoryException)
                {
                }
            }
            else
            {
                while (!ExitRequested)
                    InputThread.RunUpdate();
            }
        }

        private void window_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (!e.Control)
                return;
            switch (e.Key)
            {
                case Key.F7:
                    if (UpdateThread.ActiveHz == maximumUpdateHz)
                    {
                        UpdateThread.ActiveHz = double.MaxValue;
                        DrawThread.ActiveHz = double.MaxValue;
                    }
                    else
                    {
                        UpdateThread.ActiveHz = maximumUpdateHz;
                        DrawThread.ActiveHz = maximumDrawHz;
                    }
                    break;
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.WindowState == WindowState.Minimized) return;

            var size = Window.ClientSize;
            //When minimizing, there will be an "size zero, but WindowState not Minimized" state.
            if (size.IsEmpty) return;
            UpdateThread.Scheduler.Add(delegate
            {
                //set base.Size here to avoid the override below, which would cause a recursive loop.
                base.Size = new Vector2(size.Width, size.Height);
            });
        }

        public override Vector2 Size
        {
            set
            {
                if (Window != null)
                {
                    if (!Window.Visible)
                    {
                        //set aggressively as we haven't become visible yet
                        Window.ClientSize = new Size((int)value.X, (int)value.Y);
                        Window.CentreToScreen();
                    }
                    else
                    {
                        InputThread.Scheduler.Add(delegate { if (Window != null) Window.ClientSize = new Size((int)value.X, (int)value.Y); });
                    }
                }

                base.Size = value;
            }
        }

        InvokeOnDisposal inputPerformanceCollectionPeriod;

        public override void Add(Drawable drawable)
        {
            // TODO: We may in the future want to hold off on performing _any_ action on game host
            // before its threads have been launched. This requires changing the order from
            // host.Run -> host.Add instead of host.Add -> host.Run.

            Debug.Assert(!Children.Any(), @"Don't load more than one Game in a Host");

            BaseGame game = drawable as BaseGame;

            Debug.Assert(game != null, @"Make sure to load a Game in a Host");

            Dependencies.Cache(game);
            game.SetHost(this);

            if (!IsLoaded)
                PerformLoad(game);

            LoadGame(game);
        }

        protected virtual void WaitUntilReadyToLoad()
        {
            UpdateThread.WaitUntilInitialized();
            DrawThread.WaitUntilInitialized();
        }

        protected virtual void LoadGame(BaseGame game)
        {
            Task.Run(delegate
            {
                // Make sure we are not loading anything game-related before our threads have been initialized.
                WaitUntilReadyToLoad();

                game.PerformLoad(game);
            }).ContinueWith(task => Schedule(() =>
            {
                task.ThrowIfFaulted();
                base.Add(game);
            }));
        }

        public abstract IEnumerable<InputHandler> GetInputHandlers();

        public abstract ITextInputSource GetTextInput();
    }
}
