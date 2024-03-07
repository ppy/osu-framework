// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneDebugUtils : FrameworkTestScene
    {
        private readonly TextFlowContainer textFlow;

        private bool isHeadlessTestRun;

        public TestSceneDebugUtils()
        {
            Child = textFlow = new TextFlowContainer
            {
                RelativeSizeAxes = Axes.Both
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            isHeadlessTestRun = host.Window == null;
        }

        [Test]
        public void LogStatics()
        {
            AddStep("log DebugUtils statics", () =>
            {
                textFlow.Clear();
                log(DebugUtils.IsNUnitRunning);
                log(DebugUtils.IsDebugBuild);
                log(RuntimeInfo.EntryAssembly);
#pragma warning disable RS0030
                log(Assembly.GetEntryAssembly());
#pragma warning restore RS0030
            });
        }

        [Test]
        public void TestIsNUnitRunning()
        {
            AddAssert("check IsNUnitRunning", () => DebugUtils.IsNUnitRunning, () => Is.EqualTo(isHeadlessTestRun));
        }

        [Test]
        public void TestIsDebugBuild()
        {
            AddAssert("check IsDebugBuild", () => DebugUtils.IsDebugBuild, () => Is.EqualTo(
#if DEBUG
                true
#else
                false
#endif
            ));
        }

        [Test]
        public void TestEntryAssembly()
        {
            AddAssert("check RuntimeInfo.EntryAssembly", () => RuntimeInfo.EntryAssembly.FullName, () => Does.StartWith("osu.Framework.Tests"));
        }

        /// <summary>
        /// Logs the <paramref name="name"/> and <paramref name="value"/> on the screen and in the logs.
        /// </summary>
        private void log<T>(T value, [CallerArgumentExpression("value")] string? name = null)
        {
            string text = $"{name}: {value}";
            textFlow.AddParagraph(text);
            Logger.Log(text);
        }
    }
}
