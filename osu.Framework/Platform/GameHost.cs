// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Development;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using SixLabors.Memory;
using PixelFormat = osuTK.Graphics.ES30.PixelFormat;
using WindowState = osuTK.WindowState;

namespace osu.Framework.Platform
{
    public abstract class GameHost : IIpcHost, IDisposable
    {
        public IWindow Window { get; private set; }

        protected FrameworkDebugConfigManager DebugConfig { get; private set; }

        protected FrameworkConfigManager Config { get; private set; }

        /// <summary>
        /// Whether the <see cref="IWindow"/> is active (in the foreground).
        /// </summary>
        public readonly IBindable<bool> IsActive = new Bindable<bool>(true);

        /// <summary>
        /// Disable any system level timers that might dim or turn off the screen.
        /// </summary>
        /// <remarks>
        /// To preserve battery life on mobile devices, this should be left on whenever possible.
        /// </remarks>
        public readonly Bindable<bool> AllowScreenSuspension = new Bindable<bool>(true);

        public bool IsPrimaryInstance { get; protected set; } = true;

        /// <summary>
        /// Invoked when the game window is activated. Always invoked from the update thread.
        /// </summary>
        public event Action Activated;

        /// <summary>
        /// Invoked when the game window is deactivated. Always invoked from the update thread.
        /// </summary>
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

        /// <summary>
        /// Whether memory constraints should be considered before performance concerns.
        /// </summary>
        protected virtual bool LimitedMemoryEnvironment => false;

        protected void OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessageAsync(IpcMessage message) => throw new NotSupportedException("This platform does not implement IPC.");

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

        /// <summary>
        /// Creates the game window for the host. Should be implemented per-platform if required.
        /// </summary>
        protected virtual IWindow CreateWindow() => null;

        public virtual Clipboard GetClipboard() => null;

        protected abstract Storage GetStorage(string baseName);

        public Storage Storage { get; protected set; }

        /// <summary>
        /// If caps-lock is enabled on the system, false if not overwritten by a subclass
        /// </summary>
        public virtual bool CapsLockEnabled => false;

        public IEnumerable<GameThread> Threads => threadRunner.Threads;

        /// <summary>
        /// Register a thread to be monitored and tracked by this <see cref="GameHost"/>
        /// </summary>
        /// <param name="thread">The thread.</param>
        public void RegisterThread(GameThread thread)
        {
            threadRunner.AddThread(thread);

            thread.IsActive.BindTo(IsActive);
            thread.UnhandledException = unhandledExceptionHandler;
            thread.Monitor.EnablePerformanceProfiling = PerformanceLogging.Value;
        }

        /// <summary>
        /// Unregister a previously registered thread.<see cref="GameHost"/>
        /// </summary>
        /// <param name="thread">The thread.</param>
        public void UnregisterThread(GameThread thread)
        {
            threadRunner.RemoveThread(thread);

            IsActive.UnbindFrom(thread.IsActive);
            thread.UnhandledException = null;
        }

        public DrawThread DrawThread;
        public GameThread UpdateThread;
        public InputThread InputThread;
        public AudioThread AudioThread;

        private double maximumUpdateHz;

        public double MaximumUpdateHz
        {
            get => maximumUpdateHz;
            set => threadRunner.MaximumUpdateHz = UpdateThread.ActiveHz = maximumUpdateHz = value;
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
                threadRunner.MaximumInactiveHz = UpdateThread.InactiveHz = value;
            }
        }

        private PerformanceMonitor inputMonitor => InputThread.Monitor;
        private PerformanceMonitor drawMonitor => DrawThread.Monitor;

        private readonly Lazy<string> fullPathBacking = new Lazy<string>(RuntimeInfo.GetFrameworkAssemblyPath);

        public string FullPath => fullPathBacking.Value;

        protected string Name { get; }

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        private Toolkit toolkit;

        private readonly ToolkitOptions toolkitOptions;

        private bool suspended;

        protected GameHost(string gameName = @"", ToolkitOptions toolkitOptions = default)
        {
            this.toolkitOptions = toolkitOptions;
            Name = gameName;
        }

        /// <summary>
        /// Performs a GC collection and frees all framework caches.
        /// This is a blocking call and should not be invoked during periods of user activity unless memory is critical.
        /// </summary>
        public void Collect()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            GC.Collect();
        }

        private void unhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = (Exception)args.ExceptionObject;
            exception.Data["unhandled"] = "unhandled";
            handleException(exception);
        }

        private void unobservedExceptionHandler(object sender, UnobservedTaskExceptionEventArgs args)
        {
            args.Exception.Data["unhandled"] = "unobserved";
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

        protected virtual void OnActivated() => UpdateThread.Scheduler.Add(() => Activated?.Invoke());

        protected virtual void OnDeactivated() => UpdateThread.Scheduler.Add(() => Deactivated?.Invoke());

        /// <returns>true to cancel</returns>
        protected virtual bool OnExitRequested()
        {
            if (ExecutionState <= ExecutionState.Stopping) return false;

            bool? response = null;

            UpdateThread.Scheduler.Add(delegate { response = Exiting?.Invoke() == true; });

            //wait for a potentially blocking response
            while (!response.HasValue)
            {
                if (ThreadSafety.ExecutionMode == ExecutionMode.SingleThread)
                    threadRunner.RunMainLoop();
                else
                    Thread.Sleep(1);
            }

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

        protected Container Root;

        private ulong frameCount;

        protected virtual void UpdateFrame()
        {
            if (Root == null) return;

            frameCount++;

            if (Window == null)
            {
                var windowedSize = Config.Get<Size>(FrameworkSetting.WindowedSize);
                Root.Size = new Vector2(windowedSize.Width, windowedSize.Height);
            }
            else if (Window.WindowState != WindowState.Minimized)
                Root.Size = new Vector2(Window.ClientSize.Width, Window.ClientSize.Height);

            // Ensure we maintain a valid size for any children immediately scaling by the window size
            Root.Size = Vector2.ComponentMax(Vector2.One, Root.Size);

            TypePerformanceMonitor.NewFrame();

            Root.UpdateSubTree();
            Root.UpdateSubTreeMasking(Root, Root.ScreenSpaceDrawQuad.AABBFloat);

            using (var buffer = DrawRoots.Get(UsageType.Write))
                buffer.Object = Root.GenerateDrawNodeSubtree(frameCount, buffer.Index, false);
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
                        GLWrapper.Reset(new Vector2(Window.ClientSize.Width, Window.ClientSize.Height));

                    if (!bypassFrontToBackPass.Value)
                    {
                        var depthValue = new DepthValue();

                        GLWrapper.PushDepthInfo(DepthInfo.Default);

                        // Front pass
                        buffer.Object.DrawOpaqueInteriorSubTree(depthValue, null);

                        GLWrapper.PopDepthInfo();

                        // The back pass doesn't write depth, but needs to depth test properly
                        GLWrapper.PushDepthInfo(new DepthInfo(true, false));
                    }
                    else
                    {
                        // Disable depth testing
                        GLWrapper.PushDepthInfo(new DepthInfo());
                    }

                    // Back pass
                    buffer.Object.Draw(null);

                    GLWrapper.PopDepthInfo();

                    lastDrawFrameId = buffer.FrameId;
                    break;
                }
            }

            GLWrapper.FlushCurrentBatch();

            using (drawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
            {
                Swap();
            }
        }

        /// <summary>
        /// Swap the buffers.
        /// </summary>
        protected virtual void Swap()
        {
            Window.SwapBuffers();

            if (Window.VSync == VSyncMode.On)
                // without glFinish, vsync is basically unplayable due to the extra latency introduced.
                // we will likely want to give the user control over this in the future as an advanced setting.
                GL.Finish();
        }

        /// <summary>
        /// Takes a screenshot of the game. The returned <see cref="Image{TPixel}"/> must be disposed by the caller when applicable.
        /// </summary>
        /// <returns>The screenshot as an <see cref="Image{TPixel}"/>.</returns>
        public async Task<Image<Rgba32>> TakeScreenshotAsync()
        {
            if (Window == null) throw new InvalidOperationException($"{nameof(Window)} has not been set!");

            using (var completionEvent = new ManualResetEventSlim(false))
            {
                var image = new Image<Rgba32>(Window.ClientSize.Width, Window.ClientSize.Height);

                DrawThread.Scheduler.Add(() =>
                {
                    if (Window is SDLWindow win)
                        win.MakeCurrent();
                    else if (GraphicsContext.CurrentContext == null)
                        throw new GraphicsContextMissingException();

                    GL.ReadPixels(0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, ref MemoryMarshal.GetReference(image.GetPixelSpan()));

                    // ReSharper disable once AccessToDisposedClosure
                    completionEvent.Set();
                });

                // this is required as attempting to use a TaskCompletionSource blocks the thread calling SetResult on some configurations.
                await Task.Run(completionEvent.Wait);

                image.Mutate(c => c.Flip(FlipMode.Vertical));

                return image;
            }
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
            threadRunner.Stop();
            ExecutionState = ExecutionState.Stopped;
            stoppedEvent.Set();
        }

        public void Run(Game game)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            if (LimitedMemoryEnvironment)
            {
                // recommended middle-ground https://github.com/SixLabors/docs/blob/master/articles/ImageSharp/MemoryManagement.md#working-in-memory-constrained-environments
                SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = ArrayPoolMemoryAllocator.CreateWithModeratePooling();
            }

            DebugUtils.HostAssembly = game.GetType().Assembly;

            if (ExecutionState != ExecutionState.Idle)
                throw new InvalidOperationException("A game that has already been run cannot be restarted.");

            try
            {
                SetupToolkit();

                threadRunner = CreateThreadRunner(InputThread = new InputThread());

                AppDomain.CurrentDomain.UnhandledException += unhandledExceptionHandler;
                TaskScheduler.UnobservedTaskException += unobservedExceptionHandler;

                RegisterThread(InputThread);

                RegisterThread(AudioThread = new AudioThread());

                RegisterThread(UpdateThread = new UpdateThread(UpdateFrame, DrawThread)
                {
                    Monitor = { HandleGC = true },
                });

                RegisterThread(DrawThread = new DrawThread(DrawFrame, this));

                Trace.Listeners.Clear();
                Trace.Listeners.Add(new ThrowingTraceListener());

                var assembly = DebugUtils.GetEntryAssembly();
                string assemblyPath = DebugUtils.GetEntryPath();

                Logger.GameIdentifier = Name;
                Logger.VersionIdentifier = assembly.GetName().Version.ToString();

                if (assemblyPath != null)
                    Environment.CurrentDirectory = assemblyPath;

                Dependencies.CacheAs(this);
                Dependencies.CacheAs(Storage = GetStorage(Name));

                SetupForRun();

                Window = CreateWindow();

                ExecutionState = ExecutionState.Running;

                SetupConfig(game.GetFrameworkConfigDefaults() ?? new Dictionary<FrameworkSetting, object>());

                if (Window != null)
                {
                    Window.SetupWindow(Config);
                    Window.Title = $@"osu!framework (running ""{Name}"")";

                    IsActive.BindTo(Window.IsActive);
                }

                resetInputHandlers();

                threadRunner.Start();

                DrawThread.WaitUntilInitialized();

                bootstrapSceneGraph(game);

                frameSyncMode.TriggerChange();
                ignoredInputHandlers.TriggerChange();

                IsActive.BindValueChanged(active =>
                {
                    if (active.NewValue)
                        OnActivated();
                    else
                        OnDeactivated();
                }, true);

                try
                {
                    if (Window != null)
                    {
                        if (Window is SDLWindow window)
                            window.Update += windowUpdate;
                        else
                            Window.UpdateFrame += (o, e) => windowUpdate();

                        Window.ExitRequested += OnExitRequested;
                        Window.Exited += OnExited;

                        //we need to ensure all threads have stopped before the window is closed (mainly the draw thread
                        //to avoid GL operations running post-cleanup).
                        Window.Exited += threadRunner.Stop;

                        Window.Run();
                    }
                    else
                    {
                        while (ExecutionState != ExecutionState.Stopped)
                            windowUpdate();
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

        /// <summary>
        /// Pauses all active threads. Call <see cref="Resume"/> to resume execution.
        /// </summary>
        public void Suspend()
        {
            threadRunner.Suspend();
            suspended = true;
        }

        /// <summary>
        /// Resumes all of the current paused threads after <see cref="Suspend"/> was called.
        /// </summary>
        public void Resume()
        {
            threadRunner.Start();
            suspended = false;
        }

        private ThreadRunner threadRunner;

        private void windowUpdate()
        {
            inputPerformanceCollectionPeriod?.Dispose();
            inputPerformanceCollectionPeriod = null;

            if (suspended)
                return;

            threadRunner.RunMainLoop();

            inputPerformanceCollectionPeriod = inputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);
        }

        /// <summary>
        /// Prepare this game host for <see cref="Run"/>.
        /// <remarks>
        /// <see cref="Storage"/> is available here.
        /// </remarks>
        /// </summary>
        protected virtual void SetupForRun()
        {
            Logger.Storage = Storage.GetStorageForDirectory("logs");
        }

        protected virtual void SetupToolkit()
        {
            toolkit = toolkitOptions != null ? Toolkit.Init(toolkitOptions) : Toolkit.Init();
        }

        private void resetInputHandlers()
        {
            if (AvailableInputHandlers != null)
            {
                foreach (var h in AvailableInputHandlers)
                    h.Dispose();
            }

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
                    Child = new SafeAreaDefiningContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = game
                    }
                }
            };

            Dependencies.Cache(root);
            Dependencies.CacheAs(game);

            game.SetHost(this);

            root.Load(SceneGraphClock, Dependencies);

            //publish bootstrapped scene graph to all threads.
            Root = root;
        }

        private InvokeOnDisposal inputPerformanceCollectionPeriod;

        private Bindable<bool> bypassFrontToBackPass;

        private Bindable<FrameSync> frameSyncMode;

        private Bindable<string> ignoredInputHandlers;

        private Bindable<double> cursorSensitivity;

        public readonly Bindable<bool> PerformanceLogging = new Bindable<bool>();

        private Bindable<WindowMode> windowMode;

        private Bindable<ExecutionMode> executionMode;

        private Bindable<string> threadLocale;

        protected virtual void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.WindowMode))
                defaultOverrides.Add(FrameworkSetting.WindowMode, Window?.DefaultWindowMode ?? WindowMode.Windowed);

            Dependencies.Cache(DebugConfig = new FrameworkDebugConfigManager());
            Dependencies.Cache(Config = new FrameworkConfigManager(Storage, defaultOverrides));

            windowMode = Config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            windowMode.BindValueChanged(mode =>
            {
                if (Window == null)
                    return;

                if (!Window.SupportedWindowModes.Contains(mode.NewValue))
                    windowMode.Value = Window.DefaultWindowMode;
            }, true);

            executionMode = Config.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);
            executionMode.BindValueChanged(e => threadRunner.ExecutionMode = e.NewValue, true);

            frameSyncMode = Config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            frameSyncMode.ValueChanged += e =>
            {
                if (Window == null)
                    return;

                float refreshRate = Window.CurrentDisplay?.RefreshRate ?? 0;
                // For invalid refresh rates let's assume 60 Hz as it is most common.
                if (refreshRate <= 0)
                    refreshRate = 60;

                float drawLimiter = refreshRate;
                float updateLimiter = drawLimiter * 2;

                setVSyncMode();

                switch (e.NewValue)
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

                MaximumDrawHz = drawLimiter;
                MaximumUpdateHz = updateLimiter;
            };

            ignoredInputHandlers = Config.GetBindable<string>(FrameworkSetting.IgnoredInputHandlers);
            ignoredInputHandlers.ValueChanged += e =>
            {
                var configIgnores = e.NewValue.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s));

                // for now, we always want at least one handler disabled (don't want raw and non-raw mouse at once).
                // Todo: We renamed OpenTK to osuTK, the second condition can be removed after some time has passed
                bool restoreDefaults = !configIgnores.Any() || e.NewValue.Contains("OpenTK");

                if (restoreDefaults)
                {
                    resetInputHandlers();
                    ignoredInputHandlers.Value = string.Join(" ", AvailableInputHandlers.Where(h => !h.Enabled.Value).Select(h => h.ToString()));
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

            cursorSensitivity = Config.GetBindable<double>(FrameworkSetting.CursorSensitivity);

            PerformanceLogging.BindValueChanged(logging =>
            {
                Threads.ForEach(t => t.Monitor.EnablePerformanceProfiling = logging.NewValue);
                DebugUtils.LogPerformanceIssues = logging.NewValue;
                TypePerformanceMonitor.Active = logging.NewValue;
            }, true);

            bypassFrontToBackPass = DebugConfig.GetBindable<bool>(DebugSetting.BypassFrontToBackPass);

            threadLocale = Config.GetBindable<string>(FrameworkSetting.Locale);
            threadLocale.BindValueChanged(locale =>
            {
                var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.Name.Equals(locale.NewValue, StringComparison.OrdinalIgnoreCase)) ?? CultureInfo.InvariantCulture;

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                foreach (var t in Threads)
                {
                    t.Scheduler.Add(() => { t.CurrentCulture = culture; });
                }
            }, true);
        }

        private void setVSyncMode()
        {
            if (Window == null) return;

            DrawThread.Scheduler.Add(() => Window.VSync = frameSyncMode.Value == FrameSync.VSync ? VSyncMode.On : VSyncMode.Off);
        }

        protected abstract IEnumerable<InputHandler> CreateAvailableInputHandlers();

        public IEnumerable<InputHandler> AvailableInputHandlers { get; private set; }

        public abstract ITextInputSource GetTextInput();

        #region IDisposable Support

        private bool isDisposed;

        private readonly ManualResetEventSlim stoppedEvent = new ManualResetEventSlim(false);

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (ExecutionState > ExecutionState.Stopping)
                throw new InvalidOperationException($"{nameof(Exit)} must be called before the {nameof(GameHost)} is disposed.");

            // Delay disposal until the game has exited
            stoppedEvent.Wait();

            AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException -= unobservedExceptionHandler;

            Root?.Dispose();
            Root = null;

            stoppedEvent.Dispose();

            Config?.Dispose();
            DebugConfig?.Dispose();

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
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.X), new PlatformAction(PlatformActionType.Cut)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.C), new PlatformAction(PlatformActionType.Copy)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.V), new PlatformAction(PlatformActionType.Paste)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.A), new PlatformAction(PlatformActionType.SelectAll)),
            new KeyBinding(InputKey.Left, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.Right, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.BackSpace, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(InputKey.Delete, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Left), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Right), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.BackSpace), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Left), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Right), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.BackSpace), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Delete), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Left), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Right), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(InputKey.Home, new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.End, new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Home), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.End), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.PageUp), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.PageDown), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.S), new PlatformAction(PlatformActionType.Save)),
            new KeyBinding(InputKey.Home, new PlatformAction(PlatformActionType.ListStart, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.End, new PlatformAction(PlatformActionType.ListEnd, PlatformActionMethod.Move))
        };

        /// <summary>
        /// Create a texture loader store based on an underlying data store.
        /// </summary>
        /// <param name="underlyingStore">The underlying provider of texture data (in arbitrary image formats).</param>
        /// <returns>A texture loader store.</returns>
        public virtual IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new TextureLoaderStore(underlyingStore);

        /// <summary>
        /// Create a <see cref="VideoDecoder"/> with the given stream. May be overridden by platforms that require a different
        /// decoder implementation.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to decode.</param>
        /// <param name="scheduler">The <see cref="Scheduler"/> to use when scheduling tasks from the decoder thread.</param>
        /// <returns>An instance of <see cref="VideoDecoder"/> initialised with the given stream.</returns>
        public virtual VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => new VideoDecoder(stream, scheduler);

        /// <summary>
        /// Creates the <see cref="ThreadRunner"/> to run the threads of this <see cref="GameHost"/>.
        /// </summary>
        /// <param name="mainThread">The main thread.</param>
        /// <returns>The <see cref="ThreadRunner"/>.</returns>
        protected virtual ThreadRunner CreateThreadRunner(InputThread mainThread) => new ThreadRunner(mainThread);
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
