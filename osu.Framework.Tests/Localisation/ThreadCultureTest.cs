// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Localisation
{
    [HeadlessTest]
    public class ThreadCultureTest : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [Test]
        public void TestDefaultCultureIsInvariant()
        {
            setCulture("");
            assertCulture("");
        }

        [Test]
        public void TestValidCultureIsSelected()
        {
            setCulture("ko-KR");
            assertCulture("ko-KR");
        }

        [Test]
        public void TestInvalidCultureFallsBackToInvariant()
        {
            setCulture("ko_KR");
            assertCulture("");
        }

        [Test]
        public void TestCreateThreadOnNewCulture()
        {
            setCulture("ko-KR");
            assertThreadCulture("ko-KR");
        }

        private void setCulture(string name) => AddStep($"set culture = {name}", () =>
        {
            var locale = config.GetBindable<string>(FrameworkSetting.Locale);

            locale.Value = name;

            // force ValueChanged to trigger by calling TriggerChange explicitly
            // this is done since the existing value might have been the same and TestScene.SetUpTestForUnit() might have overridden the culture silently
            locale.TriggerChange();
        });

        private void assertCulture(string name)
        {
            var cultures = new ConcurrentBag<CultureInfo>();

            AddStep("query cultures", () =>
            {
                host.DrawThread.Scheduler.Add(() => cultures.Add(Thread.CurrentThread.CurrentCulture));
                host.UpdateThread.Scheduler.Add(() => cultures.Add(Thread.CurrentThread.CurrentCulture));
                host.AudioThread.Scheduler.Add(() => cultures.Add(Thread.CurrentThread.CurrentCulture));
            });

            AddUntilStep("wait for query", () => cultures.Count == 3);
            AddAssert($"culture is {name}", () => cultures.All(c => c.Name == name));
        }

        private void assertThreadCulture(string name)
        {
            CultureInfo culture = null;

            AddStep("start new thread", () => new Thread(() => culture = Thread.CurrentThread.CurrentCulture) { IsBackground = true }.Start());
            AddUntilStep("wait for culture", () => culture != null);
            AddAssert($"thread culture is {name}", () => culture.Name == name);
        }
    }
}
