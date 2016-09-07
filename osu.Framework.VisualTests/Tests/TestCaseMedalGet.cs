//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.VisualTests.Tests
{
    class TestCaseMedalGet : TestCase
    {
        internal override string Name => @"Medals";
        internal override string Description => @"Explore receiving medals.";

        internal override void Reset()
        {
            base.Reset();

            AddButton(@"Give me a medal!", awardMedal);
        }

        private void awardMedal()
        {
            Medal medal = new Medal(@"all-secret-bunny", @"Don't let the bunny distract you!", @"The order was indeed, not a rabbit.");
            MedalPopup popup = new MedalPopup(medal);
            Game.ShowDialog(popup);
        }
    }
}
