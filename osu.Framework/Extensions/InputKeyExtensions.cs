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
    }
}
