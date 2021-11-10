// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class AbsoluteTabletMode : AbsoluteOutputMode
    {
        public AbsoluteTabletMode(IAbsolutePointer pointer)
        {
            Pointer = pointer;
        }

        public override IAbsolutePointer Pointer { get; }
    }
}
#endif
