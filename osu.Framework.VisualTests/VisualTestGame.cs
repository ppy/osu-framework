//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Graphics.Cursor;
using osu.Framework.VisualTests.Tests;

namespace osu.Framework.VisualTests
{
    class VisualTestGame : Game
    {
        public override void Load()
        {
            base.Load();

            Add(new FieldTest());

            ShowPerformanceOverlay = true;
            Add(new CursorContainer());
        }
    }
}
