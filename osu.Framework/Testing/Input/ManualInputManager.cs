// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualInputManager : PassThroughInputManager
    {
        private readonly ManualInputHandler handler;

        public ManualInputManager()
        {
            UseParentState = true;
            AddHandler(handler = new ManualInputHandler());
        }

        public void MoveMouseTo(Drawable drawable)
        {
            UseParentState = false;
            MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
        }

        public void MoveMouseTo(Vector2 position)
        {
            UseParentState = false;
            handler.MoveMouseTo(position);
        }

        public void Click(MouseButton button)
        {
            UseParentState = false;
            handler.Click(button);
        }
    }
}
