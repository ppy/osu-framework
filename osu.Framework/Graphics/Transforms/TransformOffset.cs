// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Transforms
{
    internal class TransformOffset<TValue, TEasing, T> : TransformCustom<TValue, TEasing, T>
        where T : class, ITransformable
        where TEasing : IEasingFunction
    {
        private readonly TValue offset;

        public TransformOffset(string propertyOrFieldName, string grouping, TValue offset)
            : base(propertyOrFieldName, grouping)
        {
            Debug.Assert(Validation.IsNumericType<TValue>() || typeof(TValue) == typeof(Vector2));

            this.offset = offset;
        }

        protected override void ReadValues(T d)
        {
            base.ReadValues(d);

            EndValue = sum(StartValue, offset);
        }

        private static TValue sum(TValue a, TValue b)
        {
            if (typeof(TValue) == typeof(Vector2))
                return (TValue)(object)((Vector2)(object)a + (Vector2)(object)b);

            if (typeof(TValue) == typeof(sbyte))
                return (TValue)(object)((sbyte)(object)a + (sbyte)(object)b);
            if (typeof(TValue) == typeof(byte))
                return (TValue)(object)((byte)(object)a + (byte)(object)b);
            if (typeof(TValue) == typeof(short))
                return (TValue)(object)((short)(object)a + (short)(object)b);
            if (typeof(TValue) == typeof(ushort))
                return (TValue)(object)((ushort)(object)a + (ushort)(object)b);
            if (typeof(TValue) == typeof(int))
                return (TValue)(object)((int)(object)a + (int)(object)b);
            if (typeof(TValue) == typeof(uint))
                return (TValue)(object)((uint)(object)a + (uint)(object)b);
            if (typeof(TValue) == typeof(long))
                return (TValue)(object)((long)(object)a + (long)(object)b);
            if (typeof(TValue) == typeof(ulong))
                return (TValue)(object)((ulong)(object)a + (ulong)(object)b);
            if (typeof(TValue) == typeof(float))
                return (TValue)(object)((float)(object)a + (float)(object)b);

            return (TValue)(object)((double)(object)a + (double)(object)b);
        }
    }
}
