// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.Android;
using osuTK;
using PM = Android.Content.PM;

namespace osu.Framework.Tests.Android.Visual.Platform
{
    public class TestSceneScreenOrientation : TestScene
    {
        private readonly SpriteText currentOrientationSetting = new SpriteText();
        private readonly SpriteText currentScreenOrientation = new SpriteText();
        private readonly Dropdown<ScreenOrientation> orientationDropdown;

        private IWindow window;

        public TestSceneScreenOrientation()
        {
            Child = new FillFlowContainer
            {
                Padding = new MarginPadding(25),
                Spacing = new Vector2(25),
                Children = new Drawable[]
                {
                    currentOrientationSetting,
                    currentScreenOrientation,
                    orientationDropdown = new BasicDropdown<ScreenOrientation>
                    {
                        Width = 600,
                        Items = Enum.GetValues(typeof(ScreenOrientation)).Cast<ScreenOrientation>()
                    }
                }
            };
        }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private TestGameActivity gameActivity { get; set; }

        private Bindable<ScreenOrientation> orientationBindable;

        private ScreenOrientation originalOrientation;

        [BackgroundDependencyLoader]
        private void load()
        {
            window = host.Window;
            if (window == null) return;

            orientationBindable = config.GetBindable<ScreenOrientation>(FrameworkSetting.ScreenOrientation);
            orientationBindable.BindValueChanged(value =>
            {
                currentOrientationSetting.Text = "Current orientation setting: " + value.NewValue.ToString();
                currentScreenOrientation.Text = "Current screen orientation: " + gameActivity.RequestedOrientation;
            });
            orientationDropdown.Current.BindTo(orientationBindable);
        }

        [Test]
        public void TestUserAgnosticOrientation()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            originalOrientation = orientationBindable.Value;

            ScreenOrientation[] testOrientations =
            {
                ScreenOrientation.AnyPortrait,
                ScreenOrientation.AnyLandscape,
                ScreenOrientation.Auto
            };
            PM.ScreenOrientation[] nativeOrientations =
            {
                PM.ScreenOrientation.SensorPortrait,
                PM.ScreenOrientation.SensorLandscape,
                PM.ScreenOrientation.FullUser
            };

            for (int i = 0; i < testOrientations.Length; i++)
            {
                var test = testOrientations[i];
                var native = nativeOrientations[i];
                AddStep("Change setting to " + test.ToString(), () =>
                    orientationBindable.Value = test);
                AddAssert("Test if current screen is " + test.ToString(), () =>
                    gameActivity.RequestedOrientation == native);
            }

            AddStep("Change orientation back to original", () =>
                orientationBindable.Value = originalOrientation);
        }

        [Test]
        public void TestHardLockedOrientation()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            originalOrientation = orientationBindable.Value;

            ScreenOrientation[] testOrientations =
            {
                ScreenOrientation.Portrait,
                ScreenOrientation.ReversePortrait,
                ScreenOrientation.LandscapeLeft,
                ScreenOrientation.LandscapeRight,
            };
            PM.ScreenOrientation[] nativeOrientations =
            {
                PM.ScreenOrientation.Portrait,
                PM.ScreenOrientation.ReversePortrait,
                PM.ScreenOrientation.ReverseLandscape,
                PM.ScreenOrientation.Landscape,
            };

            for (int i = 0; i < testOrientations.Length; i++)
            {
                var test = testOrientations[i];
                var native = nativeOrientations[i];
                AddStep("Change setting to " + test.ToString(), () =>
                    orientationBindable.Value = test);
                AddAssert("Test if current screen is " + test.ToString(), () =>
                    gameActivity.RequestedOrientation == native);
            }

            AddStep("Change orientation back to original", () =>
                orientationBindable.Value = originalOrientation);
        }

        [Test]
        public void TestSensorOrientation()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            originalOrientation = orientationBindable.Value;

            AddStep("Change setting to Any", () =>
                orientationBindable.Value = ScreenOrientation.Any);
            AddAssert("Test if current screen is FullSensor", () =>
                gameActivity.RequestedOrientation == PM.ScreenOrientation.FullSensor);
            AddWaitStep("Try rotating with auto-rotate locked", 10);
            AddStep("Change orientation back to original", () =>
                orientationBindable.Value = originalOrientation);
        }

        [Test]
        public void TestLock()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            originalOrientation = orientationBindable.Value;

            AddStep("Unlock and change setting to Any", () =>
            {
                host.LockScreenOrientation.Value = false;
                orientationBindable.Value = ScreenOrientation.Any;
            });

            AddAssert("Test if current screen is Any", () =>
                gameActivity.RequestedOrientation == PM.ScreenOrientation.FullSensor);

            AddStep("Lock orientation", () =>
                host.LockScreenOrientation.Value = true);

            AddAssert("Test if orientation is locked", () =>
                gameActivity.RequestedOrientation == PM.ScreenOrientation.Locked);

            AddStep("Change setting while locked", () =>
                orientationBindable.Value = ScreenOrientation.LandscapeRight);

            AddAssert("Test if orientation is still locked", () =>
                gameActivity.RequestedOrientation == PM.ScreenOrientation.Locked);

            AddAssert("Test if setting has changed", () =>
                orientationBindable.Value == ScreenOrientation.LandscapeRight);

            AddStep("Unlock orientation", () =>
                host.LockScreenOrientation.Value = false);

            AddAssert("Test if orientation is back to setting", () =>
                gameActivity.RequestedOrientation == PM.ScreenOrientation.Landscape);

            AddStep("Change orientation back to original", () =>
                orientationBindable.Value = originalOrientation);
        }
    }
}
