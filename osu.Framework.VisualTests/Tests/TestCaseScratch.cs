//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.VisualTests.Tests
{
    class TestCaseScratch : TestCase
    {
        private Score score;
        internal override string Name => @"Scratch";

        internal override string Description => @"Make cool stuff here";

        internal override int DisplayOrder => -1;

        internal override void Reset()
        {
            base.Reset();
        }
    }
}
