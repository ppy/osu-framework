// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Touchpad
{
    /// <summary>
    /// Base class for a
    /// </summary>
    public class TouchpadHandler : InputHandler
    {
        public override bool IsActive => Enabled.Value;

        public override string Description => "Touchpad";

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>(new Vector2(0.5f, 0.5f));

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>(new Vector2(0.9f, 0.9f));

        public Vector2 Size { get; }

        private SDL2DesktopWindow window;

        public override bool Initialize(GameHost host)
        {
            // Touchpad inputs are really only used for osu!
            // other projects don't really need them, and may
            // cause confusion to have enabled by default.
            Enabled.Value = false;

            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            if (AreaSize.Value == Vector2.Zero)
                AreaSize.SetDefault();

            if (AreaOffset.Value == Vector2.Zero)
                AreaOffset.SetDefault();

            window = desktopWindow;

            return true;
        }

        // Takes raw touchpad inputs and turn them into Mouse Position events.
        public void HandleSingleTouchMove(Vector2 vector)
        {
            enqueueInput(new MousePositionAbsoluteInput() { Position = Vector2.Divide(vector + (AreaSize.Value / 2) - AreaOffset.Value, AreaSize.Value) * new Vector2(window.Size.Width, window.Size.Height)  });
        }

        public override void Reset()
        {
            AreaOffset.SetDefault();
            AreaSize.SetDefault();
            base.Reset();
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TouchpadEvents);
        }
    }
}
