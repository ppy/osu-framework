// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Testing;

namespace osu.Framework.Tests
{
    internal class AutomatedVisualTestGame : TestGame
    {
        public AutomatedVisualTestGame()
        {
            Add(new TestBrowserTestRunner(new TestBrowser()));
        }
    }
}
