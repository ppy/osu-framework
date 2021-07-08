using System;
using osuTK;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSTouch
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr sel_normalizedposition = Selector.Get("normalizedPosition");
        private static readonly IntPtr sel_phase = Selector.Get("phase");
        private static readonly IntPtr sel_identity = Selector.Get("identity");
        private static readonly IntPtr sel_isequal = Selector.Get("isEqual:");
        private static readonly IntPtr sel_copy = Selector.Get("copy");

        public NSTouch(IntPtr handle)
        {
            Handle = handle;
        }

        internal Vector2 NormalizedPosition() => Cocoa.SendVector2d(Handle, sel_normalizedposition);

        internal NSTouchPhase Phase() => (NSTouchPhase)Cocoa.SendUint(Handle, sel_phase);

        internal IntPtr Identity() => Cocoa.SendIntPtr(Handle, sel_identity);

        internal IntPtr CopyOfIdentity() => Cocoa.SendIntPtr(Identity(), sel_copy);

        internal bool IsEqual(IntPtr intPtr) => Cocoa.SendBool(Identity(), sel_isequal, intPtr);
    }

    [Flags]
    internal enum NSTouchPhase
    {
        NSTouchPhaseBegan = 1 << 0,
        NSTouchPhaseMoved = 1 << 1,
        NSTouchPhaseStationary = 1 << 2,
        NSTouchPhaseEnded = 1 << 3,
        NSTouchPhaseCancelled = 1 << 4,
        NSTouchPhaseTouching = NSTouchPhaseBegan | NSTouchPhaseMoved | NSTouchPhaseStationary,
        NSTouchPhaseAny = -1
    }
}
