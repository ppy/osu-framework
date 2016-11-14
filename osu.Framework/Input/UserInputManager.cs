// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.Handlers;
using OpenTK;
using System.Collections.Generic;
using osu.Framework.Platform;
using osu.Framework.Allocation;

namespace osu.Framework.Input
{
    public class UserInputManager : InputManager
    {
        public override bool Contains(Vector2 screenSpacePos) => true;

        public UserInputManager(BasicGameHost host)
        {
            Host = host;
        }

        [BackgroundDependencyLoader]
        private void load(BaseGame game)
        {
            if (Host != null)
            {
                foreach (InputHandler h in Host.GetInputHandlers())
                    AddHandler(h);
            }
        }
    }
}
