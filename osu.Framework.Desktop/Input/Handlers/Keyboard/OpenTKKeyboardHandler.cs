// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            host.InputScheduler.Add(new ScheduledDelegate(delegate
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
            public TkKeyboardState(OpenTK.Input.KeyboardState tkState)
            {
                List<Key> keys = new List<Key>();

                if (tkState.IsAnyKeyDown)
                {
                    foreach (Key k in Enum.GetValues(typeof(Key)))
                    {
                        if (tkState.IsKeyDown(k))
                            keys.Add(k);
                    }
                }

                Keys = keys;
            }
        }
    }
}
