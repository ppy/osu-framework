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
                new ClipboardData
                {
                    Text = text
                }
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
                new ClipboardData
                {
                    Image = image
                }
            );
        }

        /// <summary>
        /// Retrieve content with custom mime type from the clipboard.
        /// </summary>
        /// <returns>Mime type of the clipboard item to retrieve</returns>
        public abstract string? GetCustom(string mimeType);

        /// <summary>
        /// Copy item with custom mime type to the clipboard
        /// </summary>
        /// <param name="mimeType">Mime type of the clipboard item</param>
        /// <param name="text">Text to copy to the clipboard</param>
        public void SetCustom(string mimeType, string text)
        {
            var data = new ClipboardData
            {
                CustomFormatValues =
                {
                    [mimeType] = text
                }
            };

            SetData(data);
        }

        /// <summary>
        /// Copy multiple values to the clipboard
        /// </summary>
        /// <param name="data">Data to copy the clipboard</param>
        /// <returns>Whether the data was successfully copied or not</returns>
        public abstract bool SetData(ClipboardData data);
    }
}
