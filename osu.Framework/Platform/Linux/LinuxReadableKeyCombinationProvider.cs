// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL2;

namespace osu.Framework.Platform.Linux
{
    public class LinuxReadableKeyCombinationProvider : SDL2ReadableKeyCombinationProvider
    {
        protected override string GetReadableKey(InputKey key)
        {
            switch (key)
            {
                case InputKey.Super:
                    return "Super";

                case InputKey.LSuper:
                    return "LSuper";

                case InputKey.RSuper:
                    return "RSuper";

                default:
                    return base.GetReadableKey(key);
            }
        }
    }
}
