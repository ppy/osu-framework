// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class FrameworkConfigManager : ConfigManager<FrameworkConfig>
    {
        protected override string Filename => @"framework.ini";

        protected override void InitialiseDefaults()
        {
            Set(FrameworkConfig.ShowLogOverlay, true);

            Set(FrameworkConfig.Width, 1366, 640);
            Set(FrameworkConfig.Height, 768, 480);
            Set(FrameworkConfig.ConfineMouseMode, ConfineMouseMode.Fullscreen);
            Set(FrameworkConfig.WindowedPositionX, 0.5, -0.1, 1.1);
            Set(FrameworkConfig.WindowedPositionY, 0.5, -0.1, 1.1);
            Set(FrameworkConfig.AudioDevice, string.Empty);
            Set(FrameworkConfig.VolumeUniversal, 1.0, 0.0, 1.0);
            Set(FrameworkConfig.VolumeMusic, 1.0, 0.0, 1.0);
            Set(FrameworkConfig.VolumeEffect, 1.0, 0.0, 1.0);
            Set(FrameworkConfig.WidthFullscreen, 9999, 320, 9999);
            Set(FrameworkConfig.HeightFullscreen, 9999, 240, 9999);
            Set(FrameworkConfig.Letterboxing, true);
            Set(FrameworkConfig.LetterboxPositionX, 0.0, -1.0, 1.0);
            Set(FrameworkConfig.LetterboxPositionY, 0.0, -1.0, 1.0);
            Set(FrameworkConfig.FrameSync, FrameSync.Limit2x);
            Set(FrameworkConfig.WindowMode, WindowMode.Windowed);
            Set(FrameworkConfig.ShowUnicode, false);
            Set(FrameworkConfig.Locale, "");
        }

        public FrameworkConfigManager(Storage storage)
            : base(storage)
        {
        }
    }

    public enum FrameworkConfig
    {
        ShowLogOverlay,

        AudioDevice,
        VolumeUniversal,
        VolumeEffect,
        VolumeMusic,

        Width,
        Height,
        WindowedPositionX,
        WindowedPositionY,

        HeightFullscreen,
        WidthFullscreen,

        WindowMode,
        ConfineMouseMode,
        Letterboxing,
        LetterboxPositionX,
        LetterboxPositionY,
        FrameSync,

        ShowUnicode,
        Locale,
    }
}
