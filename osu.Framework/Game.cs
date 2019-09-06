// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osuTK;
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
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Platform;

namespace osu.Framework
{
    public abstract class Game : Container, IKeyBindingHandler<FrameworkAction>, IHandleGlobalKeyboardInput
    {
        public IWindow Window => Host?.Window;

        public ResourceStore<byte[]> Resources { get; private set; }

        public TextureStore Textures { get; private set; }

        protected GameHost Host { get; private set; }

        private readonly Bindable<bool> isActive = new Bindable<bool>(true);

        /// <summary>
        /// Whether the game is active (in the foreground).
        /// </summary>
        public IBindable<bool> IsActive => isActive;

        public AudioManager Audio { get; private set; }

        public ShaderManager Shaders { get; private set; }

        public FontStore Fonts { get; private set; }

        private FontStore localFonts;

        protected LocalisationManager Localisation { get; private set; }

        private readonly Container content;

        private DrawVisualiser drawVisualiser;

        private LogOverlay logOverlay;

        protected override Container<Drawable> Content => content;

        protected internal virtual UserInputManager CreateUserInputManager() => new UserInputManager();

        /// <summary>
        /// Provide <see cref="FrameworkSetting"/> defaults which should override those provided by osu-framework.
        /// <remarks>
        /// Please check https://github.com/ppy/osu-framework/blob/master/osu.Framework/Configuration/FrameworkConfigManager.cs for expected types.
        /// </remarks>
        /// </summary>
        protected internal virtual IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults() => null;

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
            host.Exiting += OnExiting;
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
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));

            Textures = new TextureStore(Host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures")));
            Textures.AddStore(Host.CreateTextureLoaderStore(new OnlineStore()));
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

            Shaders = new ShaderManager(new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));
            dependencies.Cache(Shaders);

            var cacheStorage = Host.Storage.GetStorageForDirectory(Path.Combine("cache", "fonts"));

            // base store is for user fonts
            Fonts = new FontStore(useAtlas: true, cacheStorage: cacheStorage);

            // nested store for framework provided fonts.
            // note that currently this means there could be two async font load operations.
            Fonts.AddStore(localFonts = new FontStore(useAtlas: false));

            localFonts.AddStore(new GlyphStore(Resources, @"Fonts/OpenSans/OpenSans"));
            localFonts.AddStore(new GlyphStore(Resources, @"Fonts/OpenSans/OpenSans-Bold"));
            localFonts.AddStore(new GlyphStore(Resources, @"Fonts/OpenSans/OpenSans-Italic"));
            localFonts.AddStore(new GlyphStore(Resources, @"Fonts/OpenSans/OpenSans-BoldItalic"));

            Fonts.AddStore(new GlyphStore(Resources, @"Fonts/FontAwesome5/FontAwesome-Solid"));
            Fonts.AddStore(new GlyphStore(Resources, @"Fonts/FontAwesome5/FontAwesome-Regular"));
            Fonts.AddStore(new GlyphStore(Resources, @"Fonts/FontAwesome5/FontAwesome-Brands"));

            dependencies.Cache(Fonts);

            Localisation = new LocalisationManager(config);
            dependencies.Cache(Localisation);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            PerformanceOverlay performanceOverlay;

            LoadComponentAsync(performanceOverlay = new PerformanceOverlay(Host.Threads.Reverse())
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

        public bool OnPressed(FrameworkAction action)
        {
            switch (action)
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

                case FrameworkAction.ToggleGlobalStatistics:

                    if (globalStatistics == null)
                    {
                        LoadComponentAsync(globalStatistics = new GlobalStatisticsDisplay
                        {
                            Depth = float.MinValue / 2,
                            Position = new Vector2(100 + ToolWindow.WIDTH, 100)
                        }, AddInternal);
                    }

                    globalStatistics.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleDrawVisualiser:

                    if (drawVisualiser == null)
                    {
                        LoadComponentAsync(drawVisualiser = new DrawVisualiser
                        {
                            ToolPosition = new Vector2(100),
                            Depth = float.MinValue / 2,
                        }, AddInternal);
                    }

                    drawVisualiser.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleLogOverlay:
                    if (logOverlay == null)
                    {
                        LoadComponentAsync(logOverlay = new LogOverlay
                        {
                            Depth = float.MinValue / 2,
                        }, AddInternal);
                    }

                    logOverlay.ToggleVisibility();
                    return true;

                case FrameworkAction.ToggleFullscreen:
                    Window?.CycleMode();
                    return true;
            }

            return false;
        }

        public bool OnReleased(FrameworkAction action) => false;

        public void Exit()
        {
            if (Host == null)
                throw new InvalidOperationException("Attempted to exit a game which has not yet been run");

            Host.Exit();
        }

        protected virtual bool OnExiting() => false;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Audio?.Dispose();
            Audio = null;
        }
    }
}
