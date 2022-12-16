// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.MatrixExtensions;
using osuTK;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Utils;

namespace osu.Framework.Graphics
{
    public struct DrawInfo : IEquatable<DrawInfo>
    {
        public Matrix3 Matrix;
        public Matrix3 MatrixInverse;

        public DrawInfo(Matrix3? matrix = null, Matrix3? matrixInverse = null)
        {
            Matrix = matrix ?? Matrix3.Identity;
            MatrixInverse = matrixInverse ?? Matrix3.Identity;
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
                float radians = MathUtils.DegreesToRadians(rotation);
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
                // Zero scale leads to unexpected input and autosize calculations, so it's clamped to a sane value.
                if (scale.X == 0) scale.X = Precision.FLOAT_EPSILON;
                if (scale.Y == 0) scale.Y = Precision.FLOAT_EPSILON;

                MatrixExtensions.ScaleFromLeft(ref Matrix, scale);
                MatrixExtensions.ScaleFromRight(ref MatrixInverse, Vector2.Divide(Vector2.One, scale));
            }

            if (origin != Vector2.Zero)
            {
                MatrixExtensions.TranslateFromLeft(ref Matrix, -origin);
                MatrixExtensions.TranslateFromRight(ref MatrixInverse, origin);
            }

            //========================================================================================
            //== Uncomment the following 2 lines to use a ground-truth matrix inverse for debugging ==
            //========================================================================================
            //target.MatrixInverse = target.Matrix;
            //MatrixExtensions.FastInvert(ref target.MatrixInverse);
        }

        public readonly bool Equals(DrawInfo other) => Matrix.Equals(other.Matrix);

        public override string ToString() => $@"{GetType().ReadableName().Replace(@"DrawInfo", string.Empty)} DrawInfo";
    }
}
