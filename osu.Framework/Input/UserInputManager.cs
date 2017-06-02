// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.Handlers;
using osu.Framework.Allocation;

namespace osu.Framework.Input
{
    public class UserInputManager : InputManager
    {
        public UserInputManager()
        {
            AlwaysReceiveInput = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (Host != null)
            {
                foreach (InputHandler h in Host.AvailableInputHandlers)
                    AddHandler(h);
            }
        }
    }
}
