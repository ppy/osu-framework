// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Localisation.Strings
{
    public static class WindowModeStrings
    {
        private const string prefix = "osu.Framework.Resources.Localisation.WindowMode";

        /// <summary>
        /// "Windowed"
        /// </summary>
        public static LocalisableString Windowed => new TranslatableString(getKey("windowed"), "Windowed");

        /// <summary>
        /// "Borderless"
        /// </summary>
        public static LocalisableString Borderless => new TranslatableString(getKey("borderless"), "Borderless");

        /// <summary>
        /// "Fullscreen"
        /// </summary>
        public static LocalisableString Fullscreen => new TranslatableString(getKey("fullscreen"), "Fullscreen");

        private static string getKey(string key) => $"{prefix}:{key}";
    }
}
