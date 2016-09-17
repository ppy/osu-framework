//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Input;
using OpenTK;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : LargeContainer
    {
        private Cursor cursor;

        public CursorContainer()
        {
            Add(cursor = new Cursor());
        }

        internal override bool Contains(Vector2 screenSpacePos) => true;

        protected override bool OnMouseMove(InputState state)
        {
            cursor.Position = GetLocalPosition(state.Mouse.NativePosition);
            return base.OnMouseMove(state);
        }

        class Cursor : Box
        {
            public Cursor()
            {
                Size = new Vector2(5, 5);
                Origin = Anchor.Centre;
            }
        }
    }
}
