//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Game.Configuration
{
    public class FrameworkConfigManager : ConfigManager<FrameworkConfig>
    {
        public override string Filename => @"framework.ini";

        protected override void InitialiseDefaults()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            Set(FrameworkConfig.ShowLogOverlay, true);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        public FrameworkConfigManager(BasicStorage storage) : base(storage)
        {
        }
    }

    public enum FrameworkConfig
    {
        ShowLogOverlay
    }
}
