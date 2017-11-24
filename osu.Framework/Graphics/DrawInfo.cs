// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        public Matrix3 Matrix;
        public Matrix3 MatrixInverse;
        public ColourInfo Colour;
        public BlendingInfo Blending;

        public DrawInfo(Matrix3? matrix = null, Matrix3? matrixInverse = null, ColourInfo? colour = null, BlendingInfo? blending = null)
        {
            Matrix = matrix ?? Matrix3.Identity;
            MatrixInverse = matrixInverse ?? Matrix3.Identity;
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
            checkComponentValid(translation, nameof(translation));
            checkComponentValid(scale, nameof(scale));
            checkComponentValid(rotation, nameof(rotation));
            checkComponentValid(shear, nameof(shear));
            checkComponentValid(origin, nameof(origin));

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

            //========================================================================================
            //== Uncomment the following 2 lines to use a ground-truth matrix inverse for debugging ==
            //========================================================================================
            //target.MatrixInverse = target.Matrix;
            //MatrixExtensions.FastInvert(ref target.MatrixInverse);
        }

        private void checkComponentValid(float component, string name)
        {
            if (isInvalidFloat(component))
                throw new ArgumentException($"Invalid value ({component}) provided for component {name}.");
        }

        private void checkComponentValid(Vector2 component, string name)
        {
            if (isInvalidFloat(component.X))
                throw new ArgumentException($"Invalid value ({component}) provided for component {name}.X.");
            if (isInvalidFloat(component.Y))
                throw new ArgumentException($"Invalid value ({component}) provided for component {name}.Y.");
        }

        // Takes the bits from value, interprets them as an int and then shifts them so that we get the exponent (bit 2 to 8) back.
        // Returns as byte because it's a smaller data type and enough for the exponent
        private unsafe byte singleToExponentAsByte(float value) => (byte)((*(int*)(&value)) >> 23);

        // If exponent is 255, float is either ±Infinity or NaN (by definition)
        private bool isInvalidFloat(float toCheck) => singleToExponentAsByte(toCheck) == byte.MaxValue;

        public bool Equals(DrawInfo other)
        {
            return Matrix.Equals(other.Matrix) && Colour.Equals(other.Colour) && Blending.Equals(other.Blending);
        }

        public override string ToString() => $@"{GetType().ReadableName().Replace(@"DrawInfo", string.Empty)} DrawInfo";
    }
}
