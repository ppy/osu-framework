//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.VisualTests
{
    class VisualTestGame : Game
    {
        public override void Load()
        {
            base.Load();
            Host.Size = new Vector2(1366, 768);
        }
    }
}
