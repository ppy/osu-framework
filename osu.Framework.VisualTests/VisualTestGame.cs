//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests
{
    class VisualTestGame : Game
    {
        public override void Load()
        {
            base.Load();
            Host.Size = new Vector2(1366, 768);

            SampleGame test = new SampleGame();

            DrawVisualiser drawVis;

            Add(test);
            Add(drawVis = new DrawVisualiser(test));

            Add(new CursorContainer());
        }

        class SampleGame : LargeContainer
        {
            SpriteText text;
            int num = 0;

            public override void Load()
            {
                base.Load();

                Box box;

                Add(box = new Box()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(150, 150),
                    Colour = Color4.Tomato
                });

                Add(text = new SpriteText()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });

                box.RotateTo(360 * 10, 60000);
            }

            protected override void Update()
            {
                base.Update();
                text.Text = $@"frame count: {num++}";
            }
        }
    }
}
