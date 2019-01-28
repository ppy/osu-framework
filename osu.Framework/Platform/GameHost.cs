﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osuTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osu.Framework.IO.File;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;

namespace osu.Framework.Platform
{
    public abstract class GameHost : IIpcHost, IDisposable
    {
        public GameWindow Window { get; protected set; }

        private readonly Toolkit toolkit;

        private FrameworkDebugConfigManager debugConfig;

        private FrameworkConfigManager config;

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

        /// <summary>
        /// Whether the on screen keyboard covers a portion of the game window when presented to the user.
        /// </summary>
        public virtual bool OnScreenKeyboardOverlapsGameWindow => false;

        /// <summary>
        /// Whether this host can exit (mobile platforms, for instance, do not support exiting the app).
        /// </summary>
        public virtual bool CanExit => true;

        protected void OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessageAsync(IpcMessage message)
        {
            throw new NotSupportedException("This platform does not implement IPC.");
        }

        /// <summary>
        /// Requests that a file be opened externally with an associated application, if available.
        /// </summary>
        /// <param name="filename">The absolute path to the file which should be opened.</param>
        public abstract void OpenFileExternally(string filename);

        /// <summary>
        /// Requests that a URL be opened externally in a web browser, if available.
        /// </summary>
        /// <param name="url">The URL of the page which should be opened.</param>
        public abstract void OpenUrlExternally(string url);

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
            t.UnhandledException = unhandledExceptionHandler;
            t.Monitor.EnablePerformanceProfiling = performanceLogging;
        }

        public GameThread DrawThread;
        public GameThread UpdateThread;
        public InputThread InputThread;

        private double maximumUpdateHz;

        public double MaximumUpdateHz
        {
            get => maximumUpdateHz;
            set => UpdateThread.ActiveHz = maximumUpdateHz = value;
        }

        private double maximumDrawHz;

        public double MaximumDrawHz
        {
            get => maximumDrawHz;
            set => DrawThread.ActiveHz = maximumDrawHz = value;
        }

        public double MaximumInactiveHz
        {
            get => DrawThread.InactiveHz;
            set
            {
                DrawThread.InactiveHz = value;
                UpdateThread.InactiveHz = value;
            }
        }

        private PerformanceMonitor inputMonitor => InputThread.Monitor;
        private PerformanceMonitor drawMonitor => DrawThread.Monitor;

        private readonly Lazy<string> fullPathBacking = new Lazy<string>(RuntimeInfo.GetFrameworkAssemblyPath);

        public string FullPath => fullPathBacking.Value;

        protected string Name { get; }

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        protected GameHost(string gameName = @"", ToolkitOptions toolkitOptions = default)
        {
            toolkit = toolkitOptions != null ? Toolkit.Init(toolkitOptions) : Toolkit.Init();

            AppDomain.CurrentDomain.UnhandledException += unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += unobservedExceptionHandler;

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());

            FileSafety.DeleteCleanupDirectory();

            Dependencies.CacheAs(this);
            Dependencies.CacheAs(Storage = GetStorage(gameName));

            string assemblyPath;
            var assembly = Assembly.GetEntryAssembly();

            // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
            if (assembly == null || assembly.Location.Contains("testhost"))
            {
                assembly = Assembly.GetExecutingAssembly();

                // From nuget, the executing assembly will also be wrong
                assemblyPath = TestContext.CurrentContext.TestDirectory;
            }
            else
                assemblyPath = Path.GetDirectoryName(assembly.Location);

            Name = gameName;

            Logger.GameIdentifier = gameName;
            Logger.VersionIdentifier = assembly.GetName().Version.ToString();

            threads = new List<GameThread>
            {
                (DrawThread = new DrawThread(DrawFrame)
                {
                    OnThreadStart = DrawInitialize,
                    UnhandledException = unhandledExceptionHandler
                }),
                (UpdateThread = new UpdateThread(UpdateFrame)
                {
                    OnThreadStart = UpdateInitialize,
                    Monitor = { HandleGC = true },
                    UnhandledException = unhandledExceptionHandler,
                }),
                (InputThread = new InputThread(null)
                {
                    UnhandledException = unhandledExceptionHandler
                }), //never gets started.
            };

            if (assemblyPath != null)
                Environment.CurrentDirectory = assemblyPath;
        }

        private void unhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = (Exception)args.ExceptionObject;
            exception.Data.Add("unhandled", "unhandled");
            handleException(exception);
        }

        private void unobservedExceptionHandler(object sender, UnobservedTaskExceptionEventArgs args)
        {
            args.Exception.Data.Add("unhandled", "unobserved");
            handleException(args.Exception);
        }

        private void handleException(Exception exception)
        {
            if (ExceptionThrown?.Invoke(exception) != true)
            {
                AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;

                var captured = ExceptionDispatchInfo.Capture(exception);

                //we want to throw this exception on the input thread to interrupt window and also headless execution.
                InputThread.Scheduler.Add(() => { captured.Throw(); });
            }

            Logger.Error(exception, $"An {exception.Data["unhandled"]} error has occurred.", recursive: true);
        }

        protected virtual void OnActivated() => UpdateThread.Scheduler.Add(() => setActive(true));

        protected virtual void OnDeactivated() => UpdateThread.Scheduler.Add(() => setActive(false));

        /// <returns>true to cancel</returns>
        protected virtual bool OnExitRequested()
        {
            if (ExecutionState <= ExecutionState.Stopping) return false;

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

        private ulong frameCount;

        protected virtual void UpdateFrame()
        {
            if (Root == null) return;

            frameCount++;

            if (Window == null)
            {
                var windowedSize = config.Get<Size>(FrameworkSetting.WindowedSize);
                Root.Size = new Vector2(windowedSize.Width, windowedSize.Height);
            }
            else if (Window.WindowState != WindowState.Minimized)
                Root.Size = new Vector2(Window.ClientSize.Width, Window.ClientSize.Height);

            // Ensure we maintain a valid size for any children immediately scaling by the window size
            Root.Size = Vector2.ComponentMax(Vector2.One, Root.Size);

            try
            {
                Root.UpdateSubTree();
            }
            catch (DependencyInjectionException die)
            {
                die.DispatchInfo.Throw();
            }

            Root.UpdateSubTreeMasking(Root, Root.ScreenSpaceDrawQuad.AABBFloat);

            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = Root.GenerateDrawNodeSubtree(frameCount, buffer.Index, false);
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

            while (ExecutionState > ExecutionState.Stopping)
            {
                using (var buffer = DrawRoots.Get(UsageType.Read))
                {
                    if (buffer?.Object == null || buffer.FrameId == lastDrawFrameId)
                    {
                        using (drawMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
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

        /// <summary>
        /// Takes a screenshot of the game. The returned <see cref="Image{TPixel}"/> must be disposed by the caller when applicable.
        /// </summary>
        /// <returns>The screenshot as an <see cref="Image{TPixel}"/>.</returns>
        public async Task<Image<Rgba32>> TakeScreenshotAsync()
        {
            if (Window == null) throw new NullReferenceException(nameof(Window));

            var image = new Image<Rgba32>(Window.ClientSize.Width, Window.ClientSize.Height);

            bool complete = false;

            DrawThread.Scheduler.Add(() =>
            {
                if (GraphicsContext.CurrentContext == null)
                    throw new GraphicsContextMissingException();

                GL.ReadPixels(0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, ref MemoryMarshal.GetReference(image.GetPixelSpan()));

                complete = true;
            });

            // this is required as attempting to use a TaskCompletionSource blocks the thread calling SetResult on some configurations.
            await Task.Run(() =>
            {
                while (!complete)
                    Thread.Sleep(50);
            });

            image.Mutate(c => c.Flip(FlipMode.Vertical));

            return image;
        }

        public ExecutionState ExecutionState { get; private set; }

        /// <summary>
        /// Schedules the game to exit in the next frame.
        /// </summary>
        public void Exit() => PerformExit(false);

        /// <summary>
        /// Schedules the game to exit in the next frame (or immediately if <paramref name="immediately"/> is true).
        /// </summary>
        /// <param name="immediately">If true, exits the game immediately.  If false (default), schedules the game to exit in the next frame.</param>
        protected virtual void PerformExit(bool immediately)
        {
            if (immediately)
                exit();
            else
            {
                ExecutionState = ExecutionState.Stopping;
                InputThread.Scheduler.Add(exit, false);
            }
        }

        /// <summary>
        /// Exits the game. This must always be called from <see cref="InputThread"/>.
        /// </summary>
        private void exit()
        {
            // exit() may be called without having been scheduled from Exit(), so ensure the correct exiting state
            ExecutionState = ExecutionState.Stopping;
            Window?.Close();
            stopAllThreads();
            ExecutionState = ExecutionState.Stopped;
        }

        public void Run(Game game)
        {
            if (ExecutionState != ExecutionState.Idle)
                throw new InvalidOperationException("A game that has already been run cannot be restarted.");

            try
            {
                ExecutionState = ExecutionState.Running;

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
                ignoredInputHandlers.TriggerChange();

                try
                {
                    if (Window != null)
                    {
                        Window.KeyDown += window_KeyDown;

                        Window.ExitRequested += OnExitRequested;
                        Window.Exited += OnExited;
                        Window.FocusedChanged += delegate { setActive(Window.Focused); };

                        bool initialized = false;

                        Window.UpdateFrame += delegate
                        {
                            if (!initialized)
                            {
                                setActive(Window.Focused);
                                initialized = true;
                            }

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
                        while (ExecutionState != ExecutionState.Stopped)
                            InputThread.RunUpdate();
                    }
                }
                catch (OutOfMemoryException)
                {
                }
            }
            finally
            {
                // Close the window and stop all threads
                PerformExit(true);
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
            var root = game.CreateUserInputManager();
            root.Child = new PlatformActionContainer
            {
                Child = new FrameworkActionContainer
                {
                    Child = game
                }
            };

            Dependencies.Cache(root);
            Dependencies.CacheAs(game);

            game.SetHost(this);

            try
            {
                root.Load(SceneGraphClock, Dependencies);
            }
            catch (DependencyInjectionException die)
            {
                die.DispatchInfo.Throw();
            }

            //publish bootstrapped scene graph to all threads.
            Root = root;
        }

        private const int thread_join_timeout = 30000;

        private void stopAllThreads()
        {
            threads.ForEach(t => t.Exit());
            threads.Where(t => t.Running).ForEach(t =>
            {
                if (!t.Thread.Join(thread_join_timeout))
                    Logger.Log($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
            });

            // as the input thread isn't actually handled by a thread, the above join does not necessarily mean it has been completed to an exiting state.
            while (!InputThread.Exited)
                InputThread.RunUpdate();
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

        private Bindable<string> ignoredInputHandlers;

        private Bindable<double> cursorSensitivity;
        private Bindable<bool> performanceLogging;

        private void setupConfig()
        {
            Dependencies.Cache(debugConfig = new FrameworkDebugConfigManager());
            Dependencies.Cache(config = new FrameworkConfigManager(Storage));

            activeGCMode = debugConfig.GetBindable<GCLatencyMode>(DebugSetting.ActiveGCMode);
            activeGCMode.ValueChanged += newMode => { GCSettings.LatencyMode = IsActive ? newMode : GCLatencyMode.Interactive; };

            frameSyncMode = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            frameSyncMode.ValueChanged += newMode =>
            {
                float refreshRate = DisplayDevice.Default?.RefreshRate ?? 0;
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

            ignoredInputHandlers = config.GetBindable<string>(FrameworkSetting.IgnoredInputHandlers);
            ignoredInputHandlers.ValueChanged += ignoredString =>
            {
                var configIgnores = ignoredString.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s));

                // for now, we always want at least one handler disabled (don't want raw and non-raw mouse at once).
                // Todo: We renamed OpenTK to osuTK, the second condition can be removed after some time has passed
                bool restoreDefaults = !configIgnores.Any() || ignoredString.Contains("OpenTK");

                if (restoreDefaults)
                {
                    resetInputHandlers();
                    ignoredInputHandlers.Value = string.Join(" ", AvailableInputHandlers.Where(h => !h.Enabled).Select(h => h.ToString()));
                }
                else
                {
                    foreach (var handler in AvailableInputHandlers)
                    {
                        var handlerType = handler.ToString();
                        handler.Enabled.Value = configIgnores.All(ch => ch != handlerType);
                    }
                }
            };

            cursorSensitivity = config.GetBindable<double>(FrameworkSetting.CursorSensitivity);

            performanceLogging = config.GetBindable<bool>(FrameworkSetting.PerformanceLogging);
            performanceLogging.BindValueChanged(enabled => threads.ForEach(t => t.Monitor.EnablePerformanceProfiling = enabled), true);
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

            if (ExecutionState > ExecutionState.Stopping)
                throw new InvalidOperationException($"{nameof(Exit)} must be called before the {nameof(GameHost)} is disposed.");

            // Delay disposal until the game has exited
            while (ExecutionState > ExecutionState.Stopped)
                Thread.Sleep(10);

            AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException -= unobservedExceptionHandler;

            Root?.Dispose();
            Root = null;

            config?.Dispose();
            debugConfig?.Dispose();

            Window?.Dispose();

            toolkit?.Dispose();

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
        /// Defines the platform-specific key bindings that will be used by <see cref="PlatformActionContainer"/>.
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
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.BackSpace }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Delete }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.BackSpace }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Delete }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(InputKey.Home, new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.End, new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Home }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.End }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.PageUp }), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.PageDown }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentPrevious)),
        };

        /// <summary>
        /// Create a texture loader store based on an underlying data store.
        /// </summary>
        /// <param name="underlyingStore">The underlying provider of texture data (in arbitrary image formats).</param>
        /// <returns>A texture loader store.</returns>
        public virtual IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new TextureLoaderStore(underlyingStore);
    }

    /// <summary>
    /// The game's execution states. All of these states can only be present once per <see cref="GameHost"/>.
    /// Note: The order of values in this enum matters.
    /// </summary>
    public enum ExecutionState
    {
        /// <summary>
        /// <see cref="GameHost.Run"/> has not been invoked yet.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// The game's execution has completely stopped.
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// The user has invoked <see cref="GameHost.Exit"/>, or the window has been called.
        /// The game is currently awaiting to stop all execution on the correct thread.
        /// </summary>
        Stopping = 2,

        /// <summary>
        /// <see cref="GameHost.Run"/> has been invoked.
        /// </summary>
        Running = 3
    }
}
