// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;
using OpenTK;

namespace osu.Framework.Graphics
{
    public interface IDrawable
    {
        Vector2 DrawSize { get; }

        DrawInfo DrawInfo { get; }

        IContainer Parent { get; set; }

        FrameTimeInfo Time { get; }

        /// <summary>
        /// Convert a position to the local coordinate system from either native or local to another drawable.
        /// This is *not* the same space as the Position member variable (use Parent.GetLocalPosition() in this case).
        /// </summary>
        /// <param name="screenSpacePos">The input position.</param>
        /// <returns>The output position.</returns>
        Vector2 GetLocalPosition(Vector2 screenSpacePos);
    }
}