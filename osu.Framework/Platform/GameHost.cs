// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osu.Framework.IO.File;

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

        protected abstract Storage GetStorage(string baseName);

        public Storage Storage { get; protected set; }

        /// <summary>
        /// If capslock is enabled on the system, false if not overwritten by a subclass
        /// </summary>
        public virtual bool CapsLockEnabled => false;

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

        private readonly Lazy<string> fullPathBacking = new Lazy<string>(() =>
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        });

        public string FullPath => fullPathBacking.Value;

        protected string Name { get; }

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        protected GameHost(string gameName = @"")
        {
            Instance = this;

            AppDomain.CurrentDomain.UnhandledException += exceptionHandler;

            FileSafety.DeleteCleanupDirectory();

            Dependencies.Cache(this);
            Dependencies.Cache(Storage = GetStorage(gameName));

            Name = gameName;
            Logger.GameIdentifier = gameName;

            threads = new List<GameThread>
            {
                (DrawThread = new DrawThread(DrawFrame, @"Draw")
                {
                    OnThreadStart = DrawInitialize,
                }),
                (UpdateThread = new UpdateThread(UpdateFrame, @"Update")
                {
                    OnThreadStart = UpdateInitialize,
                    Monitor = { HandleGC = true },
                }),
                (InputThread = new InputThread(null, @"Input")), //never gets started.
            };

            var path = System.IO.Path.GetDirectoryName(FullPath);
            if (path != null)
                Environment.CurrentDirectory = path;
        }

        private void exceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            Logger.Error(exception, @"fatal error:", recursive: true);

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

        protected virtual void UpdateFrame()
        {
            if (Root == null) return;

            if (Window?.WindowState != WindowState.Minimized)
                Root.Size = Window != null ? new Vector2(Window.ClientSize.Width, Window.ClientSize.Height) :
                    new Vector2(config.Get<int>(FrameworkSetting.Width), config.Get<int>(FrameworkSetting.Height));

            // Ensure we maintain a valid size for any children immediately scaling by the window size
            Root.Size = Vector2.ComponentMax(Vector2.One, Root.Size);

            Root.UpdateSubTree();
            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = Root.GenerateDrawNodeSubtree(buffer.Index, Root.ScreenSpaceDrawQuad.AABBFloat);
        }

        protected virtual void DrawInitialize()
        {
            Window.MakeCurrent();
            GLWrapper.Initialize(this);

            setVSyncMode();

            GLWrapper.Reset(new Vector2(Window.ClientSize.Width, Window.ClientSize.Height));
            GLWrapper.ClearColour(Color4.Black);
        }

        private long lastDrawFrameId;

        protected virtual void DrawFrame()
        {
            if (Root == null)
                return;

            while (!exitInitiated)
            {
                using (var buffer = DrawRoots.Get(UsageType.Read))
                {
                    if (buffer?.Object == null || buffer.FrameId == lastDrawFrameId)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    using (drawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
                    {
                        GLWrapper.Reset(new Vector2(Window.ClientSize.Width, Window.ClientSize.Height));
                        GLWrapper.ClearColour(Color4.Black);
                    }

                    buffer.Object.Draw(null);
                    lastDrawFrameId = buffer.FrameId;
                    break;
                }
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

            resetInputHandlers();

            DrawThread.Start();
            UpdateThread.Start();

            DrawThread.WaitUntilInitialized();
            bootstrapSceneGraph(game);

            frameSyncMode.TriggerChange();
            enabledInputHandlers.TriggerChange();

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

        private void resetInputHandlers()
        {
            if (AvailableInputHandlers != null)
                foreach (var h in AvailableInputHandlers)
                    h.Dispose();

            AvailableInputHandlers = CreateAvailableInputHandlers();
            foreach (var handler in AvailableInputHandlers)
            {
                if (!handler.Initialize(this))
                {
                    handler.Enabled.Value = false;
                    break;
                }

                (handler as IHasCursorSensitivity)?.Sensitivity.BindTo(cursorSensitivity);
            }
        }

        /// <summary>
        /// The clock which is to be used by the scene graph (will be assigned to <see cref="Root"/>).
        /// </summary>
        protected virtual IFrameBasedClock SceneGraphClock => UpdateThread.Clock;

        private void bootstrapSceneGraph(Game game)
        {
            var root = new UserInputManager { Child = game };

            Dependencies.Cache(root);
            Dependencies.Cache(game);

            game.SetHost(this);

            root.Load(SceneGraphClock, Dependencies);

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

        private Bindable<string> enabledInputHandlers;

        private Bindable<double> cursorSensitivity;

        private void setupConfig()
        {
            Dependencies.Cache(debugConfig = new FrameworkDebugConfigManager());
            Dependencies.Cache(config = new FrameworkConfigManager(Storage));
            Dependencies.Cache(Localisation = new LocalisationEngine(config));

            activeGCMode = debugConfig.GetBindable<GCLatencyMode>(DebugSetting.ActiveGCMode);
            activeGCMode.ValueChanged += newMode =>
            {
                GCSettings.LatencyMode = IsActive ? newMode : GCLatencyMode.Interactive;
            };

            frameSyncMode = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            frameSyncMode.ValueChanged += newMode =>
            {
                float refreshRate = DisplayDevice.Default.RefreshRate;
                // For invalid refresh rates let's assume 60 Hz as it is most common.
                if (refreshRate <= 0)
                    refreshRate = 60;

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

            enabledInputHandlers = config.GetBindable<string>(FrameworkSetting.ActiveInputHandlers);
            enabledInputHandlers.ValueChanged += enabledString =>
            {
                var configHandlers = enabledString.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s));
                bool useDefaults = !configHandlers.Any();

                // make sure all the handlers in the configuration file are available, else reset to sane defaults.
                foreach (string handler in configHandlers)
                {
                    if (AvailableInputHandlers.All(h => h.ToString() != handler))
                    {
                        useDefaults = true;
                        break;
                    }
                }

                if (useDefaults)
                {
                    resetInputHandlers();
                    enabledInputHandlers.Value = string.Join(" ", AvailableInputHandlers.Where(h => h.Enabled).Select(h => h.ToString()));
                }
                else
                {
                    foreach (var handler in AvailableInputHandlers)
                    {
                        var handlerType = handler.ToString();
                        handler.Enabled.Value = configHandlers.Any(ch => ch == handlerType);
                    }
                }
            };

            cursorSensitivity = config.GetBindable<double>(FrameworkSetting.CursorSensitivity);
        }

        private void setVSyncMode()
        {
            if (Window == null) return;

            DrawThread.Scheduler.Add(() => Window.VSync = frameSyncMode == FrameSync.VSync ? VSyncMode.On : VSyncMode.Off);
        }

        protected abstract IEnumerable<InputHandler> CreateAvailableInputHandlers();

        public IEnumerable<InputHandler> AvailableInputHandlers { get; private set; }

        public abstract ITextInputSource GetTextInput();

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;
            stopAllThreads();
            Root?.Dispose();

            config?.Dispose();
            debugConfig?.Dispose();

            Logger.Flush();
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

        /// <summary>
        /// Defines the platform-specific key bindings that will be used by <see cref="osu.Framework.Input.PlatformInputManager"/>.
        /// Should be overridden per-platform to provide native key bindings.
        /// </summary>
        public virtual IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.X }), new PlatformAction(PlatformActionType.Cut)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.C }), new PlatformAction(PlatformActionType.Copy)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.V }), new PlatformAction(PlatformActionType.Paste)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.A }), new PlatformAction(PlatformActionType.SelectAll)),
            new KeyBinding(InputKey.Left, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.Right, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.BackSpace, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(InputKey.Delete, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.BackSpace}), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Delete }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(InputKey.Home, new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.End, new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Home }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.End }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
        };
    }
}
