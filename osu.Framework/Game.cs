// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Graphics.Visualisation.Audio;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework
{
    public abstract partial class Game : Container, IKeyBindingHandler<FrameworkAction>, IKeyBindingHandler<PlatformAction>, IHandleGlobalKeyboardInput
    {
        public IWindow Window => Host?.Window;

        public ResourceStore<byte[]> Resources { get; private set; }

        public TextureStore Textures { get; private set; }

        /// <summary>
        /// The filtering mode to use for all textures fetched from <see cref="Textures"/>.
        /// </summary>
        protected virtual TextureFilteringMode DefaultTextureFilteringMode => TextureFilteringMode.Linear;

        protected GameHost Host { get; private set; }

        private readonly Bindable<bool> isActive = new Bindable<bool>(true);

        /// <summary>
        /// Whether the game is active (in the foreground).
        /// </summary>
        public IBindable<bool> IsActive => isActive;

        public AudioManager Audio { get; private set; }

        public ShaderManager Shaders { get; private set; }

        /// <summary>
        /// A store containing fonts accessible game-wide.
        /// </summary>
        /// <remarks>
        /// It is recommended to use <see cref="AddFont"/> when adding new fonts.
        /// </remarks>
        public FontStore Fonts { get; private set; }

        private FontStore localFonts;

        protected LocalisationManager Localisation { get; private set; }

        private readonly Container content;

        private readonly Container overlayContent;

        private DrawVisualiser drawVisualiser;

        private TextureVisualiser textureVisualiser;

        private LogOverlay logOverlay;

        private AudioMixerVisualiser audioMixerVisualiser;

        protected override Container<Drawable> Content => content;

        /// <summary>
        /// Creates a new <see cref="LocalisationManager"/>.
        /// </summary>
        /// <param name="frameworkConfig">The framework config manager.</param>
        protected virtual LocalisationManager CreateLocalisationManager(FrameworkConfigManager frameworkConfig) => new LocalisationManager(frameworkConfig);

        protected internal virtual UserInputManager CreateUserInputManager() => new UserInputManager();

        /// <summary>
        /// Provide <see cref="FrameworkSetting"/> defaults which should override those provided by osu-framework.
        /// <remarks>
        /// Please check https://github.com/ppy/osu-framework/blob/master/osu.Framework/Configuration/FrameworkConfigManager.cs for expected types.
        /// </remarks>
        /// </summary>
        protected internal virtual IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults() => null;

        /// <summary>
        /// Creates the <see cref="Storage"/> where this <see cref="Game"/> will reside.
        /// </summary>
        /// <param name="host">The <see cref="GameHost"/>.</param>
        /// <param name="defaultStorage">The default <see cref="Storage"/> to be used if a custom <see cref="Storage"/> isn't desired.</param>
        /// <returns>The <see cref="Storage"/>.</returns>
        protected internal virtual Storage CreateStorage(GameHost host, Storage defaultStorage) => defaultStorage;

        protected Game()
        {
            RelativeSizeAxes = Axes.Both;

            base.AddInternal(content = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
            });

            base.AddInternal(new SafeAreaContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = overlayContent = new DrawSizePreservingFillContainer
                {
                    TargetDrawSize = new Vector2(1280, 960),
                    RelativeSizeAxes = Axes.Both,
                }
            });
        }

        protected sealed override void AddInternal(Drawable drawable) => throw new InvalidOperationException($"Use {nameof(Add)} or {nameof(Content)} instead.");

        /// <summary>
        /// The earliest point of entry during <see cref="GameHost.Run"/> starting execution of a game.
        /// This should be used to set up any low level tasks such as exception handling.
        /// </summary>
        /// <remarks>
        /// At this point in execution, only <see cref="GameHost.Storage"/> and <see cref="GameHost.CacheStorage"/> are guaranteed to be valid for use.
        /// They are provided as <paramref name="gameStorage"/> and <paramref name="cacheStorage"/> respectively for convenience.
        /// </remarks>
        /// <param name="gameStorage">The default game storage.</param>
        /// <param name="cacheStorage">The default cache storage.</param>
        public virtual void SetupLogging(Storage gameStorage, Storage cacheStorage)
        {
        }

        /// <summary>
        /// As Load is run post host creation, you can override this method to alter properties of the host before it makes itself visible to the user.
        /// </summary>
        /// <param name="host"></param>
        public virtual void SetHost(GameHost host)
        {
            Host = host;
            host.ExitRequested += RequestExit;
            host.Activated += onHostActivated;
            host.Deactivated += onHostDeactivated;
        }

        private void onHostActivated()
        {
            isActive.Value = true;
        }

        private void onHostDeactivated()
        {
            isActive.Value = false;
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            Resources = new ResourceStore<byte[]>();
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources"));

            Textures = new TextureStore(Host.Renderer, Host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures")),
                filteringMode: DefaultTextureFilteringMode);

            Textures.AddTextureSource(Host.CreateTextureLoaderStore(CreateOnlineStore()));
            dependencies.Cache(Textures);

            var tracks = new ResourceStore<byte[]>();
            tracks.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"));
            tracks.AddStore(CreateOnlineStore());

            var samples = new ResourceStore<byte[]>();
            samples.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Samples"));
            samples.AddStore(CreateOnlineStore());

            Audio = new AudioManager(Host.AudioThread, tracks, samples, config) { EventScheduler = Scheduler };
            dependencies.Cache(Audio);

            dependencies.CacheAs(Audio.Tracks);
            dependencies.CacheAs(Audio.Samples);

            Shaders = new ShaderManager(Host.Renderer, new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));
            dependencies.Cache(Shaders);

            var cacheStorage = Host.CacheStorage.GetStorageForDirectory("fonts");

            // base store is for user fonts
            Fonts = new FontStore(Host.Renderer, useAtlas: true, cacheStorage: cacheStorage);

            // nested store for framework provided fonts.
            // note that currently this means there could be two async font load operations.
            Fonts.AddStore(localFonts = new FontStore(Host.Renderer, useAtlas: false));

            // Roboto (FrameworkFont.Regular)
            var roboto = AddVariableFont(Resources, @"Fonts/Roboto/Roboto", localFonts);
            roboto.AddInstance(@"Roboto-Regular");
            roboto.AddInstance(@"Roboto-Bold");
            var robotoItalic = AddVariableFont(Resources, @"Fonts/Roboto/RobotoItalic", localFonts);
            robotoItalic.AddInstance(@"Roboto-Italic", @"Roboto-RegularItalic");
            robotoItalic.AddInstance(@"Roboto-BoldItalic");

            // RobotoCondensed (FrameworkFont.Condensed)
            roboto.AddInstance(@"Roboto-CondensedRegular", @"RobotoCondensed-Regular");
            roboto.AddInstance(@"Roboto-CondensedBold", @"RobotoCondensed-Bold");

            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Solid");
            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Regular");
            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Brands");

            dependencies.Cache(Fonts);

            Localisation = CreateLocalisationManager(config);
            dependencies.CacheAs(Localisation);

            frameSyncMode = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);

            executionMode = config.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);

            logOverlayVisibility = config.GetBindable<bool>(FrameworkSetting.ShowLogOverlay);
            logOverlayVisibility.BindValueChanged(visibility =>
            {
                if (visibility.NewValue)
                {
                    if (logOverlay == null)
                    {
                        LoadComponentAsync(logOverlay = new LogOverlay
                        {
                            Depth = float.MinValue / 2,
                        }, overlayContent.Add);
                    }

                    logOverlay.Show();
                }
                else
                {
                    logOverlay?.Hide();
                }
            }, true);
        }

        /// <summary>
        /// Creates an <see cref="OnlineStore"/> to be used for online textures/tracks/samples lookups.
        /// </summary>
        protected virtual OnlineStore CreateOnlineStore() => new OnlineStore();

        /// <summary>
        /// Add a bitmap font to be globally accessible to the game.
        /// </summary>
        /// <param name="store">The backing store with font resources.</param>
        /// <param name="assetName">The base name of the font.</param>
        /// <param name="target">An optional target store to add the font to. If not specified, <see cref="Fonts"/> is used.</param>
        public void AddFont(ResourceStore<byte[]> store, string assetName = null, FontStore target = null)
            => addFont(target ?? Fonts, store, assetName);

        private void addFont(FontStore target, ResourceStore<byte[]> store, string assetName = null)
            => target.AddTextureSource(new RawCachingGlyphStore(store, assetName, Host.CreateTextureLoaderStore(store)));

        /// <summary>
        /// Add an outline font to be globally accessible to the game.
        /// </summary>
        /// <param name="store">The backing store with font resources.</param>
        /// <param name="assetName">The base name of the font.</param>
        /// <param name="target">An optional target store to add the font to. If not specified, <see cref="Fonts"/> is used.</param>
        /// <returns>The newly added font family from which fonts can be instantiated.</returns>
        public void AddOutlineFont(ResourceStore<byte[]> store, string assetName, FontStore target = null)
            => (target ?? Fonts).AddTextureSource(new OutlineGlyphStore(store, assetName));

        /// <summary>
        /// Add a variable font to be globally accessible to the game.
        /// </summary>
        /// <remarks>
        /// This does not instantiate any glyph stores. Use
        /// <see cref="OutlineFontStore.AddInstance(string, string?)"/>
        /// on the returned font store to make the font usable.
        /// </remarks>
        /// <param name="store">The backing store with font resources.</param>
        /// <param name="assetName">The base name of the font.</param>
        /// <param name="target">An optional target store to add the font to. If not specified, <see cref="Fonts"/> is used.</param>
        /// <returns>The newly added font family from which fonts can be instantiated.</returns>
        public OutlineFontStore AddVariableFont(ResourceStore<byte[]> store, string assetName, FontStore target = null)
        {
            var nestedStore = new OutlineFontStore(Host.Renderer, store, assetName);
            (target ?? Fonts).AddStore(nestedStore);
            return nestedStore;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            PerformanceOverlay performanceOverlay;

            LoadComponentAsync(performanceOverlay = new PerformanceOverlay
            {
                Margin = new MarginPadding(5),
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10, 10),
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Depth = float.MinValue
            }, overlayContent.Add);

            FrameStatistics.BindValueChanged(e => performanceOverlay.State = e.NewValue, true);

            if (FrameworkEnvironment.FrameStatisticsViaTouch)
            {
                base.AddInternal(new FrameStatisticsTouchReceptor(this)
                {
                    Depth = float.MaxValue,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.2f),
                });
            }
        }

        protected readonly Bindable<FrameStatisticsMode> FrameStatistics = new Bindable<FrameStatisticsMode>();

        private GlobalStatisticsDisplay globalStatistics;

        private Bindable<bool> logOverlayVisibility;

        private Bindable<FrameSync> frameSyncMode;

        private Bindable<ExecutionMode> executionMode;

        private float currentOverlayDepth;

        public bool OnPressed(KeyBindingPressEvent<FrameworkAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case FrameworkAction.CycleFrameStatistics:
                    CycleFrameStatistics();
                    return true;

                case FrameworkAction.ToggleDrawVisualiser:

                    if (drawVisualiser == null)
                    {
                        LoadComponentAsync(drawVisualiser = new DrawVisualiser
                        {
                            State = { Value = Visibility.Visible },
                            Depth = getNextFrontMostOverlayDepth(),
                            ToolPosition = getCascadeLocation(0),
                        }, overlayContent.Add);
                    }
                    else
                        toggleOverlay(drawVisualiser);

                    return true;

                case FrameworkAction.ToggleGlobalStatistics:

                    if (globalStatistics == null)
                    {
                        LoadComponentAsync(globalStatistics = new GlobalStatisticsDisplay
                        {
                            State = { Value = Visibility.Visible },
                            Position = getCascadeLocation(1),
                            Depth = getNextFrontMostOverlayDepth(),
                        }, overlayContent.Add);
                    }
                    else
                        toggleOverlay(globalStatistics);

                    return true;

                case FrameworkAction.ToggleAtlasVisualiser:

                    if (textureVisualiser == null)
                    {
                        LoadComponentAsync(textureVisualiser = new TextureVisualiser
                        {
                            State = { Value = Visibility.Visible },
                            Position = getCascadeLocation(2),
                            Depth = getNextFrontMostOverlayDepth(),
                        }, overlayContent.Add);
                    }
                    else
                        toggleOverlay(textureVisualiser);

                    return true;

                case FrameworkAction.ToggleAudioMixerVisualiser:
                    if (audioMixerVisualiser == null)
                    {
                        LoadComponentAsync(audioMixerVisualiser = new AudioMixerVisualiser
                        {
                            State = { Value = Visibility.Visible },
                            Position = getCascadeLocation(3),
                            Depth = getNextFrontMostOverlayDepth(),
                        }, overlayContent.Add);
                    }
                    else
                        toggleOverlay(audioMixerVisualiser);

                    return true;

                case FrameworkAction.ToggleLogOverlay:
                    logOverlayVisibility.Value = !logOverlayVisibility.Value;
                    return true;

                case FrameworkAction.ToggleFullscreen:
                    Window?.CycleMode();
                    return true;

                case FrameworkAction.CycleFrameSync:
                    var nextFrameSync = frameSyncMode.Value + 1;

                    if (nextFrameSync > FrameSync.Unlimited)
                        nextFrameSync = FrameSync.VSync;

                    frameSyncMode.Value = nextFrameSync;
                    break;

                case FrameworkAction.CycleExecutionMode:
                    var nextExecutionMode = executionMode.Value + 1;

                    if (nextExecutionMode > ExecutionMode.MultiThreaded)
                        nextExecutionMode = ExecutionMode.SingleThread;

                    executionMode.Value = nextExecutionMode;
                    break;
            }

            return false;

            static Vector2 getCascadeLocation(int index)
                => new Vector2(100 + index * (TitleBar.HEIGHT + 10));
        }

        protected void CycleFrameStatistics()
        {
            switch (FrameStatistics.Value)
            {
                case FrameStatisticsMode.None:
                    FrameStatistics.Value = FrameStatisticsMode.Minimal;
                    break;

                case FrameStatisticsMode.Minimal:
                    FrameStatistics.Value = FrameStatisticsMode.Full;
                    break;

                case FrameStatisticsMode.Full:
                    FrameStatistics.Value = FrameStatisticsMode.None;
                    break;
            }
        }

        private void toggleOverlay(OverlayContainer overlay)
        {
            overlay.ToggleVisibility();

            if (overlay.State.Value == Visibility.Visible)
                overlayContent.ChangeChildDepth(overlay, getNextFrontMostOverlayDepth());
        }

        private float getNextFrontMostOverlayDepth() => currentOverlayDepth -= 0.01f;

        public void OnReleased(KeyBindingReleaseEvent<FrameworkAction> e)
        {
        }

        public virtual bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case PlatformAction.Exit:
                    RequestExit();
                    return true;
            }

            return false;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        /// <summary>
        /// Requests the game to exit. This exit can be blocked by <see cref="OnExiting"/>.
        /// </summary>
        public void RequestExit()
        {
            if (!OnExiting())
                Exit();
        }

        /// <summary>
        /// Force-closes the game, ignoring <see cref="OnExiting"/> return value.
        /// </summary>
        public void Exit()
        {
            if (Host == null)
                throw new InvalidOperationException("Attempted to exit a game which has not yet been run");

            Host.Exit();
        }

        /// <summary>
        /// Fired when an exit has been requested.
        /// </summary>
        /// <remarks>Usually fired because <see cref="PlatformAction.Exit"/> or the window close (X) button was pressed.</remarks>
        /// <returns>Return <c>true</c> to block the exit process.</returns>
        protected virtual bool OnExiting() => false;

        protected override void Dispose(bool isDisposing)
        {
            // ensure any async disposals are completed before we begin to rip components out.
            // if we were to not wait, async disposals may throw unexpected exceptions.
            AsyncDisposalQueue.WaitForEmpty();

            base.Dispose(isDisposing);

            // call a second time to protect against anything being potentially async disposed in the base.Dispose call.
            AsyncDisposalQueue.WaitForEmpty();

            Audio?.Dispose();
            Audio = null;

            Fonts?.Dispose();
            Fonts = null;

            localFonts?.Dispose();
            localFonts = null;

            Localisation?.Dispose();
            Localisation = null;

            if (Host != null)
            {
                Host.ExitRequested -= RequestExit;
                Host.Activated -= onHostActivated;
                Host.Deactivated -= onHostDeactivated;
            }
        }

        private partial class FrameStatisticsTouchReceptor : Drawable
        {
            private readonly Game game;

            public FrameStatisticsTouchReceptor(Game game)
            {
                this.game = game;
            }

            protected override bool OnClick(ClickEvent e) => e.CurrentState.Mouse.LastSource is ISourcedFromTouch;

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                game.CycleFrameStatistics();
                return base.OnDoubleClick(e);
            }
        }
    }
}
