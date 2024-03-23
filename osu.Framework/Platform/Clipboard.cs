// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// Retrieve text from the clipboard.
        /// </summary>
        public abstract string? GetText();

        /// <summary>
        /// Copy text to the clipboard.
        /// </summary>
        /// <param name="text">Text to copy to the clipboard</param>
        public void SetText(string text)
        {
            SetData(
                new ClipboardTextEntry(text)
            );
        }

        /// <summary>
        /// Retrieve an image from the clipboard.
        /// </summary>
        public abstract Image<TPixel>? GetImage<TPixel>()
            where TPixel : unmanaged, IPixel<TPixel>;

        /// <summary>
        /// Copy the image to the clipboard.
        /// </summary>
        /// <param name="image">The image to copy to the clipboard</param>
        /// <returns>Whether the image was successfully copied or not</returns>
        public bool SetImage(Image image)
        {
            return SetData(
                new ClipboardImageEntry(image)
            );
        }

        /// <summary>
        /// Get clipboard content with custom format
        /// </summary>
        /// <returns>Name of the custom format</returns>
        public abstract string? GetCustom(string format);

        /// <summary>
        /// Get clipboard content with custom format
        /// </summary>
        /// <param name="format">Name of the custom format</param>
        /// <param name="text">Text to copy to the clipboard</param>
        public void SetCustom(string format, string text)
        {
            SetData(
                new ClipboardCustomEntry(format, text)
            );
        }

        /// <summary>
        /// Copy multiple values to the clipboard
        /// </summary>
        /// <param name="entries">Entries to copy the clipboard</param>
        public abstract bool SetData(params ClipboardEntry[] entries);
    }
}
