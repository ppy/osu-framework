// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#if NET6_0_OR_GREATER
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

        public override IAbsolutePointer Pointer { get; set; }
    }
}
#endif
