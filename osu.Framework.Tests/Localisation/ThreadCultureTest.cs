// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class ThreadCultureTest : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private FrameworkConfigManager config { get; set; } = null!;

        [Resolved]
        private LocalisationManager localisation { get; set; } = null!;

        private IDisposable? systemCultureChange;

        private const string system_culture = "en-US";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            AddStep("change system culture", () => systemCultureChange = CultureInfoHelper.ChangeSystemCulture(system_culture));

            AddStep("add locale mappings", () =>
                localisation.AddLocaleMappings(new[]
                {
                    new LocaleMapping(new TestCultureStore("ko-KR")),
                    new LocaleMapping(new TestCultureStore("en"))
                }));
        }

        [Test]
        public void TestDefaultCultureIsSystem()
        {
            setCulture("");
            assertCulture(system_culture);
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
            assertCulture(system_culture);
        }

        [Test]
        public void TestCreateThreadOnNewCulture()
        {
            setCulture("ko-KR");
            assertThreadCulture("ko-KR");
        }

        [Test]
        public void TestMoreSpecificSystemCulture()
        {
            setCulture("en");
            assertCulture(system_culture);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            systemCultureChange?.Dispose();
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
            CultureInfo? culture = null;

            AddStep("start new thread", () => new Thread(() => culture = CultureInfo.CurrentCulture) { IsBackground = true }.Start());
            AddUntilStep("wait for culture", () => culture != null);
            AddAssert($"thread culture is {name}", () => culture!.Name == name);
        }

        private class TestCultureStore : ILocalisationStore
        {
            public CultureInfo UICulture { get; }

            public TestCultureStore(string culture)
            {
                UICulture = new CultureInfo(culture);
            }

            public string? Get(string name) => null;
            public Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
            public Stream? GetStream(string name) => null;
            public IEnumerable<string> GetAvailableResources() => Enumerable.Empty<string>();

            public void Dispose()
            {
            }
        }
    }
}
