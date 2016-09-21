// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : LargeContainer
    {
        private Cursor cursor;

        public override void Load()
        {
            base.Load();

            Add(cursor = new Cursor());
        }

        internal override bool Contains(Vector2 screenSpacePos) => true;

        protected override bool OnMouseMove(InputState state)
        {
            cursor.Position = state.Mouse.Position;
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
