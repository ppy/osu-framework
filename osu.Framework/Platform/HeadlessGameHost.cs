// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Timing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// A GameHost which doesn't require a graphical or sound device.
    /// </summary>
    public class HeadlessGameHost : DesktopGameHost
    {
        public const double CLOCK_RATE = 1000.0 / 30;

        private readonly bool realtime;
        private IFrameBasedClock customClock;

        protected override IFrameBasedClock SceneGraphClock => customClock ?? base.SceneGraphClock;

        public override bool OpenFileExternally(string filename)
        {
            Logger.Log($"Application has requested file \"{filename}\" to be opened.");
            return true;
        }

        public override bool PresentFileExternally(string filename)
        {
            Logger.Log($"Application has requested file \"{filename}\" to be shown.");
            return true;
        }

        public override void OpenUrlExternally(string url) => Logger.Log($"Application has requested URL \"{url}\" to be opened.");

        public override IEnumerable<string> UserStoragePaths => new[] { "./headless/" };

        [Obsolete("Use HeadlessGameHost(HostOptions, bool) instead.")] // Can be removed 20220715
        public HeadlessGameHost(string gameName, bool bindIPC = false, bool realtime = true, bool portableInstallation = false)
            : this(gameName, new HostOptions
            {
                BindIPC = bindIPC,
                PortableInstallation = portableInstallation,
            }, realtime)
        {
        }

        public HeadlessGameHost(string gameName = null, HostOptions options = null, bool realtime = true)
            : base(gameName ?? Guid.NewGuid().ToString(), options)
        {
            this.realtime = realtime;
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            defaultOverrides[FrameworkSetting.AudioDevice] = "No sound";

            base.SetupConfig(defaultOverrides);

            if (Enum.TryParse<ExecutionMode>(Environment.GetEnvironmentVariable("OSU_EXECUTION_MODE"), out var mode))
            {
                Config.SetValue(FrameworkSetting.ExecutionMode, mode);
                Logger.Log($"Startup execution mode set to {mode} from envvar");
            }
        }

        protected override void SetupForRun()
        {
            base.SetupForRun();

            // We want the draw thread to run, but it doesn't matter how fast it runs.
            // This limiting is mostly to reduce CPU overhead.
            MaximumDrawHz = 60;

            if (!realtime)
            {
                customClock = new FramedClock(new FastClock(CLOCK_RATE));

                // time is incremented per frame, rather than based on the real-world time.
                // therefore our goal is to run frames as fast as possible.
                MaximumUpdateHz = MaximumInactiveHz = 0;
            }
            else
            {
                // in realtime runs, set a sane upper limit to avoid cpu overhead from spinning.
                MaximumUpdateHz = MaximumInactiveHz = 1000;
            }
        }

        protected override void DrawFrame()
        {
            //we can't draw.
        }

        protected override void UpdateFrame()
        {
            customClock?.ProcessFrame();

            base.UpdateFrame();
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() => Array.Empty<InputHandler>();

        private class FastClock : IClock
        {
            private readonly double increment;
            private double time;

            /// <summary>
            /// A clock which increments each time <see cref="CurrentTime"/> is requested.
            /// Run fast. Run consistent.
            /// </summary>
            /// <param name="increment">Milliseconds we should increment the clock by each time the time is requested.</param>
            public FastClock(double increment)
            {
                this.increment = increment;
            }

            public double CurrentTime => time += increment;
            public double Rate => 1;
            public bool IsRunning => true;
        }
    }
}
