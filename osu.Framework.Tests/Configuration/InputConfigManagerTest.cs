// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
