//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.VisualTests.Tests
{
    class TestCasePlayer : TestCase
    {
        internal override string Name => @"Player";
        internal override string Description => @"Play osu! (maybe?!)";

        Player p;

        internal override void Reset()
        {
            base.Reset();

            try
            {
                BeatmapManager.Initialize();
                BeatmapManager.Load(BeatmapManager.Beatmaps[0]);

                Add(p = new Player(0));
                p.Initialize();
            }
            catch
            {
                Debug.Print("No beatmaps available for this test!");
                return;
            }
        }
    }
}
