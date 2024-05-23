// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Input.Bindings;
using osuTK.Input;

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
            if (key >= InputKey.KeycodeA && key <= InputKey.KeycodeZ)
                return true;

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
        /// Gets the <see cref="Key"/> equavalent to this <see cref="InputKey"/>.
        /// </summary>
        /// <remarks>Inverse of <see cref="KeyCombination.FromKey"/>.</remarks>
        public static bool IsKeyboardKey(this InputKey inputKey, out Key key)
        {
            if (!inputKey.IsPhysical())
            {
                key = Key.Unknown;
                return false;
            }

            switch (inputKey)
            {
                case InputKey.LShift:
                    key = Key.LShift;
                    return true;

                case InputKey.RShift:
                    key = Key.RShift;
                    return true;

                case InputKey.LControl:
                    key = Key.LControl;
                    return true;

                case InputKey.RControl:
                    key = Key.RControl;
                    return true;

                case InputKey.LAlt:
                    key = Key.LAlt;
                    return true;

                case InputKey.RAlt:
                    key = Key.RAlt;
                    return true;

                case InputKey.LSuper:
                    key = Key.LWin;
                    return true;

                case InputKey.RSuper:
                    key = Key.RWin;
                    return true;
            }

            if ((inputKey >= InputKey.Menu && inputKey < InputKey.LastKey) || (inputKey >= InputKey.Mute && inputKey <= InputKey.TrackNext))
            {
                key = (Key)inputKey;
                Debug.Assert(Enum.IsDefined(key));
                return true;
            }

            key = Key.Unknown;
            return false;
        }
    }
}
