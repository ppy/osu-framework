// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    internal class TestBrowserConfig : IniConfigManager<TestBrowserSetting>
    {
        protected override string Filename => @"visualtests.cfg";

        public TestBrowserConfig(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();
            SetDefault(TestBrowserSetting.LastTest, string.Empty);
        }
    }

    internal enum TestBrowserSetting
    {
        LastTest,
    }
}
