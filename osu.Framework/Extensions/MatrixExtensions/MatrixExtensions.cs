// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;

namespace osu.Framework.Extensions.MatrixExtensions
{
    public static class MatrixExtensions
    {
        #region Matrix3x3 extensions and utilities, implemented using Matrix4x4

        public static void TranslateFromLeft(ref Matrix4x4 m, Vector2 v)
        {
            //m.Row2 += m.Row0 * v.X + m.Row1 * v.Y;
            m.M31 += m.M11 * v.X + m.M21 * v.Y;
            m.M32 += m.M12 * v.X + m.M22 * v.Y;
        }

        public static void TranslateFromRight(ref Matrix4x4 m, Vector2 v)
        {
            //m.Column0 += m.Column2 * v.X;
            m.M11 += m.M13 * v.X;
            m.M21 += m.M23 * v.X;
            m.M31 += m.M33 * v.X;

            //m.Column1 += m.Column2 * v.Y;
            m.M12 += m.M13 * v.Y;
            m.M22 += m.M23 * v.Y;
            m.M32 += m.M33 * v.Y;
        }

        public static void RotateFromLeft(ref Matrix4x4 m, float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            //Vector3 row0 = m.Row0 * cos + m.Row1 * sin;
            float m11 = m.M11 * cos + m.M21 * sin;
            float m12 = m.M12 * cos + m.M22 * sin;
            float m13 = m.M13 * cos + m.M23 * sin;

            //m.Row1 = m.Row1 * cos - m.Row0 * sin;
            m.M21 = m.M21 * cos - m.M11 * sin;
            m.M22 = m.M22 * cos - m.M12 * sin;
            m.M23 = m.M23 * cos - m.M13 * sin;

            //m.Row0 = row0
            m.M11 = m11;
            m.M12 = m12;
            m.M13 = m13;
        }

        public static void RotateFromRight(ref Matrix4x4 m, float radians)
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

        public static void ScaleFromLeft(ref Matrix4x4 m, Vector2 v)
        {
            //m.Row0 *= v.X;
            m.M11 *= v.X;
            m.M12 *= v.X;
            m.M13 *= v.X;

            //m.Row1 *= v.Y;
            m.M21 *= v.Y;
            m.M22 *= v.Y;
            m.M23 *= v.Y;
        }

        public static void ScaleFromRight(ref Matrix4x4 m, Vector2 v)
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
        public static void ShearFromLeft(ref Matrix4x4 m, Vector2 v)
        {
            float xy = v.X * v.Y;

            //Vector3 row0 = m.Row0 + m.Row1 * v.Y + m.Row0 * v.X * v.Y;
            float m11 = m.M11 + m.M21 * v.Y + m.M11 * xy;
            float m12 = m.M12 + m.M22 * v.Y + m.M12 * xy;
            float m13 = m.M13 + m.M23 * v.Y + m.M13 * xy;

            //m.Row1 += m.Row0 * v.X;
            m.M21 += m.M11 * v.X;
            m.M22 += m.M12 * v.X;
            m.M23 += m.M13 * v.X;

            //m.Row0 = row0;
            m.M11 = m11;
            m.M12 = m12;
            m.M13 = m13;
        }

        /// <summary>
        /// Apply shearing in X and Y direction from the right hand side.
        /// Since shearing is non-commutative it is important to note that we
        /// first shear in the Y direction, and then in the X direction.
        /// </summary>
        /// <param name="m">The matrix to apply the shearing operation to.</param>
        /// <param name="v">The X and Y amounts of shearing.</param>
        public static void ShearFromRight(ref Matrix4x4 m, Vector2 v)
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

        public static void FastInvert(ref Matrix4x4 value)
        {
            float d11 = value.M22 * value.M33 + value.M23 * -value.M32;
            float d12 = value.M21 * value.M33 + value.M23 * -value.M31;
            float d13 = value.M21 * value.M32 + value.M22 * -value.M31;

            float det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13;

            if (Math.Abs(det) == 0.0f)
            {
                value = Zero;
                return;
            }

            det = 1f / det;

            float d21 = value.M12 * value.M33 + value.M13 * -value.M32;
            float d22 = value.M11 * value.M33 + value.M13 * -value.M31;
            float d23 = value.M11 * value.M32 + value.M12 * -value.M31;

            float d31 = value.M12 * value.M23 - value.M13 * value.M22;
            float d32 = value.M11 * value.M23 - value.M13 * value.M21;
            float d33 = value.M11 * value.M22 - value.M12 * value.M21;

            value.M11 = +d11 * det;
            value.M12 = -d21 * det;
            value.M13 = +d31 * det;
            value.M21 = -d12 * det;
            value.M22 = +d22 * det;
            value.M23 = -d32 * det;
            value.M31 = +d13 * det;
            value.M32 = -d23 * det;
            value.M33 = +d33 * det;
        }

        public static Vector3 ExtractScale(this Matrix4x4 mat)
        {
            float row1Length = new Vector3(mat.M11, mat.M12, mat.M13).Length();
            float row2Length = new Vector3(mat.M21, mat.M22, mat.M23).Length();
            float row3Length = new Vector3(mat.M31, mat.M32, mat.M33).Length();
            return new Vector3(row1Length, row2Length, row3Length);
        }

        public static osuTK.Matrix3 ToMatrix3(this Matrix4x4 mat)
            => new osuTK.Matrix3(mat.M11, mat.M12, mat.M13, mat.M21, mat.M22, mat.M23, mat.M31, mat.M32, mat.M33);
        
        #endregion

        #region Matrix4x4 extensions and utilities.

        public static readonly Matrix4x4 Zero = new Matrix4x4();

        #endregion
    }
}
