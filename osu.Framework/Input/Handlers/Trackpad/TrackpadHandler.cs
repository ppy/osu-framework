// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Trackpad
{
    /// <summary>
    /// Handles mouse events from an <see cref="SDL2DesktopWindow"/>.
    /// Will use relative mouse mode where possible.
    /// </summary>
    public class TrackpadHandler : InputHandler
    {
        public override string Description => "Trackpad";

        public override bool IsActive => true;

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>();

        private SDL2DesktopWindow window;

        public override bool Initialize(GameHost host)
        {
            // By default this should be disabled.
            Enabled.Value = false;
            AreaOffset.Default = new Vector2(0.5f, 0.5f);
            AreaSize.Default = new Vector2(0.9f, 0.9f);

            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window.TrackpadPositionChanged += HandleTrackpadMove;
                }
                else
                {
                    window.TrackpadPositionChanged -= HandleTrackpadMove;
                }
            }, true);

            return true;
        }

        public void HandleTrackpadMove(Vector2[] vectors)
        {
            // todo: doesn't accoutn for window transofrmations in lazer
            Vector2 vector = vectors[0];
            vector.Y = 1 - vector.Y;
            enqueueInput(new MousePositionAbsoluteInput() { Position = Vector2.Divide(vector + (AreaSize.Value / 2) - AreaOffset.Value, AreaSize.Value) * new Vector2(window.Size.Width, window.Size.Height) });
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
            FrameStatistics.Increment(StatisticsCounterType.TrackpadEvents);
        }
    }
}
