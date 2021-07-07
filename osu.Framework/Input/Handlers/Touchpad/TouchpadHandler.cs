// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using Vector2 = osuTK.Vector2;

namespace osu.Framework.Input.Handlers.Touchpad
{
    /// <summary>
    /// Base class for Touchpad Handlers.
    /// </summary>
    public class TouchpadHandler : InputHandler
    {
        public override bool IsActive => false;

        public override string Description => "Touchpad";

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>(new Vector2(0.5f, 0.5f));

        // Usually an AreaSize of (1, 1) leads to being unable to reach areas towards the edges.
        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>(new Vector2(0.9f, 0.9f));

        private SDL2DesktopWindow window;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            // Touchpad inputs aren't really desirable in other projects
            // this may cause confusion to have enabled by default.
            Enabled.Value = false;
            Enabled.Default = false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            // Same reasoning as defined in OpenTabletDriver Handler
            if (AreaSize.Value == Vector2.Zero)
                AreaSize.SetDefault();

            if (AreaOffset.Value == Vector2.Zero)
                AreaOffset.SetDefault();

            window = desktopWindow;

            return true;
        }

        public void HandleSingleTouchMove(Vector2 input)
        {
            // Takes raw touchpad inputs, applies area offset and area size, and turn them into Mouse Position events.
            Vector2 vector = input + (AreaSize.Value / 2) - AreaOffset.Value;
            vector = Vector2.Divide(vector, AreaSize.Value);
            vector *= new Vector2(window.Size.Width, window.Size.Height);

            enqueueInput(new MousePositionAbsoluteInput { Position = vector });
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
