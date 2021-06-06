using System;
using osuTK;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSTouch
    {

        internal IntPtr Handle { get; }

        private static readonly IntPtr sel_normalizedposition = Selector.Get("normalizedPosition");
        private static readonly IntPtr sel_phase = Selector.Get("phase");
        //view:
        private static readonly IntPtr sel_previouslocationinview = Selector.Get("locationInView:");

        public NSTouch(IntPtr handle)
        {
            Handle = handle;
        }

        internal Vector2 NormalizedPosition() => Cocoa.SendNSPoint(Handle, sel_normalizedposition);

        internal Vector2 PreviousLocationInView(IntPtr intPtr) => Cocoa.SendNSPoint(Handle, sel_previouslocationinview, intPtr);

        internal NSTouchPhase Phase() => (NSTouchPhase) Cocoa.SendUint(Handle, sel_phase);
    }

    enum NSTouchPhase : uint
    {
        NSTouchPhaseBegan = 1u << 0,
        NSTouchPhaseMoved = 1u << 1,
        NSTouchPhaseStationary = 1u << 2,
        NSTouchPhaseEnded = 1u << 3,
        NSTouchPhaseCancelled = 1u << 4,
        NSTouchPhaseTouching = NSTouchPhaseBegan | NSTouchPhaseMoved | NSTouchPhaseStationary,
        NSTouchPhaseAny = uint.MaxValue
    }
}
