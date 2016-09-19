//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.VisualTests.Tests;

namespace osu.Framework.VisualTests
{
    class Benchmark : Game
    {
        public override void Load()
        {
            base.Load();

            Host.MaximumDrawHz = int.MaxValue;
            Host.MaximumUpdateHz = int.MaxValue;

            FieldTest f = new FieldTest();
            Add(f);

            for (int i = 1; i < f.TestCount; i++)
            {
                int loadableCase = i;
                Scheduler.AddDelayed(delegate { f.LoadTest(loadableCase); }, loadableCase * 1000);
            }

            Scheduler.AddDelayed(Exit, f.TestCount * 1000);
        }
    }
}