// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.Handlers;

namespace osu.Framework.Input
{
    public class UserInputManager : InputManager
    {
        public UserInputManager()
        {
            AlwaysReceiveInput = true;
        }

        protected override IEnumerable<InputHandler> InputHandlers => Host.AvailableInputHandlers;
    }
}
