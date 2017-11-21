// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class FrameworkConfigManager : ConfigManager<FrameworkSetting>
    {
        protected override string Filename => @"framework.ini";

        protected override void InitialiseDefaults()
        {
            Set(FrameworkSetting.ShowLogOverlay, false);

            Set(FrameworkSetting.Width, 1366, 640);
            Set(FrameworkSetting.Height, 768, 480);
            Set(FrameworkSetting.ConfineMouseMode, ConfineMouseMode.Fullscreen);
            Set(FrameworkSetting.WindowedPositionX, 0.5, -0.1, 1.1);
            Set(FrameworkSetting.WindowedPositionY, 0.5, -0.1, 1.1);
            Set(FrameworkSetting.AudioDevice, string.Empty);
            Set(FrameworkSetting.VolumeUniversal, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.VolumeMusic, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.VolumeEffect, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.WidthFullscreen, 9999, 320, 9999);
            Set(FrameworkSetting.HeightFullscreen, 9999, 240, 9999);
            Set(FrameworkSetting.Letterboxing, true);
            Set(FrameworkSetting.LetterboxPositionX, 0.0, -1.0, 1.0, 0.01);
            Set(FrameworkSetting.LetterboxPositionY, 0.0, -1.0, 1.0, 0.01);
            Set(FrameworkSetting.FrameSync, FrameSync.Limit2x);
            Set(FrameworkSetting.WindowMode, WindowMode.Windowed);
            Set(FrameworkSetting.ShowUnicode, false);
            Set(FrameworkSetting.ActiveInputHandlers, string.Empty);
            Set(FrameworkSetting.CursorSensitivity, 1.0, 0.1, 6, 0.01);
            Set(FrameworkSetting.Locale, string.Empty);
        }

        public FrameworkConfigManager(Storage storage)
            : base(storage)
        {
        }
    }

    public enum FrameworkSetting
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
        ActiveInputHandlers,
        CursorSensitivity
    }
}
