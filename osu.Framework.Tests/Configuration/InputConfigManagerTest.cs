// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Configuration
{
    [TestFixture]
    public class InputConfigManagerTest
    {
        private Storage storage;

        [Test]
        public void TestOldConfigPersists()
        {
            using (var host = new TestHeadlessGameHost(bypassCleanup: true))
            {
                host.Run(new TestGame((h, config) =>
                {
                    storage = h.Storage;
#pragma warning disable 618
                    config.SetValue(FrameworkSetting.CursorSensitivity, 5.0);
#pragma warning restore 618
                }));
            }

            // test with only FrameworkConfigManager configuration file present
            storage.Delete(InputConfigManager.FILENAME);

            double sensitivity = 0;

            using (var host = new TestHeadlessGameHost())
            {
                host.Run(new TestGame((h, config) => sensitivity = h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value));
            }

            Assert.AreEqual(5, sensitivity);
        }

        [Test]
        public void TestNewConfigPersists()
        {
            using (var host = new TestHeadlessGameHost(bypassCleanup: true))
            {
                host.Run(new TestGame((h, config) =>
                {
                    storage = h.Storage;
                    h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value = 5;
                }));
            }

            // test with only InputConfigManager configuration file present
            storage.Delete(FrameworkConfigManager.FILENAME);

            double sensitivity = 0;

            using (var host = new TestHeadlessGameHost())
            {
                host.Run(new TestGame((h, config) => sensitivity = h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value));
            }

            Assert.AreEqual(5, sensitivity);
        }

        [Test]
        public void TestModifyingSettingsSavesToDisk()
        {
            string beforeChange = null;
            string afterSensitivityChange = null;
            string afterEnabledChange = null;
            string afterExit;

            using (var host = new TestHeadlessGameHost(bypassCleanup: true))
            {
                host.Run(new TestGame((h, config) =>
                {
                    storage = h.Storage;

                    // let the InputConfigManager save and close the file
                    Thread.Sleep(100);

                    using (var stream = storage.GetStream(InputConfigManager.FILENAME))
                    using (var sr = new StreamReader(stream))
                    {
                        beforeChange = sr.ReadToEnd();
                    }

                    h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value = 5;

                    // let the InputConfigManager QueueBackgroundSave() debounce
                    Thread.Sleep(200);

                    using (var stream = storage.GetStream(InputConfigManager.FILENAME))
                    using (var sr = new StreamReader(stream))
                    {
                        afterSensitivityChange = sr.ReadToEnd();
                    }

                    h.AvailableInputHandlers.OfType<MouseHandler>().First().Enabled.Value = true;

                    // let the InputConfigManager QueueBackgroundSave() debounce
                    Thread.Sleep(200);

                    using (var stream = storage.GetStream(InputConfigManager.FILENAME))
                    using (var sr = new StreamReader(stream))
                    {
                        afterEnabledChange = sr.ReadToEnd();
                    }
                }));
            }

            using (var stream = storage.GetStream(InputConfigManager.FILENAME))
            using (var sr = new StreamReader(stream))
            {
                afterExit = sr.ReadToEnd();
            }

            // delete the files so the test can have a clean slate next time it's run.
            storage.Delete(FrameworkConfigManager.FILENAME);
            storage.Delete(InputConfigManager.FILENAME);

            // the handler is disabled by default because it failed to initialize (no SDL2DesktopWindow to bind events)
            assertConfigMatches(beforeChange, "1.0", "false");
            assertConfigMatches(afterSensitivityChange, "5.0", "false");
            assertConfigMatches(afterEnabledChange, "5.0", "true");
            assertConfigMatches(afterExit, "5.0", "true");
        }

        private void assertConfigMatches(string file, string sensitivity, string enabled)
        {
            Assert.True(file.Contains($"\"Sensitivity\":{sensitivity}"));
            Assert.True(file.Contains($"\"Enabled\":{enabled}"));
        }

        public class TestHeadlessGameHost : TestRunHeadlessGameHost
        {
            public TestHeadlessGameHost([CallerMemberName] string caller = "", bool bypassCleanup = false)
                : base(caller, new HostOptions(), bypassCleanup: bypassCleanup)
            {
            }

            protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() => new[]
            {
                new MouseHandler()
            };
        }

        private class TestGame : Game
        {
            private readonly Action<GameHost, FrameworkConfigManager> action;

            [Resolved]
            private FrameworkConfigManager config { get; set; }

            public TestGame(Action<GameHost, FrameworkConfigManager> action)
            {
                this.action = action;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                action(Host, config);
            }

            protected override void Update()
            {
                base.Update();
                Exit();
            }
        }
    }
}
