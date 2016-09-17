//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics
{
    public struct DrawInfo : IEquatable<DrawInfo>
    {
        public Matrix3 Matrix;
        public Matrix3 MatrixInverse;
        public Color4 Colour;
        public BlendingInfo Blending;

        public DrawInfo(Matrix3? matrix = null, Matrix3? matrixInverse = null, Color4? colour = null, BlendingInfo? blending = null)
        {
            Matrix = matrix ?? Matrix3.Identity;
            MatrixInverse = matrixInverse ?? Matrix3.Identity;
            Colour = colour ?? Color4.White;
            Blending = blending ?? new BlendingInfo();
        }

        /// <summary>
        /// Applies a transformation to the current DrawInfo.
        /// </summary>
        /// <param name="target">The DrawInfo instance to be filled with the result.</param>
        /// <param name="translation">The amount by which to translate the current position.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <param name="rotation">The amount by which to rotate.</param>
        /// <param name="origin">The center of rotation and scale.</param>
        /// <param name="colour">An optional color to be applied multiplicatively.</param>
        /// <param name="viewport">An optional new viewport size.</param>
        public void ApplyTransform(ref DrawInfo target, Vector2 translation, Vector2 scale, float rotation, Vector2 origin, Color4? colour = null, BlendingInfo? blending = null)
        {
            target.Matrix = Matrix;
            target.MatrixInverse = MatrixInverse;

            if (translation != Vector2.Zero)
                MatrixExtensions.Translate(ref target.Matrix, translation);

            if (rotation != 0)
                MatrixExtensions.Rotate(ref target.Matrix, rotation);

            if (scale != Vector2.One)
                MatrixExtensions.Scale(ref target.Matrix, scale);

            if (origin != Vector2.Zero)
            {
                MatrixExtensions.Translate(ref target.Matrix, -origin);
                MatrixExtensions.Translate(ref target.MatrixInverse, origin);
            }

            if (scale != Vector2.One)
            {
                Vector2 inverseScale = new Vector2(1.0f / scale.X, 1.0f / scale.Y);
                MatrixExtensions.Scale(ref target.MatrixInverse, inverseScale);
            }

            if (rotation != 0)
                MatrixExtensions.Rotate(ref target.MatrixInverse, -rotation);

            if (translation != Vector2.Zero)
                MatrixExtensions.Translate(ref target.MatrixInverse, -translation);

            target.Colour = Colour;

            if (colour != null)
            {
                target.Colour.R *= colour.Value.R;
                target.Colour.G *= colour.Value.G;
                target.Colour.B *= colour.Value.B;
                target.Colour.A *= colour.Value.A;
            }

            if (blending == null)
                Blending.Copy(ref target.Blending);
            else
                blending.Value.Copy(ref target.Blending);
        }

        /// <summary>
        /// Copies the current DrawInfo into target.
        /// </summary>
        /// <param name="target">The DrawInfo to be filled with the copy.</param>
        public void Copy(ref DrawInfo target)
        {
            target.Matrix = Matrix;
            target.MatrixInverse = MatrixInverse;
            target.Colour = Colour;
        }

        public bool Equals(DrawInfo other)
        {
            return Matrix.Equals(other.Matrix) && Colour.Equals(other.Colour);
        }
    }
}
