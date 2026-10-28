// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Extensions
{
    public static class InputKeyExtensions
    {
        public static bool IsPhysical(this InputKey key)
        {
            if (!Enum.IsDefined(key) || IsVirtual(key))
                return false;

            switch (key)
            {
                case InputKey.None:
                case InputKey.LastKey:
                    return false;

                default:
                    return true;
            }
        }

        public static bool IsVirtual(this InputKey key)
        {
            switch (key)
            {
                case InputKey.Shift:
                case InputKey.Control:
                case InputKey.Alt:
                case InputKey.Super:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// If <paramref name="key"/> is a <see cref="IsPhysical">physical</see> key which is covered by another <see cref="IsVirtual">virtual</see> key, returns that virtual key.
        /// Otherwise, returns <see langword="null"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// &gt; InputKey.LShift.GetVirtualKey()
        /// Shift
        /// &gt; InputKey.RSuper.GetVirtualKey()
        /// Super
        /// &gt; InputKey.A.GetVirtualKey()
        /// null
        /// </code>
        /// </example>
        public static InputKey? GetVirtualKey(this InputKey key)
        {
            switch (key)
            {
                case InputKey.LShift:
                case InputKey.RShift:
                    return InputKey.Shift;

                case InputKey.LControl:
                case InputKey.RControl:
                    return InputKey.Control;

                case InputKey.LAlt:
                case InputKey.RAlt:
                    return InputKey.Alt;

                case InputKey.LSuper:
                case InputKey.RSuper:
                    return InputKey.Super;
            }

            return null;
        }
    }
}
