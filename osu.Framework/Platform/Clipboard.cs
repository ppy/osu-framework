// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Platform
{
    /// <summary>
    /// This class allows placing and retrieving data from the clipboard
    /// </summary>
    public abstract class Clipboard
    {
        /// <summary>
        /// Retrieve text from the clipboard
        /// </summary>
        public abstract string GetText();

        /// <summary>
        /// Copy text to the clipboard
        /// </summary>
        /// <param name="selectedText">Text to copy to the clipboard</param>
        public abstract void SetText(string selectedText);

        /// <summary>
        /// Retrieve image from the clipboard.
        /// </summary>
        /// <remarks>
        /// Currently only supported on Windows.
        /// </remarks>
        public abstract Image<TPixel> GetImage<TPixel>()
            where TPixel : unmanaged, IPixel<TPixel>;

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
