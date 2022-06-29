// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Localisation;

namespace osu.Framework.Configuration.Tracking
{
    /// <summary>
    /// Contains information that can be displayed when tracked settings change.
    /// </summary>
    public class SettingDescription
    {
        /// <summary>
        /// The raw setting value.
        /// </summary>
        public readonly object RawValue;

        /// <summary>
        /// The readable setting name.
        /// </summary>
        public readonly LocalisableString Name;

        /// <summary>
        /// The readable setting value.
        /// </summary>
        public readonly LocalisableString Value;

        /// <summary>
        /// The shortcut keys that cause this setting to change.
        /// </summary>
        public readonly LocalisableString Shortcut;

        /// <summary>
        /// Constructs a new <see cref="SettingDescription"/>.
        /// </summary>
        /// <param name="rawValue">The raw setting value.</param>
        /// <param name="name">The readable setting name.</param>
        /// <param name="value">The readable setting value.</param>
        /// <param name="shortcut">The shortcut keys that cause this setting to change.</param>
        public SettingDescription(object rawValue, LocalisableString name, LocalisableString value, LocalisableString? shortcut = null)
        {
            RawValue = rawValue;
            Name = name;
            Value = value;
            Shortcut = shortcut ?? string.Empty;
        }
    }
}
