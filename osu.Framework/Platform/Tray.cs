// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    public class TrayIcon : IDisposable
    {
        string Label { get; set; }

        // Icon Icon { get; set; }

        TrayMenuEntry[] Menu { get; set; }
    }

    public abstract class TrayMenuEntry
    {
        bool Enabled { get; set; }
    }

    public class TrayButton : TrayMenuEntry
    {
        string Label { get; set; }

        Action Action { get; set; }
        
    }

    public class TrayCheckBox : TrayMenuEntry
    {
        string Label { get; set; }
        
        bool Checked { get; set; }
    }

    public class TraySubMenu : TrayMenuEntry
    {
        string Label { get; set; }

        TrayMenuEntry[] Menu { get; set; }
    }

    public class TraySeparator : TrayMenuEntry
    {
    }
}
