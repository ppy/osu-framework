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

namespace osu.Framework.Desktop.Input.Handlers.Keyboard
{
    class OpenTKKeyboardHandler : InputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(BasicGameHost host)
        {
            host.InputThread.Scheduler.Add(new ScheduledDelegate(delegate
            {
                PendingStates.Enqueue(new InputState
                {
                    Keyboard = new TkKeyboardState(host.IsActive ? OpenTK.Input.Keyboard.GetState() : new OpenTK.Input.KeyboardState())
                });
            }, 0, 0));

            return true;
        }

        class TkKeyboardState : KeyboardState
        {
            private static IEnumerable<Key> allKeys = Enum.GetValues(typeof(Key)).Cast<Key>();

            public TkKeyboardState(OpenTK.Input.KeyboardState tkState)
            {
                if (tkState.IsAnyKeyDown)
                    Keys = allKeys.Where(tkState.IsKeyDown);
            }
        }
    }
}
