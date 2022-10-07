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
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework
{
    public abstract class Game : Container, IKeyBindingHandler<FrameworkAction>, IKeyBindingHandler<PlatformAction>, IHandleGlobalKeyboardInput
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

            AddRangeInternal(new Drawable[]
            {
                content = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
            });
        }

        /// <summary>
        /// As Load is run post host creation, you can override this method to alter properties of the host before it makes itself visible to the user.
        /// </summary>
        /// <param name="host"></param>
        public virtual void SetHost(GameHost host)
        {
            Host = host;
            host.ExitRequested += RequestExit;
            host.Activated += () => isActive.Value = true;
            host.Deactivated += () => isActive.Value = false;
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

            Textures.AddTextureSource(Host.CreateTextureLoaderStore(new OnlineStore()));
            dependencies.Cache(Textures);

            var tracks = new ResourceStore<byte[]>();
            tracks.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"));
            tracks.AddStore(new OnlineStore());

            var samples = new ResourceStore<byte[]>();
            samples.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Samples"));
            samples.AddStore(new OnlineStore());

            Audio = new AudioManager(Host.AudioThread, tracks, samples) { EventScheduler = Scheduler };
            dependencies.Cache(Audio);

            dependencies.CacheAs(Audio.Tracks);
            dependencies.CacheAs(Audio.Samples);

            // attach our bindables to the audio subsystem.
            config.BindWith(FrameworkSetting.AudioDevice, Audio.AudioDevice);
            config.BindWith(FrameworkSetting.VolumeUniversal, Audio.Volume);
            config.BindWith(FrameworkSetting.VolumeEffect, Audio.VolumeSample);
            config.BindWith(FrameworkSetting.VolumeMusic, Audio.VolumeTrack);

            Shaders = new ShaderManager(Host.Renderer, new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));
            dependencies.Cache(Shaders);

            var cacheStorage = Host.CacheStorage.GetStorageForDirectory("fonts");

            // base store is for user fonts
            Fonts = new FontStore(Host.Renderer, useAtlas: true, cacheStorage: cacheStorage);

            // nested store for framework provided fonts.
            // note that currently this means there could be two async font load operations.
            Fonts.AddStore(localFonts = new FontStore(Host.Renderer, useAtlas: false));

            // Roboto (FrameworkFont.Regular)
            addFont(localFonts, Resources, @"Fonts/Roboto/Roboto-Regular");
            addFont(localFonts, Resources, @"Fonts/Roboto/Roboto-RegularItalic");
            addFont(localFonts, Resources, @"Fonts/Roboto/Roboto-Bold");
            addFont(localFonts, Resources, @"Fonts/Roboto/Roboto-BoldItalic");

            // RobotoCondensed (FrameworkFont.Condensed)
            addFont(localFonts, Resources, @"Fonts/RobotoCondensed/RobotoCondensed-Regular");
            addFont(localFonts, Resources, @"Fonts/RobotoCondensed/RobotoCondensed-Bold");

            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Solid");
            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Regular");
            addFont(Fonts, Resources, @"Fonts/FontAwesome5/FontAwesome-Brands");

            dependencies.Cache(Fonts);

            Localisation = CreateLocalisationManager(config);
            dependencies.Cache(Localisation);

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
                        }, AddInternal);
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
        /// Add a font to be globally accessible to the game.
        /// </summary>
        /// <param name="store">The backing store with font resources.</param>
        /// <param name="assetName">The base name of the font.</param>
        /// <param name="target">An optional target store to add the font to. If not specified, <see cref="Fonts"/> is used.</param>
        public void AddFont(ResourceStore<byte[]> store, string assetName = null, FontStore target = null)
            => addFont(target ?? Fonts, store, assetName);

        private void addFont(FontStore target, ResourceStore<byte[]> store, string assetName = null)
            => target.AddTextureSource(new RawCachingGlyphStore(store, assetName, Host.CreateTextureLoaderStore(store)));

        protected override void LoadComplete()
        {
            base.LoadComplete();

            PerformanceOverlay performanceOverlay;

            LoadComponentAsync(performanceOverlay = new PerformanceOverlay(Host.Threads)
            {
                Margin = new MarginPadding(5),
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10, 10),
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Depth = float.MinValue
            }, AddInternal);

            FrameStatistics.BindValueChanged(e => performanceOverlay.State = e.NewValue, true);
        }

        protected readonly Bindable<FrameStatisticsMode> FrameStatistics = new Bindable<FrameStatisticsMode>();

        private GlobalStatisticsDisplay globalStatistics;

        private Bindable<bool> logOverlayVisibility;

        private Bindable<FrameSync> frameSyncMode;

        private Bindable<ExecutionMode> executionMode;

        public bool OnPressed(KeyBindingPressEvent<FrameworkAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case FrameworkAction.CycleFrameStatistics:

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

                    return true;

                case FrameworkAction.ToggleDrawVisualiser:

                    if (drawVisualiser == null)
                    {
                        LoadComponentAsync(drawVisualiser = new DrawVisualiser
                        {
                            ToolPosition = getCascadeLocation(0),
                            Depth = float.MinValue / 2,
                        }, AddInternal);
                    }

                    drawVisualiser.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleGlobalStatistics:

                    if (globalStatistics == null)
                    {
                        LoadComponentAsync(globalStatistics = new GlobalStatisticsDisplay
                        {
                            Depth = float.MinValue / 2,
                            Position = getCascadeLocation(1),
                        }, AddInternal);
                    }

                    globalStatistics.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleAtlasVisualiser:

                    if (textureVisualiser == null)
                    {
                        LoadComponentAsync(textureVisualiser = new TextureVisualiser
                        {
                            Position = getCascadeLocation(2),
                            Depth = float.MinValue / 2,
                        }, AddInternal);
                    }

                    textureVisualiser.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleAudioMixerVisualiser:
                    if (audioMixerVisualiser == null)
                    {
                        LoadComponentAsync(audioMixerVisualiser = new AudioMixerVisualiser
                        {
                            Position = getCascadeLocation(3),
                            Depth = float.MinValue / 2,
                        }, AddInternal);
                    }

                    audioMixerVisualiser.ToggleVisibility();
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
        }
    }
}
