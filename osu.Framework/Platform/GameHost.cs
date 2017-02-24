// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using OpenTK;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Input;
using OpenTK.Graphics;

namespace osu.Framework.Platform
{
    public abstract class GameHost : Container, IIpcHost
    {
        public static GameHost Instance;

        public GameWindow Window;

        private void setActive(bool isActive)
        {
            threads.ForEach(t => t.IsActive = isActive);

            if (isActive)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                Activated?.Invoke();
            }
            else
            {
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                Deactivated?.Invoke();
            }
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

        public virtual Task SendMessageAsync(IpcMessage message)
        {
            throw new NotImplementedException("This platform does not implement IPC.");
        }

        public virtual Clipboard GetClipboard() => null;

        public virtual Storage Storage { get; protected set; } //public set currently required for visualtests setup.

        public override bool IsPresent => true;

        private List<GameThread> threads;

        public IEnumerable<GameThread> Threads => threads;

        public void RegisterThread(GameThread t)
        {
            threads.Add(t);
        }

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

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        protected GameHost(string gameName = @"")
        {
            Instance = this;

            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;

            Dependencies.Cache(this);
            name = gameName;

            threads = new List<GameThread>
            {
                (DrawThread = new GameThread(DrawFrame, @"Draw")
                {
                    OnThreadStart = DrawInitialize,
                }),
                (UpdateThread = new GameThread(UpdateFrame, @"Update")
                {
                    OnThreadStart = UpdateInitialize,
                    Monitor = { HandleGC = true }
                }),
                (InputThread = new InputThread(null, @"Input")) //never gets started.
            };

            Clock = UpdateThread.Clock;

            MaximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;
            MaximumDrawHz = (DisplayDevice.Default?.RefreshRate ?? 0) * 4;

            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(FullPath);

            AddInternal(inputManager = new UserInputManager(this));

            Dependencies.Cache(inputManager);
        }

        private void exceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

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
            if (exitInitiated) return false;

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

        private volatile bool exitInitiated;

        private volatile bool exitCompleted;

        public void Exit()
        {
            exitInitiated = true;

            InputThread.Scheduler.Add(delegate
            {
                Window?.Close();
                stopAllThreads();
                exitCompleted = true;
            }, false);
        }

        public virtual void Run()
        {
            DrawThread.Start();
            UpdateThread.Start();

            if (Window != null)
            {
                setActive(Window.Focused);

                Window.KeyDown += window_KeyDown;
                Window.Resize += window_ClientSizeChanged;
                Window.ExitRequested += OnExitRequested;
                Window.Exited += OnExited;
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
                    Window.Closed += delegate
                    {
                        //we need to ensure all threads have stopped before the window is closed (mainly the draw thread
                        //to avoid GL operations running post-cleanup).
                        stopAllThreads();
                    };

                    Window.Run();
                }
                catch (OutOfMemoryException)
                {
                }
            }
            else
            {
                while (!exitCompleted)
                    InputThread.RunUpdate();
            }
        }

        private void stopAllThreads()
        {
            threads.ForEach(t => t.Exit());
            threads.Where(t => t.Running).ForEach(t => t.Thread.Join());
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

            if (Children.Any())
                throw new InvalidOperationException($"Can not add more than one {nameof(Game)} to a {nameof(GameHost)}.");

            Game game = drawable as Game;
            if (game == null)
                throw new ArgumentException($"Can only add {nameof(Game)} to {nameof(GameHost)}.", nameof(drawable));

            Dependencies.Cache(game);
            game.SetHost(this);

            if (!IsLoaded)
                Load(game);

            LoadGame(game);
        }

        protected virtual void WaitUntilReadyToLoad()
        {
            UpdateThread.WaitUntilInitialized();
            DrawThread.WaitUntilInitialized();
        }

        protected virtual void LoadGame(Game game)
        {
            Task.Run(delegate
            {
                // Make sure we are not loading anything game-related before our threads have been initialized.
                WaitUntilReadyToLoad();

                game.Load(game);
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
