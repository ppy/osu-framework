// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.Handlers;
using OpenTK;

namespace osu.Framework.Input
{
    public class UserInputManager : InputManager
    {
        public override bool Contains(Vector2 screenSpacePos) => true;

        public override void Load()
        {
            base.Load();

            if (Game?.Host != null)
            {
                foreach (InputHandler h in Game.Host.GetInputHandlers())
                    AddHandler(h);
            }
        }
    }
}
