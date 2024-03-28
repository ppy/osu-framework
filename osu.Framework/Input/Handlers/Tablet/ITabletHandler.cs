// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osuTK;

namespace osu.Framework.Input.Handlers.Tablet
{
    /// <summary>
    /// An interface to access OpenTabletDriverHandler.
    /// Can be considered for removal now that we're solely targeting .NET 6
    /// </summary>
    public interface ITabletHandler
    {
        /// <summary>
        /// The offset of the area which should be mapped to the game window.
        /// </summary>
        Bindable<Vector2> AreaOffset { get; }

        /// <summary>
        /// The size of the area which should be mapped to the game window.
        /// </summary>
        Bindable<Vector2> AreaSize { get; }

        /// <summary>
        /// Relative position of center of output area in game window, padded by half of <see cref="OutputAreaSize"/>.
        /// Values between zero and one for each axe will always place the area inside window:
        /// (0; 0) is most top-left position, (1;1) is most bottom-right position. Does nothing if <see cref="OutputAreaSize"/> equals to (1; 1).
        /// </summary>
        Bindable<Vector2> OutputAreaPosition { get; }

        /// <summary>
        /// Relative size of output area inside game window.
        /// </summary>
        Bindable<Vector2> OutputAreaSize { get; }

        /// <summary>
        /// Information on the currently connected tablet device. May be null if no tablet is detected.
        /// </summary>
        IBindable<TabletInfo> Tablet { get; }

        /// <summary>
        /// The rotation of the tablet area in degrees.
        /// </summary>
        Bindable<float> Rotation { get; }

        BindableBool Enabled { get; }
    }
}
