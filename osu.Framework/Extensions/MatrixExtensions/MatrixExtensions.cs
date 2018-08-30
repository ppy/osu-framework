// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;

namespace osu.Framework.Extensions.MatrixExtensions
{
    public static class MatrixExtensions
    {
        public static void TranslateFromLeft(ref Matrix3x2 m, Vector2 v)
        {
            m.Row2 += m.Row0 * v.X + m.Row1 * v.Y;
        }

        public static void TranslateFromRight(ref Matrix3x2 m, Vector2 v)
        {
            //m.Column0 += m.Column2 * v.X;
            m.M31 += v.X;
            m.M32 += v.Y;
        }

        public static void RotateFromLeft(ref Matrix3x2 m, float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            Vector2 row0 = m.Row0 * cos + m.Row1 * sin;
            m.Row1 = m.Row1 * cos - m.Row0 * sin;
            m.Row0 = row0;
        }

        public static void RotateFromRight(ref Matrix3x2 m, float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            //Vector3 column0 = m.Column0 * cos + m.Column1 * sin;
            float m11 = m.M11 * cos - m.M12 * sin;
            float m21 = m.M21 * cos - m.M22 * sin;
            float m31 = m.M31 * cos - m.M32 * sin;

            //m.Column1 = m.Column1 * cos - m.Column0 * sin;
            m.M12 = m.M12 * cos + m.M11 * sin;
            m.M22 = m.M22 * cos + m.M21 * sin;
            m.M32 = m.M32 * cos + m.M31 * sin;

            //m.Column0 = row0;
            m.M11 = m11;
            m.M21 = m21;
            m.M31 = m31;
        }

        public static void ScaleFromLeft(ref Matrix3x2 m, Vector2 v)
        {
            m.Row0 *= v.X;
            m.Row1 *= v.Y;
        }

        public static void ScaleFromRight(ref Matrix3x2 m, Vector2 v)
        {
            //m.Column0 *= v.X;
            m.M11 *= v.X;
            m.M21 *= v.X;
            m.M31 *= v.X;

            //m.Column1 *= v.Y;
            m.M12 *= v.Y;
            m.M22 *= v.Y;
            m.M32 *= v.Y;
        }

        /// <summary>
        /// Apply shearing in X and Y direction from the left hand side.
        /// Since shearing is non-commutative it is important to note that we
        /// first shear in the X direction, and then in the Y direction.
        /// </summary>
        /// <param name="m">The matrix to apply the shearing operation to.</param>
        /// <param name="v">The X and Y amounts of shearing.</param>
        public static void ShearFromLeft(ref Matrix3x2 m, Vector2 v)
        {
            Vector2 row0 = m.Row0 + m.Row1 * v.Y + m.Row0 * v.X * v.Y;
            m.Row1 += m.Row0 * v.X;
            m.Row0 = row0;
        }

        /// <summary>
        /// Apply shearing in X and Y direction from the right hand side.
        /// Since shearing is non-commutative it is important to note that we
        /// first shear in the Y direction, and then in the X direction.
        /// </summary>
        /// <param name="m">The matrix to apply the shearing operation to.</param>
        /// <param name="v">The X and Y amounts of shearing.</param>
        public static void ShearFromRight(ref Matrix3x2 m, Vector2 v)
        {
            float xy = v.X * v.Y;

            //m.Column0 += m.Column1 * v.X;
            float m11 = m.M11 + m.M12 * v.X;
            float m21 = m.M21 + m.M22 * v.X;
            float m31 = m.M31 + m.M32 * v.X;

            //m.Column1 += m.Column0 * v.Y + m.Column1 * xy;
            m.M12 += m.M11 * v.Y + m.M12 * xy;
            m.M22 += m.M21 * v.Y + m.M22 * xy;
            m.M32 += m.M31 * v.Y + m.M32 * xy;

            m.M11 = m11;
            m.M21 = m21;
            m.M31 = m31;
        }

        public static Matrix3x2 Invert(Matrix3x2 value)
        {
            float d11 = value.M22;
            float d12 = value.M21;
            float d13 = value.M21 * value.M32 + value.M22 * -value.M31;

            float det = value.M11 * d11 - value.M12 * d12;

            if (Math.Abs(det) == 0.0f)
            {
                return Matrix3x2.Zero;
            }

            det = 1f / det;

            float d21 = value.M12;
            float d22 = value.M11;
            float d23 = value.M11 * value.M32 + value.M12 * -value.M31;

            return new Matrix3x2(
                +d11 * det,
                -d21 * det,
                -d12 * det,
                +d22 * det,
                +d13 * det,
                -d23 * det);
        }

        public static Matrix3x2 Mult(Matrix3x2 a, Matrix3x2 b)
        {
            return new Matrix3x2(
                a.M11 * b.M11 + a.M12 * b.M21,
                a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21,
                a.M21 * b.M12 + a.M22 * b.M22,
                a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
                a.M31 * b.M12 + a.M32 * b.M22 + b.M32);
        }

        public static Matrix3 ToMatrix3(Matrix3x2 m)
        {
            return new Matrix3(m.M11, m.M12, 0, m.M21, m.M22, 0, m.M31, m.M32, 1);
        }

        public static Matrix2 RemoveTranslation(Matrix3x2 m)
        {
            return new Matrix2(m.M11, m.M12, m.M21, m.M22);
        }

        public static Vector3 ExtractScale(Matrix3x2 m)
        {
            return new Vector3(m.Row0.Length, m.Row1.Length, m.Row2.Length);
        }

        public static Matrix3x2 TruncatedIdentityMatrix = new Matrix3x2(1, 0, 0, 1, 0, 0);
    }
}
