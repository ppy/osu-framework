// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class FrameworkDebugConfigManager : ConfigManager<FrameworkDebugConfig>
    {
        public override string Filename => null;

        public FrameworkDebugConfigManager()
            : base(null)
        {
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            Set(FrameworkDebugConfig.ActiveGCMode, GCLatencyMode.SustainedLowLatency);
        }
    }

    public enum FrameworkDebugConfig
    {
        ActiveGCMode,
    }
}
