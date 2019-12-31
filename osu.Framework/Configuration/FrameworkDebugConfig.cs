﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Configuration
{
    public class FrameworkDebugConfigManager : IniConfigManager<DebugSetting>
    {
        protected override string Filename => null;

        public FrameworkDebugConfigManager()
            : base(null)
        {
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            Set(DebugSetting.BypassFrontToBackPass, false);
            Set(DebugSetting.PerformanceLogging, false);
        }
    }

    public enum DebugSetting
    {
        BypassFrontToBackPass,
        PerformanceLogging
    }
}
