//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Performance
{
    class PerformanceOverlay : FlowContainer
    {
        public override void Load()
        {
            base.Load();

            Add(new FpsDisplay(@"UPD", Game.Host.UpdateClock));
            Add(new FpsDisplay(@"DRW", Game.Host.DrawClock));

            Direction = FlowDirection.VerticalOnly;
        }
    }
}
