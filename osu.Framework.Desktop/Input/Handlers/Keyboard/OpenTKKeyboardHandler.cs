// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK.Input;
using KeyboardState = osu.Framework.Input.KeyboardState;
using osu.Framework.Statistics;

namespace osu.Framework.Desktop.Input.Handlers.Keyboard
{
    class OpenTKKeyboardHandler : InputHandler
    {
        private ScheduledDelegate scheduled;

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
            {
                PendingStates.Enqueue(new InputState
                {
                    Keyboard = new TkKeyboardState(host.IsActive ? OpenTK.Input.Keyboard.GetState() : new OpenTK.Input.KeyboardState())
                });

                FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
            }, 0, 0));

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            scheduled.Cancel();
        }

        class TkKeyboardState : KeyboardState
        {
            private static readonly IEnumerable<Key> all_keys = Enum.GetValues(typeof(Key)).Cast<Key>();

            public TkKeyboardState(OpenTK.Input.KeyboardState tkState)
            {
                if (tkState.IsAnyKeyDown)
                    Keys = all_keys.Where(tkState.IsKeyDown);
            }
        }
    }
}
