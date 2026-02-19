// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Framework.Localisation.Strings;

namespace osu.Framework.Configuration
{
    public enum WindowMode
    {
        [LocalisableDescription(typeof(WindowModeStrings), nameof(WindowModeStrings.Windowed))]
        Windowed = 0,

        [LocalisableDescription(typeof(WindowModeStrings), nameof(WindowModeStrings.Borderless))]
        Borderless = 1,

        [LocalisableDescription(typeof(WindowModeStrings), nameof(WindowModeStrings.Fullscreen))]
        Fullscreen = 2
    }
}
