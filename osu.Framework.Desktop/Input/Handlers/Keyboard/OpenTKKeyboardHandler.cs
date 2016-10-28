// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Input.Handlers;
using OpenTK.Input;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Desktop.Input.Handlers.Keyboard
{
    class OpenTKKeyboardHandler : InputHandler, IKeyboardInputHandler
    {
        public override bool IsActive => true;

        private KeyboardState state;

        public override int Priority => 0;

        public override void Dispose()
        {
        }

        public override bool Initialize(BasicGameHost host)
        {
            PressedKeys = new List<Key>();

            host.InputScheduler.Add(new ScheduledDelegate(delegate
            {
                state = host.IsActive ? OpenTK.Input.Keyboard.GetState() : new KeyboardState();
            }, 0, 0));

            return true;
        }

        public override void UpdateInput(bool isActive)
        {
            PressedKeys.Clear();

            if (state.IsAnyKeyDown)
            {
                foreach (Key k in Enum.GetValues(typeof(Key)))
                {
                    if (state.IsKeyDown(k))
                        PressedKeys.Add(k);
                }
            }
        }

        public List<Key> PressedKeys { get; set; }
    }
}
