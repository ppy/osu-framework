// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    public class TestBrowserTestRunner : CompositeDrawable
    {
        private const double time_between_tests = 200;

        private Bindable<double> volume;
        private double volumeAtStartup;

        public TestBrowserTestRunner(TestBrowser browser)
        {
            this.browser = browser;
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            volume = config.GetBindable<double>(FrameworkSetting.VolumeUniversal);
            volumeAtStartup = volume.Value;
            volume.Value = 0;
        }

        protected override void Dispose(bool isDisposing)
        {
            volume.Value = volumeAtStartup;
            base.Dispose(isDisposing);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddInternal(browser);

            Logger.Log($@"Running {browser.TestTypes.Count} visual test cases...");

            runNext();
        }

        private int testIndex;

        private Type loadableTestType => testIndex >= 0 ? browser.TestTypes.ElementAtOrDefault(testIndex) : null;

        private readonly TestBrowser browser;

        [Resolved]
        private GameHost host { get; set; }

        private void runNext()
        {
            if (loadableTestType == null)
            {
                //we're done
                Scheduler.AddDelayed(host.Exit, time_between_tests);
                return;
            }

            if (browser.CurrentTest?.GetType() != loadableTestType)
            {
                browser.LoadTest(loadableTestType, () =>
                {
                    testIndex++;
                    Scheduler.AddDelayed(runNext, time_between_tests);
                });
            }
        }
    }
}
