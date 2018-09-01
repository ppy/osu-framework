// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Graphics
{
    public struct DrawInfo : IEquatable<DrawInfo>
    {
        public Matrix3x2 Matrix;
        public Matrix3x2 MatrixInverse;
        public ColourInfo Colour;
        public BlendingInfo Blending;

        public DrawInfo(Matrix3x2? matrix = null, Matrix3x2? matrixInverse = null, ColourInfo? colour = null, BlendingInfo? blending = null)
        {
            Matrix = matrix ?? MatrixExtensions.TruncatedIdentityMatrix;
            MatrixInverse = matrixInverse ?? MatrixExtensions.TruncatedIdentityMatrix;
            Colour = colour ?? ColourInfo.SingleColour(Color4.White);
            Blending = blending ?? new BlendingInfo();
        }

        /// <summary>
        /// Applies a transformation to the current DrawInfo.
        /// </summary>
        /// <param name="translation">The amount by which to translate the current position.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <param name="rotation">The amount by which to rotate.</param>
        /// <param name="shear">The shear amounts for both directions.</param>
        /// <param name="origin">The center of rotation and scale.</param>
        public void ApplyTransform(Vector2 translation, Vector2 scale, float rotation, Vector2 shear, Vector2 origin)
        {
            if (translation != Vector2.Zero)
            {
                MatrixExtensions.TranslateFromLeft(ref Matrix, translation);
                MatrixExtensions.TranslateFromRight(ref MatrixInverse, -translation);
            }

            if (rotation != 0)
            {
                float radians = MathHelper.DegreesToRadians(rotation);
                MatrixExtensions.RotateFromLeft(ref Matrix, radians);
                MatrixExtensions.RotateFromRight(ref MatrixInverse, -radians);
            }

            if (shear != Vector2.Zero)
            {
                MatrixExtensions.ShearFromLeft(ref Matrix, -shear);
                MatrixExtensions.ShearFromRight(ref MatrixInverse, shear);
            }

            if (scale != Vector2.One)
            {
                Vector2 inverseScale = new Vector2(1.0f / scale.X, 1.0f / scale.Y);
                MatrixExtensions.ScaleFromLeft(ref Matrix, scale);
                MatrixExtensions.ScaleFromRight(ref MatrixInverse, inverseScale);
            }

            if (origin != Vector2.Zero)
            {
                MatrixExtensions.TranslateFromLeft(ref Matrix, -origin);
                MatrixExtensions.TranslateFromRight(ref MatrixInverse, origin);
            }

            // MatrixInverse = MatrixExtensions.Invert(Matrix);
        }

        public bool Equals(DrawInfo other)
        {
            return Matrix.Equals(other.Matrix) && Colour.Equals(other.Colour) && Blending.Equals(other.Blending);
        }

        public override string ToString() => $@"{GetType().ReadableName().Replace(@"DrawInfo", string.Empty)} DrawInfo";
    }
}
