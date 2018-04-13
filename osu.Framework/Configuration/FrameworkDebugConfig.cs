// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime;
using osu.Framework.Caching;

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

            Set(DebugSetting.ActiveGCMode, GCLatencyMode.SustainedLowLatency);
            Set(DebugSetting.BypassCaching, false).ValueChanged += delegate { StaticCached.BypassCache = Get<bool>(DebugSetting.BypassCaching); };
        }
    }

    public enum DebugSetting
    {
        ActiveGCMode,
        BypassCaching
    }
}
