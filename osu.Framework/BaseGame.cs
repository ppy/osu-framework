// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
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
using OpenTK;
using OpenTK.Input;
using FlowDirection = osu.Framework.Graphics.Containers.FlowDirection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework
{
    public class BaseGame : Container
    {
        public BasicGameWindow Window => host?.Window;

        public ResourceStore<byte[]> Resources;

        public TextureStore Textures;

        public override bool Contains(Vector2 screenSpacePos) => true;

        /// <summary>
        /// This should point to the main resource dll file. If not specified, it will use resources embedded in your executable.
        /// </summary>
        protected virtual string MainResourceFile => Host.FullPath;

        private BasicGameHost host;

        public BasicGameHost Host => host;

        private bool isActive;

        public AudioManager Audio;

        public ShaderManager Shaders;

        public FontStore Fonts;

        private Container content;
        private PerformanceOverlay performanceContainer;
        internal DrawVisualiser DrawVisualiser;

        private LogOverlay logOverlay;

        protected FrameworkConfigManager Config;

        protected override Container<Drawable> Content => content;

        public DependencyContainer Dependencies => Host.Dependencies;

        public BaseGame()
        {
            RelativeSizeAxes = Axes.Both;

            AddInternal(new Drawable[]
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
            (DrawVisualiser = new DrawVisualiser()
            {
                Depth = float.MinValue / 2,
            }).Preload(this, AddInternal);

            (logOverlay = new LogOverlay()
            {
                Depth = float.MinValue / 2,
            }).Preload(this, AddInternal);
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (!base.Invalidate(invalidation, source, shallPropagate)) return false;

            return true;
        }

        /// <summary>
        /// As Load is run post host creation, you can override this method to alter properties of the host before it makes itself visible to the user.
        /// </summary>
        /// <param name="host"></param>
        public virtual void SetHost(BasicGameHost host)
        {
            if (Config == null)
                Config = new FrameworkConfigManager(host.Storage);

            this.host = host;
            updateWindowModeProperties(Config.Get<WindowMode>(FrameworkConfig.WindowMode));

            host.Exiting += OnExiting;
        }

        private void updateWindowModeProperties(WindowMode windowMode)
        {
            host.CurrentWindowMode = windowMode;

            switch (host.CurrentWindowMode)
            {
                case WindowMode.Windowed:
                case WindowMode.Borderless:
                    host.Size = new Vector2(Config.Get<int>(FrameworkConfig.Width), Config.Get<int>(FrameworkConfig.Height));
                    host.ViewPosition = new Vector2((float)Config.Get<double>(FrameworkConfig.WindowedPositionX), (float)Config.Get<double>(FrameworkConfig.WindowedPositionY));
                    break;
                case WindowMode.Fullscreen:
                    host.Size = new Vector2(Config.Get<int>(FrameworkConfig.WidthFullscreen), Config.Get<int>(FrameworkConfig.HeightFullscreen));
                    break;
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.Cache(Config);

            Resources = new ResourceStore<byte[]>();
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));
            Resources.AddStore(new DllResourceStore(MainResourceFile));

            Textures = new TextureStore(new RawTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures")));
            Textures.AddStore(new RawTextureLoaderStore(new OnlineStore()));
            Dependencies.Cache(Textures);

            Audio = Dependencies.Cache(new AudioManager(
                new NamespacedResourceStore<byte[]>(Resources, @"Tracks"),
                new NamespacedResourceStore<byte[]>(Resources, @"Samples")));

            //attach our bindables to the audio subsystem.
            Audio.AudioDevice.Weld(Config.GetBindable<string>(FrameworkConfig.AudioDevice));
            Audio.Volume.Weld(Config.GetBindable<double>(FrameworkConfig.VolumeUniversal));
            Audio.VolumeSample.Weld(Config.GetBindable<double>(FrameworkConfig.VolumeEffect));
            Audio.VolumeTrack.Weld(Config.GetBindable<double>(FrameworkConfig.VolumeMusic));

            Config.GetBindable<WindowMode>(FrameworkConfig.WindowMode).ValueChanged += delegate(object sender, EventArgs e)
                {
                    WindowMode windowMode = ((Bindable<WindowMode>)sender).Value;
                    if (windowMode == WindowMode.Fullscreen)
                    {
                        setWindowModeProperties(WindowMode.Windowed);
                    }

                    updateWindowModeProperties(windowMode);
                };

            Shaders = new ShaderManager(new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));
            Dependencies.Cache(Shaders);

            Fonts = new FontStore(new GlyphStore(Resources, @"Fonts/OpenSans"))
            {
                ScaleAdjust = 100
            };
            Dependencies.Cache(Fonts);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            (performanceContainer = new PerformanceOverlay
            {
                Margin = new MarginPadding(5),
                Direction = FlowDirection.VerticalOnly,
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Spacing = new Vector2(10, 10),
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Depth = float.MinValue
            }).Preload(this, AddInternal);

            performanceContainer.AddThread(host.InputThread);
            performanceContainer.AddThread(Audio.Thread);
            performanceContainer.AddThread(host.UpdateThread);
            performanceContainer.AddThread(host.DrawThread);

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

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (state.Keyboard.ControlPressed)
            {
                switch (args.Key)
                {
                    case Key.F11:
                        switch (performanceContainer.State)
                        {
                            case FrameStatisticsMode.None:
                                performanceContainer.State = FrameStatisticsMode.Minimal;
                                break;
                            case FrameStatisticsMode.Minimal:
                                performanceContainer.State = FrameStatisticsMode.Full;
                                break;
                            case FrameStatisticsMode.Full:
                                performanceContainer.State = FrameStatisticsMode.None;
                                break;
                        }
                        return true;
                    case Key.F1:
                        DrawVisualiser.ToggleVisibility();
                        return true;
                    case Key.F10:
                        logOverlay.ToggleVisibility();
                        return true;
                }
            }

            if (state.Keyboard.AltPressed && args.Key == Key.Enter)
            {
                Config.Set(FrameworkConfig.WindowMode,
                                        Window.CurrentWindowMode == WindowMode.Fullscreen || Window.CurrentWindowMode == WindowMode.Borderless ?
                                        WindowMode.Windowed :
                                        WindowMode.Fullscreen);
                return true;
            }

            return base.OnKeyDown(state, args);

        }

        public void Exit()
        {
            host.Exit();
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

        private void setWindowModeProperties(WindowMode windowMode)
        {
            if (Parent != null)
            {
                switch (host.CurrentWindowMode)
                {
                    case WindowMode.Windowed:
                    case WindowMode.Borderless:
                        Config.Set(FrameworkConfig.Width, (int)DrawSize.X);
                        Config.Set(FrameworkConfig.Height, (int)DrawSize.Y);

                        Vector2 viewPosition = host.ViewPosition;

                        Config.Set(FrameworkConfig.WindowedPositionX, (double)viewPosition.X);
                        Config.Set(FrameworkConfig.WindowedPositionY, (double)viewPosition.Y);
                        break;

                    case WindowMode.Fullscreen:
                        Config.Set(FrameworkConfig.WidthFullscreen, (int)DrawSize.X);
                        Config.Set(FrameworkConfig.HeightFullscreen, (int)DrawSize.Y);
                        break;
                }
            }

        }

        protected override void Dispose(bool isDisposing)
        {
            setWindowModeProperties(host.CurrentWindowMode);

            base.Dispose(isDisposing);

            Audio?.Dispose();
            Audio = null;
        }
    }
}
