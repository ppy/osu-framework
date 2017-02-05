// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Lists;
using osu.Framework.Timing;
using OpenTK;

namespace osu.Framework.Graphics
{
    public interface IDrawable : IHasLifetime
    {
        Vector2 DrawSize { get; }

        DrawInfo DrawInfo { get; }

        IContainer Parent { get; set; }

        /// <summary>
        /// Whether this drawable is present for any sort of user-interaction.
        /// If this is false, then this drawable will not be drawn, it will not handle input,
        /// and it will not affect layouting (e.g. autosizing and flow).
        /// </summary>
        bool IsPresent { get; }

        FrameTimeInfo Time { get; }

        IFrameBasedClock Clock { get; }

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in another Drawable's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <param name="other">The drawable in which space we want to transform the vector to.</param>
        /// <returns>The vector in other's coordinates.</returns>
        Vector2 ToSpaceOfOtherDrawable(Vector2 input, IDrawable other);

        /// <summary>
        /// Convert a position to the local coordinate system from either native or local to another drawable.
        /// This is *not* the same space as the Position member variable (use Parent.GetLocalPosition() in this case).
        /// </summary>
        /// <param name="screenSpacePos">The input position.</param>
        /// <returns>The output position.</returns>
        Vector2 ToLocalSpace(Vector2 screenSpacePos);

        BlendingMode BlendingMode { get; }
    }
}