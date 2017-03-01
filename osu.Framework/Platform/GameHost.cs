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
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Input;
using OpenTK.Graphics;

namespace osu.Framework.Platform
{
    public abstract class GameHost : IIpcHost, IDisposable
    {
        public static GameHost Instance;

        public GameWindow Window;

        private FrameworkDebugConfigManager debugConfig;

        private FrameworkConfigManager config;

        private void setActive(bool isActive)
        {
            threads.ForEach(t => t.IsActive = isActive);

            setLatencyMode();

            if (isActive)
                Activated?.Invoke();
            else
                Deactivated?.Invoke();
        }

        private void setLatencyMode() => GCSettings.LatencyMode = IsActive ? activeGCMode : GCLatencyMode.Interactive;

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

        private PerformanceMonitor inputMonitor => InputThread.Monitor;
        private PerformanceMonitor drawMonitor => DrawThread.Monitor;

        private Cached<string> fullPathBacking = new Cached<string>();
        public string FullPath => fullPathBacking.EnsureValid() ? fullPathBacking.Value : fullPathBacking.Refresh(() =>
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        });

        protected string Name { get; }

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        protected GameHost(string gameName = @"")
        {
            Instance = this;

            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;

            Dependencies.Cache(this);
            Name = gameName;

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

            MaximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;
            MaximumDrawHz = (DisplayDevice.Default?.RefreshRate ?? 0) * 4;

            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(FullPath);
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

        protected virtual void OnActivated() => UpdateThread.Scheduler.Add(() => setActive(true));

        protected virtual void OnDeactivated() => UpdateThread.Scheduler.Add(() => setActive(false));

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

        protected Container Root;

        protected void UpdateFrame()
        {
            if (Root == null) return;

            if (Window?.WindowState != WindowState.Minimized)
                Root.Size = Window != null ? new Vector2(Window.ClientSize.Width, Window.ClientSize.Height) : Vector2.One;

            Root.UpdateSubTree();
            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = Root.GenerateDrawNodeSubtree(buffer.Index, Root.ScreenSpaceDrawQuad.AABBFloat);
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
            if (Root == null)
                return;

            using (drawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
            {
                GLWrapper.Reset(Root.DrawSize);
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

            using (drawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
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

        public void Run(Game game)
        {
            setupConfig();

            if (Window != null)
            {
                Window.SetupWindow(config);
                Window.Title = $@"osu.Framework (running ""{Name}"")";
            }

            Task.Run(() => bootstrapSceneGraph(game));

            DrawThread.Start();
            UpdateThread.Start();

            try
            {
                if (Window != null)
                {
                    setActive(Window.Focused);

                    Window.KeyDown += window_KeyDown;

                    Window.ExitRequested += OnExitRequested;
                    Window.Exited += OnExited;
                    Window.FocusedChanged += delegate { setActive(Window.Focused); };

                    Window.UpdateFrame += delegate
                    {
                        inputPerformanceCollectionPeriod?.Dispose();
                        InputThread.RunUpdate();
                        inputPerformanceCollectionPeriod = inputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);
                    };
                    Window.Closed += delegate
                    {
                        //we need to ensure all threads have stopped before the window is closed (mainly the draw thread
                        //to avoid GL operations running post-cleanup).
                        stopAllThreads();
                    };

                    Window.Run();
                }
                else
                {
                    while (!exitCompleted)
                        InputThread.RunUpdate();
                }
            }
            catch (OutOfMemoryException)
            {
            }
        }

        private void bootstrapSceneGraph(Game game)
        {
            var root = new UserInputManager(this)
            {
                Clock = UpdateThread.Clock,
                Children = new[] { game },
            };

            Dependencies.Cache(root);
            Dependencies.Cache(game);

            game.SetHost(this);

            root.Load(game);

            //publish bootstrapped scene graph to all threads.
            Root = root;
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

        InvokeOnDisposal inputPerformanceCollectionPeriod;

        private Bindable<GCLatencyMode> activeGCMode;

        private void setupConfig()
        {
            Dependencies.Cache(debugConfig = new FrameworkDebugConfigManager());
            Dependencies.Cache(config = new FrameworkConfigManager(Storage));

            activeGCMode = debugConfig.GetBindable<GCLatencyMode>(FrameworkDebugConfig.ActiveGCMode);
            activeGCMode.ValueChanged += delegate { setLatencyMode(); };
        }

        public abstract IEnumerable<InputHandler> GetInputHandlers();

        public abstract ITextInputSource GetTextInput();

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~GameHost()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
