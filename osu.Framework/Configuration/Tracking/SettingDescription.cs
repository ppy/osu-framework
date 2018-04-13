// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        public readonly string Name;

        /// <summary>
        /// The readable setting value.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// The shortcut keys that cause this setting to change.
        /// </summary>
        public readonly string Shortcut;

        /// <summary>
        /// Constructs a new <see cref="SettingDescription"/>.
        /// </summary>
        /// <param name="rawValue">The raw setting value.</param>
        /// <param name="name">The readable setting name.</param>
        /// <param name="value">The readable setting value.</param>
        /// <param name="shortcut">The shortcut keys that cause this setting to change.</param>
        public SettingDescription(object rawValue, string name, string value, string shortcut = @"")
        {
            RawValue = rawValue;
            Name = name;
            Value = value;
            Shortcut = shortcut;
        }
    }
}
