// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test does not cover correct GL context acquire/release when run headless.")]
    public class TestSceneExecutionModes : FrameworkTestScene
    {
        private Bindable<ExecutionMode> executionMode;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager configManager)
        {
            executionMode = configManager.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);
        }

        [Test]
        public void ToggleModeSmokeTest()
        {
            AddRepeatStep("toggle execution mode", () => executionMode.Value = executionMode.Value == ExecutionMode.MultiThreaded
                ? ExecutionMode.SingleThread
                : ExecutionMode.MultiThreaded, 2);
        }

        [Test]
        public void TestRapidSwitching()
        {
            ScheduledDelegate switchStep = null;
            int switchCount = 0;

            AddStep("install quick switch step", () =>
            {
                switchStep = Scheduler.AddDelayed(() =>
                {
                    executionMode.Value = executionMode.Value == ExecutionMode.MultiThreaded ? ExecutionMode.SingleThread : ExecutionMode.MultiThreaded;
                    switchCount++;
                }, 0, true);
            });

            AddUntilStep("switch count sufficiently high", () => switchCount > 1000);

            AddStep("remove", () => switchStep.Cancel());
        }
    }
}
