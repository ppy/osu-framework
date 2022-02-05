// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SixLabors.ImageSharp;

namespace osu.Framework.Platform
{
    public abstract class Clipboard
    {
        public abstract string GetText();

        public abstract void SetText(string selectedText);

        /// <summary>
        /// Copy the image to the clipboard.
        /// </summary>
        /// <remarks>
        /// Currently only supported on Windows.
        /// </remarks>
        /// <param name="image">The image to copy to the clipboard</param>
        /// <returns>Whether the image was successfully copied or not</returns>
        public abstract bool SetImage(Image image);
    }
}
