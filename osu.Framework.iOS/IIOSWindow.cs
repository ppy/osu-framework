// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    /// <summary>
    /// Interface representation of the game window on the iOS platform.
    /// </summary>
    public interface IIOSWindow : IWindow
    {
        /// <summary>
        /// The underlying <see cref="UIWindow"/> associated to this window.
        /// </summary>
        UIWindow UIWindow { get; }

        /// <summary>
        /// The <see cref="UIViewController"/> presenting this window's content.
        /// </summary>
        UIViewController ViewController { get; }
    }
}
