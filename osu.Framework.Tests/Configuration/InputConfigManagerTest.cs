// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
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
                host.Run(new TestGame((h, _) => sensitivity = h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value));
            }

            Assert.AreEqual(5, sensitivity);
        }

        [Test]
        public void TestNewConfigPersists()
        {
            using (var host = new TestHeadlessGameHost(bypassCleanup: true))
            {
                host.Run(new TestGame((h, _) =>
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
                host.Run(new TestGame((h, _) => sensitivity = h.AvailableInputHandlers.OfType<MouseHandler>().First().Sensitivity.Value));
            }

            Assert.AreEqual(5, sensitivity);
        }

        /// <summary>
        /// Verifies that <see cref="InputConfigManager"/> saves when any public-facing bindables of <see cref="InputHandler"/>s are changed.
        /// </summary>
        [Test]
        public void TestChangingSettingsSaves()
        {
            var handler = new MouseHandler();
            var config = new TestInputConfigManager(new[] { handler });

            handler.Sensitivity.Value = 5;
            Assert.IsTrue(config.SaveEvent.WaitOne(10000)); // wait for QueueBackgroundSave() debounce.
            Assert.AreEqual(1, config.TimesSaved);

            handler.Enabled.Value = !handler.Enabled.Value;
            Assert.IsTrue(config.SaveEvent.WaitOne(10000));
            Assert.AreEqual(2, config.TimesSaved);

            handler.Reset();
            Assert.IsTrue(config.SaveEvent.WaitOne(10000));
            Assert.AreEqual(3, config.TimesSaved);

            for (int i = 0; i < 10; i++)
            {
                handler.Sensitivity.Value += 0.1;
                Assert.AreEqual(3, config.TimesSaved);
            }

            Assert.IsTrue(config.SaveEvent.WaitOne(10000));
            Assert.AreEqual(4, config.TimesSaved);
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

        private class TestInputConfigManager : InputConfigManager
        {
            public int TimesSaved;

            public readonly AutoResetEvent SaveEvent = new AutoResetEvent(false);

            public TestInputConfigManager(IReadOnlyList<InputHandler> inputHandlers)
                : base(null!, inputHandlers)
            {
            }

            protected override void PerformLoad()
            {
            }

            protected override bool PerformSave()
            {
                TimesSaved++;
                SaveEvent.Set();
                return true;
            }
        }
    }
}
