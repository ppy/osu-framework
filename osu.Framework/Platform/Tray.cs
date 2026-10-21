// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Platform
{
    public class TrayIcon
    {
        public required string Label { get; set; }

        public Image<Rgba32>? Icon { get; set; }

        public TrayMenuEntry[]? Menu { get; set; }
    }

    public abstract class TrayMenuEntry
    {
        public bool Enabled { get; set; } = true;
    }

    public class TrayButton : TrayMenuEntry
    {
        public required string Label { get; set; }

        public Action? Action { get; set; }
    }

    public class TrayCheckBox : TrayMenuEntry
    {
        public required string Label { get; set; }

        public bool Checked { get; set; }

        public Action? Action { get; set; }
    }

    public class TraySubMenu : TrayMenuEntry
    {
        public required string Label { get; set; }

        public TrayMenuEntry[]? Menu { get; set; }
    }

    public class TraySeparator : TrayMenuEntry
    {
    }
}
