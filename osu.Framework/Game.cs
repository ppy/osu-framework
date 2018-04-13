// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Input;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Input.Bindings;
using OpenTK;
using GameWindow = osu.Framework.Platform.GameWindow;

namespace osu.Framework
{
    public abstract class Game : Container, IKeyBindingHandler<FrameworkAction>
    {
        public GameWindow Window => Host?.Window;

        public ResourceStore<byte[]> Resources;

        public TextureStore Textures;

        /// <summary>
        /// This should point to the main resource dll file. If not specified, it will use resources embedded in your executable.
        /// </summary>
        protected virtual string MainResourceFile => Host.FullPath;

        protected GameHost Host { get; private set; }

        private bool isActive;

        public AudioManager Audio;

        public ShaderManager Shaders;

        public FontStore Fonts;

        private readonly Container content;
        private PerformanceOverlay performanceContainer;
        internal DrawVisualiser DrawVisualiser;

        private LogOverlay logOverlay;

        protected override Container<Drawable> Content => content;

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

        private void addDebugTools()
        {
            LoadComponentAsync(DrawVisualiser = new DrawVisualiser
            {
                Depth = float.MinValue / 2,
            }, AddInternal);

            LoadComponentAsync(logOverlay = new LogOverlay
            {
                Depth = float.MinValue / 2,
            }, AddInternal);
        }

        /// <summary>
        /// As Load is run post host creation, you can override this method to alter properties of the host before it makes itself visible to the user.
        /// </summary>
        /// <param name="host"></param>
        public virtual void SetHost(GameHost host)
        {
            Host = host;
            host.Exiting += OnExiting;
            host.Activated += () => IsActive = true;
            host.Deactivated += () => IsActive = false;
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateLocalDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateLocalDependencies(parent));

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            Resources = new ResourceStore<byte[]>();
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));
            Resources.AddStore(new DllResourceStore(MainResourceFile));

            Textures = new TextureStore(new RawTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures")));
            Textures.AddStore(new RawTextureLoaderStore(new OnlineStore()));
            dependencies.Cache(Textures);

            var tracks = new ResourceStore<byte[]>(Resources);
            tracks.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"));
            tracks.AddStore(new OnlineStore());

            var samples = new ResourceStore<byte[]>(Resources);
            samples.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Samples"));
            samples.AddStore(new OnlineStore());

            Audio = new AudioManager(tracks, samples) { EventScheduler = Scheduler };
            dependencies.Cache(Audio);

            Host.RegisterThread(Audio.Thread);

            //attach our bindables to the audio subsystem.
            config.BindWith(FrameworkSetting.AudioDevice, Audio.AudioDevice);
            config.BindWith(FrameworkSetting.VolumeUniversal, Audio.Volume);
            config.BindWith(FrameworkSetting.VolumeEffect, Audio.VolumeSample);
            config.BindWith(FrameworkSetting.VolumeMusic, Audio.VolumeTrack);

            Shaders = new ShaderManager(new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));
            dependencies.Cache(Shaders);

            Fonts = new FontStore(new GlyphStore(Resources, @"Fonts/OpenSans"))
            {
                ScaleAdjust = 100
            };
            dependencies.Cache(Fonts);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            LoadComponentAsync(performanceContainer = new PerformanceOverlay(Host.Threads.Reverse())
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

            addDebugTools();
        }

        /// <summary>
        /// Whether the Game environment is active (in the foreground).
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                if (value == isActive)
                    return;
                isActive = value;

                if (isActive)
                    OnActivated();
                else
                    OnDeactivated();
            }
        }

        protected FrameStatisticsMode FrameStatisticsMode
        {
            get { return performanceContainer.State; }
            set { performanceContainer.State = value; }
        }

        public bool OnPressed(FrameworkAction action)
        {
            switch (action)
            {
                case FrameworkAction.CycleFrameStatistics:
                    switch (FrameStatisticsMode)
                    {
                        case FrameStatisticsMode.None:
                            FrameStatisticsMode = FrameStatisticsMode.Minimal;
                            break;
                        case FrameStatisticsMode.Minimal:
                            FrameStatisticsMode = FrameStatisticsMode.Full;
                            break;
                        case FrameStatisticsMode.Full:
                            FrameStatisticsMode = FrameStatisticsMode.None;
                            break;
                    }
                    return true;
                case FrameworkAction.ToggleDrawVisualiser:
                    DrawVisualiser.ToggleVisibility();
                    return true;
                case FrameworkAction.ToggleLogOverlay:
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
            Host.Exit();
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnDeactivated()
        {
        }

        protected virtual bool OnExiting()
        {
            return false;
        }

        /// <summary>
        /// Called before a frame cycle has started (Update and Draw).
        /// </summary>
        protected virtual void PreFrame()
        {
        }

        /// <summary>
        /// Called after a frame cycle has been completed (Update and Draw).
        /// </summary>
        protected virtual void PostFrame()
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Audio?.Dispose();
            Audio = null;
        }
    }
}
