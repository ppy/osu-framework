// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;

namespace osu.Framework.Extensions
{
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Build a quaternion from the given axis and angle in radians
        /// </summary>
        /// <param name="axis">The axis to rotate about</param>
        /// <param name="angle">The rotation angle in radians</param>
        /// <returns>The equivalent quaternion</returns>
        public static Quaternion FromAxisAngle(Vector3 axis, float angle)
        {
            if (axis.LengthSquared() == 0.0f)
            {
                return Quaternion.Identity;
            }

            Quaternion result = Quaternion.Identity;

            angle *= 0.5f;
            axis = Vector3.Normalize(axis);
            result.X = axis.X * (float)System.Math.Sin(angle);
            result.Y = axis.Y * (float)System.Math.Sin(angle);
            result.Z = axis.Z * (float)System.Math.Sin(angle);
            result.W = (float)System.Math.Cos(angle);

            return Quaternion.Normalize(result);
        }
    }
}
