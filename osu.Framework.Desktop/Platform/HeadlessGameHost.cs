// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Statistics;
using OpenTK;
using GLControl = osu.Framework.Platform.GLControl;

namespace osu.Framework.Desktop.Platform
{
    /// <summary>
    /// A GameHost which doesn't require a graphical or sound device.
    /// </summary>
    public class HeadlessGameHost : DesktopGameHost
    {
        public override GLControl GLControl => null;
        public override TextInputSource TextInput => null;

        protected override void DrawFrame()
        {
            //we can't draw.
        }

        public override void Run()
        {
            Size = Vector2.One; //we may be expected to have a non-zero size by components we run.

            while (!ExitRequested)
            {
                UpdateMonitor.NewFrame();

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                {
                    UpdateScheduler.Update();
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Update))
                {
                    UpdateSubTree();
                    using (var buffer = DrawRoots.Get(UsageType.Write))
                        buffer.Object = GenerateDrawNodeSubtree(buffer.Object);
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                {
                    UpdateClock.ProcessFrame();
                }
            }
        }

        public override IEnumerable<InputHandler> GetInputHandlers() => new InputHandler[] { };
    }
}
