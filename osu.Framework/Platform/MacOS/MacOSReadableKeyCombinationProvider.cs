// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL2;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSReadableKeyCombinationProvider : SDL2ReadableKeyCombinationProvider
    {
        protected override string GetReadableKey(InputKey key)
        {
            switch (key)
            {
                case InputKey.Super:
                    return "Cmd";

                case InputKey.LSuper:
                    return "LCmd";

                case InputKey.RSuper:
                    return "RCmd";

                case InputKey.Alt:
                    return "Option";

                case InputKey.LAlt:
                    return "LOption";

                case InputKey.RAlt:
                    return "ROption";

                default:
                    return base.GetReadableKey(key);
            }
        }
    }
}
