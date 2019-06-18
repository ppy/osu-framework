// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content.PM;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        public override IGraphicsContext Context
            => View.GraphicsContext;

        internal static AndroidGameView View;

        public override bool Focused
            => true;

        public override osuTK.WindowState WindowState {
            get => osuTK.WindowState.Normal;
            set { }
        }

        public AndroidGameWindow() : base(View)
        {
            orientationMode.ValueChanged += (value) =>
            {
                var activity = (AndroidGameActivity)View.Context;

                switch (value.NewValue)
                {
                    case WindowOrientationMode.Landscape:
                        activity.RequestedOrientation = ScreenOrientation.Landscape;
                        break;
                    case WindowOrientationMode.Portrait:
                        activity.RequestedOrientation = ScreenOrientation.Portrait;
                        break;
                    case WindowOrientationMode.UpsideDownLandscape:
                        activity.RequestedOrientation = ScreenOrientation.ReverseLandscape;
                        break;
                    case WindowOrientationMode.UpsideDownPortrait:
                        activity.RequestedOrientation = ScreenOrientation.ReversePortrait;
                        break;
                    case WindowOrientationMode.SensorLandscape:
                        activity.RequestedOrientation = ScreenOrientation.SensorLandscape;
                        break;
                    case WindowOrientationMode.SensorPortait:
                        activity.RequestedOrientation = ScreenOrientation.SensorPortrait;
                        break;
                    case WindowOrientationMode.Sensor:
                        activity.RequestedOrientation = ScreenOrientation.Sensor;
                        break;
                }
            };
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            // Let's just say the cursor is always in the window.
            CursorInWindow = true;

            config.BindWith(FrameworkSetting.WindowOrientationMode, orientationMode);
        }

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new WindowMode[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public override IEnumerable<WindowOrientationMode> SupportedOrientationModes => new[]
        {
            WindowOrientationMode.Landscape,
            WindowOrientationMode.Portrait,
            WindowOrientationMode.SensorLandscape,
            WindowOrientationMode.SensorPortait,
            WindowOrientationMode.Sensor
        };

        private readonly Bindable<WindowOrientationMode> orientationMode = new Bindable<WindowOrientationMode>();

        public override void Run()
        {
            View.Run();
        }

        public override void Run(double updateRate)
        {
            View.Run(updateRate);
        }
    }
}
