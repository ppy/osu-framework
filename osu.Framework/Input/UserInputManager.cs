// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Mouse;
using OpenTK;

namespace osu.Framework.Input
{
    public class UserInputManager : InputManager
    {
        internal override bool Contains(Vector2 screenSpacePos) => true;

        public override void Load()
        {
            base.Load();

            //AddHandler(new TouchHandler());
            //if (!RawDisctionary.sSkipTablet) AddHandler(new TabletHandler());
            //AddHandler(new JoystickHandler());
            //AddHandler(new RawMouseHandler());
            AddHandler(new CursorMouseHandler());
            AddHandler(new FormMouseHandler());
            AddHandler(new FormKeyboardHandler());
            AddHandler(new OpenTKKeyboardHandler());
        }
    }
}
