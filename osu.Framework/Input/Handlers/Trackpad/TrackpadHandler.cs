// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Logging;
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
        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2> {  };

        private SDL2DesktopWindow window;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;
            
            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            Enabled.Default = false;

            AreaOffset.Default = new Vector2(0.5f, 0.5f);

            if (AreaOffset.Value == Vector2.Zero)
                AreaOffset.SetDefault();

            AreaSize.Default = new Vector2(1, 1);

            if (AreaSize.Value == Vector2.Zero)
                AreaSize.SetDefault();

            // By default this should be disabled.
            window = desktopWindow;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window.TrackpadPositionChanged += handleTrackpadMove;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.TrackpadPositionChanged -= handleTrackpadMove;
                    window.MouseWheel -= handleMouseWheel;
                }
            }, true);

            return true;
        }

        private void handleTrackpadMove(Vector2[] vectors)
        {
            // todo: doesn't accoutn for window transofrmations in lazer
            // possible multitouchsupport
            Vector2 vector = vectors[0];
            vector.Y = 1 - vector.Y;
            // enqueueInput(new MousePositionAbsoluteInput() { Position = Vector2.Divide(vector + (AreaSize.Value / 2) - AreaOffset.Value, AreaSize.Value) * new Vector2(window.Size.Width, window.Size.Height) });
        }

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });

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
