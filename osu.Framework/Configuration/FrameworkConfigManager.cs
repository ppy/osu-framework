// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using osu.Framework.Configuration.Tracking;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Configuration
{
    public class FrameworkConfigManager : IniConfigManager<FrameworkSetting>
    {
        protected override string Filename => @"framework.ini";

        protected override void InitialiseDefaults()
        {
            Set(FrameworkSetting.ShowLogOverlay, false);

            Set(FrameworkSetting.WindowedSize, new Size(1366, 768), new Size(640, 480));
            Set(FrameworkSetting.ConfineMouseMode, ConfineMouseMode.Fullscreen);
            Set(FrameworkSetting.MapAbsoluteInputToWindow, false);
            Set(FrameworkSetting.WindowedPositionX, 0.5, -0.5, 1.5);
            Set(FrameworkSetting.WindowedPositionY, 0.5, -0.5, 1.5);
            Set(FrameworkSetting.LastDisplayDevice, DisplayIndex.Default);
            Set(FrameworkSetting.AudioDevice, string.Empty);
            Set(FrameworkSetting.VolumeUniversal, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.VolumeMusic, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.VolumeEffect, 1.0, 0.0, 1.0, 0.01);
            Set(FrameworkSetting.SizeFullscreen, new Size(9999, 9999), new Size(320, 240));
            Set(FrameworkSetting.FrameSync, FrameSync.Limit2x);
            Set(FrameworkSetting.WindowMode, WindowMode.Windowed);
            Set(FrameworkSetting.ShowUnicode, false);
            Set(FrameworkSetting.IgnoredInputHandlers, string.Empty);
            Set(FrameworkSetting.CursorSensitivity, 1.0, 0.1, 6, 0.01);
            Set(FrameworkSetting.Locale, string.Empty);
            Set(FrameworkSetting.PerformanceLogging, false);
        }

        public FrameworkConfigManager(Storage storage)
            : base(storage)
        {
        }

        public override TrackedSettings CreateTrackedSettings() => new TrackedSettings
        {
            new TrackedSetting<FrameSync>(FrameworkSetting.FrameSync, v => new SettingDescription(v, "Frame Limiter", v.GetDescription(), "Ctrl+F7")),
            new TrackedSetting<string>(FrameworkSetting.AudioDevice, v => new SettingDescription(v, "Audio Device", string.IsNullOrEmpty(v) ? "Default" : v, v)),
            new TrackedSetting<bool>(FrameworkSetting.ShowLogOverlay, v => new SettingDescription(v, "Debug Logs", v ? "visible" : "hidden", "Ctrl+F10")),
            new TrackedSetting<Size>(FrameworkSetting.WindowedSize, v => new SettingDescription(v, "Screen resolution", $"{v.Width}x{v.Height}")),
            new TrackedSetting<double>(FrameworkSetting.CursorSensitivity, v => new SettingDescription(v, "Cursor Sensitivity", v.ToString(@"0.##x"), "Ctrl+Alt+R to reset")),
            new TrackedSetting<string>(FrameworkSetting.IgnoredInputHandlers, v =>
            {
                bool raw = !v.Contains("Raw");
                return new SettingDescription(raw, "Raw Input", raw ? "enabled" : "disabled", "Ctrl+Alt+R to reset");
            }),
            new TrackedSetting<WindowMode>(FrameworkSetting.WindowMode, v => new SettingDescription(v, "Screen Mode", v.ToString(), "Alt+Enter"))
        };
    }

    public enum FrameworkSetting
    {
        ShowLogOverlay,

        AudioDevice,
        VolumeUniversal,
        VolumeEffect,
        VolumeMusic,

        WindowedSize,
        WindowedPositionX,
        WindowedPositionY,
        LastDisplayDevice,

        SizeFullscreen,

        WindowMode,
        ConfineMouseMode,
        FrameSync,

        ShowUnicode,
        Locale,
        IgnoredInputHandlers,
        CursorSensitivity,
        MapAbsoluteInputToWindow,

        PerformanceLogging
    }
}
