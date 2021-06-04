// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Trackpad
{
    public class TrackpadHandler : InputHandler
    {
        public override bool IsActive => throw new System.NotImplementedException();

        public override string Description => "Trackpad";

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;


            return true;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TrackpadEvents);
        }
    }
}
