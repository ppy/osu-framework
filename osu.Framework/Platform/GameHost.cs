// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Serialization;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = osuTK.Graphics.ES30.PixelFormat;
using Size = System.Drawing.Size;

namespace osu.Framework.Platform
{
    public abstract class GameHost : IIpcHost, IDisposable
    {
        public IWindow Window { get; private set; }

        public IRenderer Renderer { get; private set; }

        /// <summary>
        /// Whether "unlimited" frame limiter should be allowed to exceed sane limits.
        /// Only use this for benchmarking purposes (see <see cref="maximum_sane_fps"/> for further reasoning).
        /// </summary>
        public bool AllowBenchmarkUnlimitedFrames { get; set; }

        protected FrameworkDebugConfigManager DebugConfig { get; private set; }

        protected FrameworkConfigManager Config { get; private set; }

        private InputConfigManager inputConfig { get; set; }

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
        public readonly AggregateBindable<bool> AllowScreenSuspension = new AggregateBindable<bool>((a, b) => a & b, new Bindable<bool>(true));

        /// <summary>
        /// For IPC messaging purposes, whether this <see cref="GameHost"/> is the primary (bound) host.
        /// </summary>
        public virtual bool IsPrimaryInstance { get; protected set; } = true;

        /// <summary>
        /// Invoked when the game window is activated. Always invoked from the update thread.
        /// </summary>
        public event Action Activated;

        /// <summary>
        /// Invoked when the game window is deactivated. Always invoked from the update thread.
        /// </summary>
        public event Action Deactivated;

        /// <summary>
        /// Invoked when an exit was requested. Always invoked from the update thread.
        /// </summary>
        /// <remarks>
        /// Usually invoked when the window close (X) button or another platform-native exit action has been pressed.
        /// </remarks>
        public event Action ExitRequested;

        public event Action Exited;

        /// <summary>
        /// An unhandled exception was thrown. Return true to ignore and continue running.
        /// </summary>
        public event Func<Exception, bool> ExceptionThrown;

        public event Func<IpcMessage, IpcMessage> MessageReceived;

        /// <summary>
        /// Whether the on screen keyboard covers a portion of the game window when presented to the user.
        /// </summary>
        public virtual bool OnScreenKeyboardOverlapsGameWindow => false;

        /// <summary>
        /// Whether this host can exit (mobile platforms, for instance, do not support exiting the app).
        /// </summary>
        /// <remarks>Also see <see cref="CanSuspendToBackground"/>.</remarks>
        public virtual bool CanExit => true;

        /// <summary>
        /// Whether this host can suspend and minimize to background.
        /// </summary>
        /// <remarks>
        /// This and <see cref="SuspendToBackground"/> are an alternative way to exit on hosts that have <see cref="CanExit"/> <c>false</c>.
        /// </remarks>
        public virtual bool CanSuspendToBackground => false;

        protected IpcMessage OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessageAsync(IpcMessage message) => throw new NotSupportedException("This platform does not implement IPC.");

        /// <summary>
        /// Requests that a file be opened externally with an associated application, if available.
        /// </summary>
        /// <remarks>
        /// Some platforms do not support interacting with files externally (ie. mobile or sandboxed platforms), check the return value as to whether it succeeded.
        /// </remarks>
        /// <param name="filename">The absolute path to the file which should be opened.</param>
        /// <returns>Whether the file was successfully opened.</returns>
        public abstract bool OpenFileExternally(string filename);

        /// <summary>
        /// Requests to present a file externally in the platform's native file browser.
        /// </summary>
        /// <remarks>
        /// This will open the parent folder and, (if available) highlight the file.
        /// Some platforms do not support interacting with files externally (ie. mobile or sandboxed platforms), check the return value as to whether it succeeded.
        /// </remarks>
        /// <example>
        ///     <para>"C:\Windows\explorer.exe" -> opens 'C:\Windows' and highlights 'explorer.exe' in the window.</para>
        ///     <para>"C:\Windows\System32" -> opens 'C:\Windows' and highlights 'System32' in the window.</para>
        ///     <para>"C:\Windows\System32\" -> opens 'C:\Windows\System32' and highlights nothing.</para>
        /// </example>
        /// <param name="filename">The absolute path to the file/folder to be shown in its parent folder.</param>
        /// <returns>Whether the file was successfully presented.</returns>
        public abstract bool PresentFileExternally(string filename);

        /// <summary>
        /// Requests that a URL be opened externally in a web browser, if available.
        /// </summary>
        /// <param name="url">The URL of the page which should be opened.</param>
        public abstract void OpenUrlExternally(string url);

        /// <summary>
        /// Creates the game window for the host. Should be implemented per-platform if required.
        /// </summary>
        protected virtual IWindow CreateWindow() => null;

        [CanBeNull]
        public virtual Clipboard GetClipboard() => null;

        protected virtual ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new ReadableKeyCombinationProvider();

        private ReadableKeyCombinationProvider readableKeyCombinationProvider;

        /// <summary>
        /// The default initial path when requesting a user to select a file/folder.
        /// </summary>
        /// <remarks>
        /// Provides a sane starting point for user-accessible storage.
        /// </remarks>
        public virtual string InitialFileSelectorPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>
        /// Retrieve a storage for the specified location.
        /// </summary>
        /// <param name="path">The absolute path to be used as a root for the storage.</param>
        public abstract Storage GetStorage(string path);

        /// <summary>
        /// All valid user storage paths in order of usage priority.
        /// </summary>
        public virtual IEnumerable<string> UserStoragePaths => Environment.GetFolderPath(Environment.SpecialFolder.Personal).Yield();

        /// <summary>
        /// The main storage as proposed by the host game.
        /// </summary>
        public Storage Storage { get; protected set; }

        /// <summary>
        /// An auxiliary cache storage which is fixed in the default game directory.
        /// </summary>
        public Storage CacheStorage { get; protected set; }

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

            if (thread.Monitor != null)
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

        public DrawThread DrawThread { get; private set; }
        public GameThread UpdateThread { get; private set; }
        public InputThread InputThread { get; private set; }
        public AudioThread AudioThread { get; private set; }

        private double maximumUpdateHz;

        /// <summary>
        /// The target number of update frames per second when the game window is active.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double MaximumUpdateHz
        {
            get => maximumUpdateHz;
            set => threadRunner.MaximumUpdateHz = UpdateThread.ActiveHz = maximumUpdateHz = value;
        }

        private double maximumDrawHz;

        /// <summary>
        /// The target number of draw frames per second when the game window is active.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double MaximumDrawHz
        {
            get => maximumDrawHz;
            set => DrawThread.ActiveHz = maximumDrawHz = value;
        }

        /// <summary>
        /// The target number of updates per second when the game window is inactive.
        /// This is applied to all threads.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
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

        /// <summary>
        /// The name of the game to be hosted.
        /// </summary>
        public string Name { get; }

        [NotNull]
        public HostOptions Options { get; private set; }

        public DependencyContainer Dependencies { get; } = new DependencyContainer();

        private bool suspended;

        protected GameHost([NotNull] string gameName, [CanBeNull] HostOptions options = null)
        {
            Options = options ?? new HostOptions();

            Name = gameName;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter() }
            };
        }

        protected virtual IRenderer CreateRenderer() => new GLRenderer();

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

            logException(exception, "unhandled");
            abortExecutionFromException(sender, exception, args.IsTerminating);
        }

        private void unobservedExceptionHandler(object sender, UnobservedTaskExceptionEventArgs args)
        {
            var actualException = args.Exception.AsSingular();

            // unobserved exceptions are logged but left unhandled (most of the time they are not intended to be critical).
            logException(actualException, "unobserved");

            if (DebugUtils.IsNUnitRunning)
                abortExecutionFromException(sender, actualException, false);
        }

        private void logException(Exception exception, string type)
        {
            Logger.Error(exception, $"An {type} error has occurred.", recursive: true);
        }

        /// <summary>
        /// Give the running application a last change to handle an otherwise unhandled exception, and potentially ignore it.
        /// </summary>
        /// <param name="sender">The source, generally a <see cref="GameThread"/>.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <param name="isTerminating">Whether the CLR is terminating.</param>
        private void abortExecutionFromException(object sender, Exception exception, bool isTerminating)
        {
            // nothing needs to be done if the consumer has requested continuing execution.
            if (ExceptionThrown?.Invoke(exception) == true) return;

            // otherwise, we need to unwind and abort execution.

            AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException -= unobservedExceptionHandler;

            // In the case of an unhandled exception, it's feasible that the disposal flow for `GameHost` doesn't run.
            // This can result in the exception not being logged (or being partially logged) due to the logger running asynchronously.
            // We force flushing the logger here to ensure logging completes.
            Logger.Flush();

            var captured = ExceptionDispatchInfo.Capture(exception);
            var thrownEvent = new ManualResetEventSlim(false);

            //we want to throw this exception on the input thread to interrupt window and also headless execution.
            InputThread.Scheduler.Add(() =>
            {
                try
                {
                    captured.Throw();
                }
                finally
                {
                    thrownEvent.Set();
                }
            });

            // Stopping running threads before the exception is rethrown on the input thread causes some debuggers (e.g. Rider 2020.2) to not properly display the stack.
            // To avoid this, pause the exceptioning thread until the rethrow takes place.
            waitForThrow();

            void waitForThrow()
            {
                // This is bypassed for sources in a few situations where deadlocks can occur:
                // 1. When the exceptioning thread is GameThread.Input.
                // 2. When the game is running in single-threaded mode. Single threaded stacks will be displayed correctly at the point of rethrow.
                // 3. When the CLR is terminating. We can't guarantee the input thread is still running, and may delay application termination.
                if (isTerminating || (sender is GameThread && (sender == InputThread || executionMode.Value == ExecutionMode.SingleThread)))
                    return;

                // The process can deadlock in an extreme case such as the input thread dying before the delegate executes, so wait up to a maximum of 10 seconds at all times.
                thrownEvent.Wait(TimeSpan.FromSeconds(10));
            }
        }

        protected virtual void OnActivated() => UpdateThread.Scheduler.Add(() => Activated?.Invoke());

        protected virtual void OnDeactivated() => UpdateThread.Scheduler.Add(() => Deactivated?.Invoke());

        protected void OnExitRequested() => UpdateThread.Scheduler.Add(() => ExitRequested?.Invoke());

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
            else if (Window.WindowState != WindowState.Minimised)
                Root.Size = new Vector2(Window.ClientSize.Width, Window.ClientSize.Height);

            // Ensure we maintain a valid size for any children immediately scaling by the window size
            Root.Size = Vector2.ComponentMax(Vector2.One, Root.Size);

            TypePerformanceMonitor.NewFrame();

            Root.UpdateSubTree();
            Root.UpdateSubTreeMasking(Root, Root.ScreenSpaceDrawQuad.AABBFloat);

            using (var buffer = DrawRoots.GetForWrite())
                buffer.Object = Root.GenerateDrawNodeSubtree(frameCount, buffer.Index, false);
        }

        private readonly DepthValue depthValue = new DepthValue();

        protected virtual void DrawFrame()
        {
            if (Root == null)
                return;

            if (ExecutionState != ExecutionState.Running)
                return;

            ObjectUsage<DrawNode> buffer;

            using (drawMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                buffer = DrawRoots.GetForRead();

            if (buffer == null)
                return;

            try
            {
                using (drawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
                    Renderer.BeginFrame(new Vector2(Window.ClientSize.Width, Window.ClientSize.Height));

                if (!bypassFrontToBackPass.Value)
                {
                    depthValue.Reset();

                    GL.ColorMask(false, false, false, false);
                    Renderer.SetBlend(BlendingParameters.None);
                    Renderer.PushDepthInfo(DepthInfo.Default);

                    // Front pass
                    buffer.Object.DrawOpaqueInteriorSubTree(Renderer, depthValue);

                    Renderer.PopDepthInfo();
                    GL.ColorMask(true, true, true, true);

                    // The back pass doesn't write depth, but needs to depth test properly
                    Renderer.PushDepthInfo(new DepthInfo(true, false));
                }
                else
                {
                    // Disable depth testing
                    Renderer.PushDepthInfo(new DepthInfo());
                }

                // Back pass
                buffer.Object.Draw(Renderer);

                Renderer.PopDepthInfo();

                Renderer.FinishFrame();

                using (drawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
                {
                    Swap();
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        /// <summary>
        /// Swap the buffers.
        /// </summary>
        protected virtual void Swap()
        {
            Window.SwapBuffers();

            if (Window.VerticalSync)
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
                int width = Window.ClientSize.Width;
                int height = Window.ClientSize.Height;
                var pixelData = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<Rgba32>(width * height);

                DrawThread.Scheduler.Add(() =>
                {
                    if (Window is SDL2DesktopWindow win)
                        win.MakeCurrent();
                    else if (GraphicsContext.CurrentContext == null)
                        throw new GraphicsContextMissingException();

                    GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, ref MemoryMarshal.GetReference(pixelData.Memory.Span));

                    // ReSharper disable once AccessToDisposedClosure
                    completionEvent.Set();
                });

                // this is required as attempting to use a TaskCompletionSource blocks the thread calling SetResult on some configurations.
                // ReSharper disable once AccessToDisposedClosure
                if (!await Task.Run(() => completionEvent.Wait(5000)).ConfigureAwait(false))
                    throw new TimeoutException("Screenshot data did not arrive in a timely fashion");

                var image = Image.LoadPixelData<Rgba32>(pixelData.Memory.Span, width, height);
                image.Mutate(c => c.Flip(FlipMode.Vertical));

                return image;
            }
        }

        public ExecutionState ExecutionState
        {
            get => executionState;
            private set
            {
                if (executionState == value)
                    return;

                executionState = value;
                Logger.Log($"Host execution state changed to {value}");
            }
        }

        private ExecutionState executionState;

        /// <summary>
        /// Schedules the game to exit in the next frame.
        /// </summary>
        /// <remarks>Consider using <see cref="SuspendToBackground"/> on mobile platforms that can't exit normally.</remarks>
        public void Exit()
        {
            if (CanExit)
                PerformExit(false);
        }

        /// <summary>
        /// Suspends and minimizes the game to background.
        /// </summary>
        /// <remarks>
        /// This is provided as an alternative to <see cref="Exit"/> on hosts that can't exit (see <see cref="CanExit"/>).
        /// Should only be called if <see cref="CanSuspendToBackground"/> is <c>true</c>.
        /// </remarks>
        /// <returns><c>true</c> if the game was successfully suspended and minimized.</returns>
        public virtual bool SuspendToBackground()
        {
            return false;
        }

        /// <summary>
        /// Schedules the game to exit in the next frame (or immediately if <paramref name="immediately"/> is true).
        /// </summary>
        /// <remarks>
        /// Will never be called if <see cref="CanExit"/> is <see langword="false"/>.
        /// </remarks>
        /// <param name="immediately">If true, exits the game immediately.  If false (default), schedules the game to exit in the next frame.</param>
        protected virtual void PerformExit(bool immediately) => performExit(immediately);

        private void performExit(bool immediately)
        {
            if (executionState == ExecutionState.Stopped || executionState == ExecutionState.Idle)
                return;

            ExecutionState = ExecutionState.Stopping;

            if (immediately)
                exit();
            else
                InputThread.Scheduler.Add(exit, false);

            void exit()
            {
                Debug.Assert(ExecutionState == ExecutionState.Stopping);

                Window?.Close();
                threadRunner.Stop();

                ExecutionState = ExecutionState.Stopped;
                stoppedEvent.Set();
            }
        }

        private static readonly SemaphoreSlim host_running_mutex = new SemaphoreSlim(1);

        public void Run(Game game)
        {
            if (Thread.CurrentThread.IsThreadPoolThread)
            {
                // This is a common misuse of GameHost, where typically consumers will have a mutex waiting for the game to run.
                // Exceptions thrown here will become unobserved, so any such mutexes will never be set.
                // Instead, immediately terminate the application in order to notify of incorrect use in all cases.
                Environment.FailFast($"{nameof(GameHost)}s should not be run on a TPL thread (use TaskCreationOptions.LongRunning).");
            }

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            if (ExecutionState != ExecutionState.Idle)
                throw new InvalidOperationException("A game that has already been run cannot be restarted.");

            Renderer = CreateRenderer();

            try
            {
                if (!host_running_mutex.Wait(10000))
                    throw new TimeoutException($"This {nameof(GameHost)} could not start {game} because another {nameof(GameHost)} was already running.");

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

                Logger.GameIdentifier = Name;
                Logger.VersionIdentifier = assembly.GetName().Version?.ToString() ?? Logger.VersionIdentifier;

                Dependencies.CacheAs(this);
                Dependencies.CacheAs(Storage = game.CreateStorage(this, GetDefaultGameStorage()));
                Dependencies.CacheAs(Renderer);

                CacheStorage = GetDefaultGameStorage().GetStorageForDirectory("cache");

                SetupForRun();

                Window = CreateWindow();

                populateInputHandlers();

                SetupConfig(game.GetFrameworkConfigDefaults() ?? new Dictionary<FrameworkSetting, object>());

                initialiseInputHandlers();

                if (Window != null)
                {
                    Window.SetupWindow(Config);

                    Window.Create();
                    Window.Title = $@"osu!framework (running ""{Name}"")";

                    currentDisplayMode = Window.CurrentDisplayMode.GetBoundCopy();
                    currentDisplayMode.BindValueChanged(_ => updateFrameSyncMode());

                    IsActive.BindTo(Window.IsActive);
                }

                Dependencies.CacheAs(readableKeyCombinationProvider = CreateReadableKeyCombinationProvider());
                Dependencies.CacheAs(CreateTextInput());

                ExecutionState = ExecutionState.Running;
                threadRunner.Start();

                DrawThread.WaitUntilInitialized();

                bootstrapSceneGraph(game);

                frameSyncMode.TriggerChange();

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
                        switch (Window)
                        {
                            case SDL2DesktopWindow window:
                                window.Update += windowUpdate;
                                break;

                            case OsuTKWindow tkWindow:
                                tkWindow.UpdateFrame += (_, _) => windowUpdate();
                                break;
                        }

                        Window.ExitRequested += OnExitRequested;
                        Window.Exited += OnExited;
                        Window.KeymapChanged += readableKeyCombinationProvider.OnKeymapChanged;

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
                if (CanExit)
                {
                    // Close the window and stop all threads
                    performExit(true);

                    host_running_mutex.Release();
                }
            }
        }

        /// <summary>
        /// Finds the default <see cref="Storage"/> for the game to be used if <see cref="Game.CreateStorage"/> is not overridden.
        /// </summary>
        /// <returns>The <see cref="Storage"/>.</returns>
        protected virtual Storage GetDefaultGameStorage()
        {
            // first check all valid paths for any existing install.
            foreach (string path in UserStoragePaths)
            {
                var storage = GetStorage(path);

                // if an existing data directory exists for this application, prefer it immediately.
                if (storage.ExistsDirectory(Name))
                    return storage.GetStorageForDirectory(Name);
            }

            // if an existing directory could not be found, use the first path that can be created.
            foreach (string path in UserStoragePaths)
            {
                try
                {
                    return GetStorage(path).GetStorageForDirectory(Name);
                }
                catch
                {
                    // may fail on directory creation.
                }
            }

            throw new InvalidOperationException("No valid user storage path could be resolved.");
        }

        /// <summary>
        /// Pauses all active threads. Call <see cref="Resume"/> to resume execution.
        /// </summary>
        public void Suspend()
        {
            suspended = true;
            threadRunner.Suspend();
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
            Logger.Enabled = true;
        }

        private void populateInputHandlers()
        {
            AvailableInputHandlers = CreateAvailableInputHandlers().ToImmutableArray();
        }

        private void initialiseInputHandlers()
        {
            foreach (var handler in AvailableInputHandlers)
            {
                if (!handler.Initialize(this))
                    handler.Enabled.Value = false;
            }
        }

        /// <summary>
        /// Reset all input handlers' settings to a default state.
        /// </summary>
        public void ResetInputHandlers()
        {
            // restore any disable handlers per legacy configuration.
            ignoredInputHandlers.TriggerChange();

            foreach (var handler in AvailableInputHandlers)
            {
                handler.Reset();
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

        private IBindable<DisplayMode> currentDisplayMode;

        private Bindable<string> ignoredInputHandlers;

        private readonly Bindable<double> cursorSensitivity = new Bindable<double>(1);

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
            frameSyncMode.ValueChanged += _ => updateFrameSyncMode();

#pragma warning disable 618
            // pragma region can be removed 20210911
            ignoredInputHandlers = Config.GetBindable<string>(FrameworkSetting.IgnoredInputHandlers);
            ignoredInputHandlers.ValueChanged += e =>
            {
                var configIgnores = e.NewValue.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s));

                foreach (var handler in AvailableInputHandlers)
                {
                    string handlerType = handler.ToString();
                    handler.Enabled.Value = configIgnores.All(ch => ch != handlerType);
                }
            };

            Config.BindWith(FrameworkSetting.CursorSensitivity, cursorSensitivity);

            var cursorSensitivityHandlers = AvailableInputHandlers.OfType<IHasCursorSensitivity>();

            // one way bindings to preserve compatibility.
            cursorSensitivity.BindValueChanged(val =>
            {
                foreach (var h in cursorSensitivityHandlers)
                    h.Sensitivity.Value = val.NewValue;
            }, true);

            foreach (var h in cursorSensitivityHandlers)
                h.Sensitivity.BindValueChanged(s => cursorSensitivity.Value = s.NewValue);
#pragma warning restore 618

            PerformanceLogging.BindValueChanged(logging =>
            {
                Threads.ForEach(t =>
                {
                    if (t.Monitor != null)
                        t.Monitor.EnablePerformanceProfiling = logging.NewValue;
                });
                DebugUtils.LogPerformanceIssues = logging.NewValue;
                TypePerformanceMonitor.Active = logging.NewValue;
            }, true);

            bypassFrontToBackPass = DebugConfig.GetBindable<bool>(DebugSetting.BypassFrontToBackPass);

            threadLocale = Config.GetBindable<string>(FrameworkSetting.Locale);
            threadLocale.BindValueChanged(locale =>
            {
                // return value of TryGet ignored as the failure case gives expected results (CultureInfo.InvariantCulture)
                CultureInfoHelper.TryGetCultureInfo(locale.NewValue, out var culture);

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                threadRunner.SetCulture(culture);
            }, true);

            inputConfig = new InputConfigManager(Storage, AvailableInputHandlers);
        }

        /// <summary>
        /// Games using osu!framework can generally run at *very* high frame rates when not much is going on.
        ///
        /// This can be counter-productive due to the induced allocation and GPU overhead.
        /// - Allocation overhead can lead to excess garbage collection
        /// - GPU overhead can lead to unexpected pipeline blocking (and stutters as a result).
        ///   Also, in general graphics card manufacturers do not test their hardware at insane frame rates and
        ///   therefore drivers are not optimised to handle this kind of throughput.
        /// - We only harvest input at 1000hz, so running any higher has zero benefits.
        ///
        /// We limit things to the same rate we poll input at, to keep both gamers and their systems happy
        /// and (more) stutter-free.
        /// </summary>
        private const int maximum_sane_fps = GameThread.DEFAULT_ACTIVE_HZ;

        private void updateFrameSyncMode()
        {
            if (Window == null)
                return;

            int refreshRate = Window.CurrentDisplayMode.Value.RefreshRate;

            // For invalid refresh rates let's assume 60 Hz as it is most common.
            if (refreshRate <= 0)
                refreshRate = 60;

            int drawLimiter = refreshRate;
            int updateLimiter = drawLimiter * 2;

            setVSyncMode();

            switch (frameSyncMode.Value)
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
                    drawLimiter = int.MaxValue;
                    updateLimiter = int.MaxValue;
                    break;
            }

            if (!AllowBenchmarkUnlimitedFrames)
            {
                drawLimiter = Math.Min(maximum_sane_fps, drawLimiter);
                updateLimiter = Math.Min(maximum_sane_fps, updateLimiter);
            }

            MaximumDrawHz = drawLimiter;
            MaximumUpdateHz = updateLimiter;
        }

        private void setVSyncMode()
        {
            if (Window == null) return;

            DrawThread.Scheduler.Add(() => Window.VerticalSync = frameSyncMode.Value == FrameSync.VSync);
        }

        /// <summary>
        /// Construct all input handlers for this host. The order here decides the priority given to handlers, with the earliest occurring having higher priority.
        /// </summary>
        protected abstract IEnumerable<InputHandler> CreateAvailableInputHandlers();

        public ImmutableArray<InputHandler> AvailableInputHandlers { get; private set; }

        protected virtual TextInputSource CreateTextInput() => new TextInputSource();

        #region IDisposable Support

        private bool isDisposed;

        private readonly ManualResetEventSlim stoppedEvent = new ManualResetEventSlim(false);

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            switch (ExecutionState)
            {
                case ExecutionState.Running:
                    throw new InvalidOperationException($"{nameof(Exit)} must be called before the {nameof(GameHost)} is disposed.");

                case ExecutionState.Stopping:
                case ExecutionState.Stopped:
                    // Delay disposal until the game has exited
                    if (!stoppedEvent.Wait(60000))
                        throw new InvalidOperationException("Game stuck in runnning state.");

                    break;
            }

            AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException -= unobservedExceptionHandler;

            Root?.Dispose();
            Root = null;

            stoppedEvent.Dispose();

            inputConfig?.Dispose();
            Config?.Dispose();
            DebugConfig?.Dispose();

            Window?.Dispose();

            LoadingComponentsLogger.LogAndFlush();
            Logger.Flush();
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
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.X), PlatformAction.Cut),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.C), PlatformAction.Copy),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.V), PlatformAction.Paste),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.A), PlatformAction.SelectAll),
            new KeyBinding(InputKey.Left, PlatformAction.MoveBackwardChar),
            new KeyBinding(InputKey.Right, PlatformAction.MoveForwardChar),
            new KeyBinding(InputKey.BackSpace, PlatformAction.DeleteBackwardChar),
            new KeyBinding(InputKey.Delete, PlatformAction.DeleteForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.BackSpace), PlatformAction.DeleteBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Left), PlatformAction.MoveBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Right), PlatformAction.MoveForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.BackSpace), PlatformAction.DeleteBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Delete), PlatformAction.DeleteForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardWord),
            new KeyBinding(InputKey.Home, PlatformAction.MoveBackwardLine),
            new KeyBinding(InputKey.End, PlatformAction.MoveForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Home), PlatformAction.SelectBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.End), PlatformAction.SelectForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.PageUp), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.PageDown), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.W), PlatformAction.DocumentClose),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.F4), PlatformAction.DocumentClose),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.N), PlatformAction.DocumentNew),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.T), PlatformAction.TabNew),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.T), PlatformAction.TabRestore),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.S), PlatformAction.Save),
            new KeyBinding(InputKey.Home, PlatformAction.MoveToListStart),
            new KeyBinding(InputKey.End, PlatformAction.MoveToListEnd),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Z), PlatformAction.Undo),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Y), PlatformAction.Redo),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Z), PlatformAction.Redo),
            new KeyBinding(InputKey.Delete, PlatformAction.Delete),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Plus), PlatformAction.ZoomIn),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.KeypadPlus), PlatformAction.ZoomIn),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Minus), PlatformAction.ZoomOut),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.KeypadMinus), PlatformAction.ZoomOut),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Number0), PlatformAction.ZoomDefault),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Keypad0), PlatformAction.ZoomDefault),
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
        /// <returns>An instance of <see cref="VideoDecoder"/> initialised with the given stream.</returns>
        public virtual VideoDecoder CreateVideoDecoder(Stream stream) => new VideoDecoder(Renderer, stream);

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
