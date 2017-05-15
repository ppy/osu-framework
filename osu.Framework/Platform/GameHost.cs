// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
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
using OpenTK;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Input;
using OpenTK.Graphics;
using osu.Framework.Localisation;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Platform
{
    public abstract class GameHost : IIpcHost, IDisposable
    {
        public static GameHost Instance;

        public GameWindow Window;

        private FrameworkDebugConfigManager debugConfig;

        private FrameworkConfigManager config;

        public LocalisationEngine Localisation { get; private set; }

        private void setActive(bool isActive)
        {
            threads.ForEach(t => t.IsActive = isActive);

            activeGCMode.TriggerChange();

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

        /// <summary>
        /// An unhandled exception was thrown. Return true to ignore and continue running.
        /// </summary>
        public event Func<Exception, bool> ExceptionThrown;

        public event Action<IpcMessage> MessageReceived;

        protected void OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessageAsync(IpcMessage message)
        {
            throw new NotSupportedException("This platform does not implement IPC.");
        }

        public virtual Clipboard GetClipboard() => null;

        public virtual Storage Storage { get; protected set; }

        private readonly List<GameThread> threads;

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
            get { return maximumUpdateHz; }

            set { UpdateThread.ActiveHz = maximumUpdateHz = value; }
        }

        private double maximumDrawHz;

        public double MaximumDrawHz
        {
            get { return maximumDrawHz; }

            set { DrawThread.ActiveHz = maximumDrawHz = value; }
        }

        public double MaximumInactiveHz
        {
            get { return DrawThread.InactiveHz; }

            set
            {
                DrawThread.InactiveHz = value;
                UpdateThread.InactiveHz = value;
            }
        }

        private PerformanceMonitor inputMonitor => InputThread.Monitor;
        private PerformanceMonitor drawMonitor => DrawThread.Monitor;

        private Cached<string> fullPathBacking = new Cached<string>();

        public string FullPath => fullPathBacking.EnsureValid()
            ? fullPathBacking.Value
            : fullPathBacking.Refresh(() =>
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

            var path = System.IO.Path.GetDirectoryName(FullPath);
            if (path != null)
                Environment.CurrentDirectory = path;
        }

        private void exceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            var exInfo = ExceptionDispatchInfo.Capture(exception);

            if (ExceptionThrown?.Invoke(exception) != true)
            {
                AppDomain.CurrentDomain.UnhandledException -= exceptionHandler;

                //we want to throw this exception on the input thread to interrupt window and also headless execution.
                InputThread.Scheduler.Add(() => { exInfo.Throw(); });
            }
        }

        protected virtual void OnActivated() => UpdateThread.Scheduler.Add(() => setActive(true));

        protected virtual void OnDeactivated() => UpdateThread.Scheduler.Add(() => setActive(false));

        /// <returns>true to cancel</returns>
        protected virtual bool OnExitRequested()
        {
            if (exitInitiated) return false;

            bool? response = null;

            UpdateThread.Scheduler.Add(delegate { response = Exiting?.Invoke() == true; });

            //wait for a potentially blocking response
            while (!response.HasValue)
                Thread.Sleep(1);

            if (response ?? false)
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
                Root.Size = Window != null ? new Vector2(Window.ClientSize.Width, Window.ClientSize.Height) :
                    new Vector2(config.Get<int>(FrameworkSetting.Width), config.Get<int>(FrameworkSetting.Height));

            Root.UpdateSubTree();
            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = Root.GenerateDrawNodeSubtree(buffer.Index, Root.ScreenSpaceDrawQuad.AABBFloat);
        }

        protected virtual void DrawInitialize()
        {
            Window.MakeCurrent();
            GLWrapper.Initialize(this);

            setVSyncMode();
        }

        private long lastDrawFrameId;

        protected virtual void DrawFrame()
        {
            if (Root == null)
                return;

            using (drawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
            {
                GLWrapper.Reset(Root.DrawSize);
                GLWrapper.ClearColour(Color4.Black);
            }

            while (!exitInitiated)
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
            {
                Window.SwapBuffers();

                if (Window.VSync == VSyncMode.On)
                    // without glFinish, vsync is basically unplayable due to the extra latency introduced.
                    // we will likely want to give the user control over this in the future as an advanced setting.
                    GL.Finish();
            }
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
                Window.Title = $@"osu!framework (running ""{Name}"")";
            }

            DrawThread.Start();
            UpdateThread.Start();

            DrawThread.WaitUntilInitialized();
            bootstrapSceneGraph(game);

            frameSyncMode.TriggerChange();

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
            var root = new UserInputManager { Children = new[] { game } };

            Dependencies.Cache(root);
            Dependencies.Cache(game);

            game.SetHost(this);

            root.Load(UpdateThread.Clock, Dependencies);

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
                    var nextMode = frameSyncMode.Value + 1;
                    if (nextMode > FrameSync.Unlimited)
                        nextMode = FrameSync.VSync;
                    frameSyncMode.Value = nextMode;
                    break;
            }
        }

        private InvokeOnDisposal inputPerformanceCollectionPeriod;

        private Bindable<GCLatencyMode> activeGCMode;

        private Bindable<FrameSync> frameSyncMode;

        private void setupConfig()
        {
            Dependencies.Cache(debugConfig = new FrameworkDebugConfigManager());
            Dependencies.Cache(config = new FrameworkConfigManager(Storage));
            Dependencies.Cache(Localisation = new LocalisationEngine(config));

            activeGCMode = debugConfig.GetBindable<GCLatencyMode>(FrameworkDebugConfig.ActiveGCMode);
            activeGCMode.ValueChanged += newMode =>
            {
                GCSettings.LatencyMode = IsActive ? newMode : GCLatencyMode.Interactive;
            };

            frameSyncMode = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            frameSyncMode.ValueChanged += newMode =>
            {

                float refreshRate = DisplayDevice.Default.RefreshRate;

                float drawLimiter = refreshRate;
                float updateLimiter = drawLimiter * 2;

                setVSyncMode();

                switch (newMode)
                {
                    case FrameSync.VSync:
                        drawLimiter = int.MaxValue;
                        updateLimiter *= 2;
                        break;
                    case FrameSync.Limit2x:
                        drawLimiter *= 2;
                        updateLimiter *= 2;
                        break;
                    case FrameSync.Limit4x:
                        drawLimiter *= 4;
                        updateLimiter *= 4;
                        break;
                    case FrameSync.Limit8x:
                        drawLimiter *= 8;
                        updateLimiter *= 8;
                        break;
                    case FrameSync.Unlimited:
                        drawLimiter = updateLimiter = int.MaxValue;
                        break;
                }

                if (DrawThread != null) DrawThread.ActiveHz = drawLimiter;
                if (UpdateThread != null) UpdateThread.ActiveHz = updateLimiter;
            };
        }

        private void setVSyncMode()
        {
            if (Window == null) return;

            DrawThread.Scheduler.Add(() => Window.VSync = frameSyncMode == FrameSync.VSync ? VSyncMode.On : VSyncMode.Off);
        }

        public abstract IEnumerable<InputHandler> GetInputHandlers();

        public abstract ITextInputSource GetTextInput();

        #region IDisposable Support

        private bool isDisposed;

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
