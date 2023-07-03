// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot run in headless mode (a window instance is required).")]
    [System.ComponentModel.Description($"Checks that {nameof(IWindow.Resized)} behaves correctly when {nameof(IWindow.WindowMode)} is changed.")]
    public partial class TestSceneWindowModeResizedEvent : FrameworkTestScene
    {
        private static readonly Size windowed_size = new Size(1280, 720);

        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();
        private IWindow window = null!;

        private readonly Queue<Size> resizeInvokes = new Queue<Size>();

        [Resolved]
        private FrameworkConfigManager config { get; set; } = null!;

        public TestSceneWindowModeResizedEvent()
        {
            Child = new WindowDisplaysPreview
            {
                RelativeSizeAxes = Axes.Both
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window!;
            window.Resized += windowResized;
            config.BindWith(FrameworkSetting.WindowMode, windowMode);
        }

        private void windowResized() => resizeInvokes.Enqueue(window.ClientSize);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep($"set windowed size to {windowed_size}", () => config.SetValue(FrameworkSetting.WindowedSize, windowed_size));
            AddStep("set default fullscreen size", () => config.GetBindable<Size>(FrameworkSetting.SizeFullscreen).SetDefault());
        }

        private void setUp(WindowMode startingMode, WindowMode finalMode)
        {
            AddStep($"set mode to {startingMode}", () => windowMode.Value = startingMode);
            AddStep("clear resize queue", () => resizeInvokes.Clear());
            AddStep($"set mode to {finalMode}", () => windowMode.Value = finalMode);
        }

        [TestCase(WindowMode.Borderless, WindowMode.Fullscreen)]
        [TestCase(WindowMode.Fullscreen, WindowMode.Borderless)]
        public void TestFullscreenAndBorderless(WindowMode startingMode, WindowMode finalMode)
        {
            setUp(startingMode, finalMode);
            // since the two window modes take up the entire display, the size of the window shouldn't change.
            AddAssert("resize queue is empty", () => resizeInvokes, () => Is.Empty);
        }

        [TestCase(WindowMode.Windowed, WindowMode.Borderless)]
        [TestCase(WindowMode.Windowed, WindowMode.Fullscreen)]
        [TestCase(WindowMode.Fullscreen, WindowMode.Windowed)]
        [TestCase(WindowMode.Borderless, WindowMode.Windowed)]
        public void TestModeSwitchWindowed([Values] WindowMode startingMode, [Values] WindowMode finalMode)
        {
            setUp(startingMode, finalMode);
            AddAssert("only one resize event", () => resizeInvokes, () => Has.Count.EqualTo(1));
            if (finalMode == WindowMode.Windowed)
                AddAssert("resized to windowed size", () => resizeInvokes.Dequeue(), () => Is.EqualTo(windowed_size));
            else
                AddAssert("resized to display size", () => resizeInvokes.Dequeue(), () => Is.EqualTo(window.CurrentDisplayBindable.Value.Bounds.Size));
        }

        protected override void Dispose(bool isDisposing)
        {
            if (window.IsNotNull())
                window.Resized -= windowResized;

            base.Dispose(isDisposing);
        }
    }
}
