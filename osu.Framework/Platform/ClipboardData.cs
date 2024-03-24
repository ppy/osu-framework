// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Holds multiple values that can be copied to the clipboard
    /// </summary>
    public struct ClipboardData
    {
        /// <summary>
        /// Text to be stored in the default plaintext format in the clipboard.
        /// </summary>
        public string? Text;

        /// <summary>
        /// Image to be stored in the default image format in the clipboard.
        /// </summary>
        public Image? Image;

        /// <summary>
        /// Values to be stored as custom formats on the clipboard.
        /// </summary>
        public readonly Dictionary<string, string> CustomFormatValues = new Dictionary<string, string>();

        public ClipboardData()
        {
            Text = null;
            Image = null;
        }

        /// <summary>
        /// Adds a clipboard entry with a custom format
        /// </summary>
        /// <param name="format">format of the clipboard entry</param>
        /// <param name="value">value of the clipboard entry</param>
        public void AddCustom(string format, string value)
        {
            CustomFormatValues[format] = value;
        }

        /// <summary>
        /// Returns true if no data is present
        /// </summary>
        public bool IsEmpty()
        {
            return Text == null && Image == null && CustomFormatValues.Count == 0;
        }
    }
}
