// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Localisation
{
    [HeadlessTest]
    public partial class ThreadCultureTest : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [Test]
        public void TestDefaultCultureIsSystem()
        {
            setCulture("");
            assertCulture(CultureInfoHelper.SystemCulture.Name);
        }

        [Test]
        public void TestValidCultureIsSelected()
        {
            setCulture("ko-KR");
            assertCulture("ko-KR");
        }

        [Test]
        public void TestInvalidCultureFallsBackToSystem()
        {
            setCulture("ko_KR");
            assertCulture(CultureInfoHelper.SystemCulture.Name);
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
            // force ValueChanged to trigger by calling SetValue explicitly instead of setting .Value
            // this is done since the existing value might have been the same and TestScene.SetUpTestForUnit() might have overridden the culture silently
            locale.SetValue(locale.Value, name);
        });

        private void assertCulture(string name)
        {
            var cultures = new ConcurrentBag<CultureInfo>();

            AddStep("query cultures", () =>
            {
                host.DrawThread.Scheduler.Add(() => cultures.Add(CultureInfo.CurrentCulture));
                host.UpdateThread.Scheduler.Add(() => cultures.Add(CultureInfo.CurrentCulture));
                host.InputThread.Scheduler.Add(() => cultures.Add(CultureInfo.CurrentCulture));
                host.AudioThread.Scheduler.Add(() => cultures.Add(CultureInfo.CurrentCulture));
            });

            AddUntilStep("wait for query", () => cultures.Count == 4);
            AddAssert($"culture is {name}", () => cultures.Select(c => c.Name), () => Is.All.EqualTo(name));
        }

        private void assertThreadCulture(string name)
        {
            CultureInfo culture = null;

            AddStep("start new thread", () => new Thread(() => culture = CultureInfo.CurrentCulture) { IsBackground = true }.Start());
            AddUntilStep("wait for culture", () => culture != null);
            AddAssert($"thread culture is {name}", () => culture.Name == name);
        }
    }
}
