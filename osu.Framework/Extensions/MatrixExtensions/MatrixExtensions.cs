//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Extensions.MatrixExtensions
{
    public static class MatrixExtensions
    {
        public static Matrix3 TranslateTo(this Matrix3 m, Vector2 v)
        {
            m.Row2 += m.Row0 * v.X + m.Row1 * v.Y;

            return m;
        }

        public static Matrix3 RotateTo(this Matrix3 m, float angle)
        {
            // Convert to radians
            angle = angle / (180 / MathHelper.Pi);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            Vector3 temp = m.Row0 * cos + m.Row1 * sin;
            m.Row1 = m.Row1 * cos - m.Row0 * sin;
            m.Row0 = temp;

            return m;
        }

        public static Matrix3 FastInvert(this Matrix3 value)
        {
            Matrix3 result = Matrix3.Zero;
            float d11 = value.M22 * value.M33 + value.M23 * -value.M32;
            float d12 = value.M21 * value.M33 + value.M23 * -value.M31;
            float d13 = value.M21 * value.M32 + value.M22 * -value.M31;

            float det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13;

            if (Math.Abs(det) == 0.0f)
                return result;

            det = 1f / det;

            float d21 = value.M12 * value.M33 + value.M13 * -value.M32;
            float d22 = value.M11 * value.M33 + value.M13 * -value.M31;
            float d23 = value.M11 * value.M32 + value.M12 * -value.M31;

            float d31 = (value.M12 * value.M23) - (value.M13 * value.M22);
            float d32 = (value.M11 * value.M23) - (value.M13 * value.M21);
            float d33 = (value.M11 * value.M22) - (value.M12 * value.M21);

            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det;

            return result;
        }

        public static Matrix3 ScaleTo(this Matrix3 m, Vector2 v)
        {
            m.Row0 *= v.X;
            m.Row1 *= v.Y;

            return m;
        }
    }
}
