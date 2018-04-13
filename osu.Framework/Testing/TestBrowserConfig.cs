// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    internal class TestBrowserConfig : IniConfigManager<TestBrowserSetting>
    {
        protected override string Filename => @"visualtests.cfg";

        public TestBrowserConfig(Storage storage) : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();
            Set(TestBrowserSetting.LastTest, string.Empty);
        }
    }

    internal enum TestBrowserSetting
    {
        LastTest,
    }
}
