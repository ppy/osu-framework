// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osuTK;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Creates a new camera that looks at a <see cref="Drawable"/>.
    /// </summary>
    /// <param name="FovY">The y-axis field of view.</param>
    /// <param name="Position">The position of the camera relative to the Drawable.</param>
    public readonly record struct Camera(float FovY, Vector2 Position, float ZNear = 0.1f, float ZFar = 1000f)
    {
        public Matrix4 CreateMatrix(RectangleF viewport)
        {
            if (FovY == 0)
                return Matrix4.Identity;

            float focalLength = 2 * MathF.Tan(0.5f * MathUtils.DegreesToRadians(FovY));

            Matrix4 mat = new Matrix4(
                focalLength * viewport.Height / viewport.Width, 0, 0, 0,
                0, -focalLength, 0, 0,
                0, 0, (ZFar + ZNear) / (ZNear - ZFar), 1.0f,
                0, 0, 2 * ZFar * ZNear / (ZNear - ZFar), 0);

            return Matrix4.CreateTranslation(-Position.X * viewport.Width, -Position.Y * viewport.Height, viewport.Height * focalLength / 2) * mat;
        }
    }
}
