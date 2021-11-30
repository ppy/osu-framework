// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Android;
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

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneScreenOrientation : TestScene
    {
        private AndroidOrientationManager manager;

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
        private TestGameActivity gameActivity { get; set; }
        private Bindable<ScreenOrientation> orientationBindable;

        private PM.ScreenOrientation originalOrientation;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window;
            if (window == null) return;

            originalOrientation = gameActivity.RequestedOrientation;
            manager = new AndroidOrientationManager(config, gameActivity);

            orientationBindable = config.GetBindable<ScreenOrientation>(FrameworkSetting.ScreenOrientation);
            orientationBindable.BindValueChanged(value =>
            {
                currentOrientationSetting.Text = "Current orientation setting: " + value.NewValue.ToString();
                currentScreenOrientation.Text = "Current screen orientation: " + manager.SettingToNativeOrientation(value.NewValue);
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

            ScreenOrientation[] testOrientations = {
                ScreenOrientation.AnyPortrait,
                ScreenOrientation.AnyLandscape,
                ScreenOrientation.Auto
            };

            foreach (var orientation in testOrientations)
            {
                AddStep("Change setting to " + orientation.ToString(), () =>
                {
                    orientationBindable.Value = orientation;
                });
                AddAssert("Test if current screen is " + orientation.ToString(), () =>
                {
                    var nativeOrientation = manager.SettingToNativeOrientation(orientation);
                    return gameActivity.RequestedOrientation == nativeOrientation;
                });
            }
        }

        [Test]
        public void TestHardLockedOrientation()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            ScreenOrientation[] testOrientations = {
                ScreenOrientation.Portrait,
                ScreenOrientation.ReversePortrait,
                ScreenOrientation.LandscapeLeft,
                ScreenOrientation.LandscapeRight,
            };

            foreach (var orientation in testOrientations)
            {
                AddStep("Change setting to " + orientation.ToString(), () =>
                {
                    orientationBindable.Value = orientation;
                });
                AddAssert("Test if current screen is " + orientation.ToString(), () =>
                {
                    var nativeOrientation = manager.SettingToNativeOrientation(orientation);
                    if (gameActivity.RequestedOrientation != nativeOrientation)
                        return false;
                    return true;
                });
            }
            AddStep("Change orientation back to original", () =>
            {
                gameActivity.RequestedOrientation = originalOrientation;
            });
        }

        [Test]
        public void TestSensorOrientation()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }
            AddStep("Change setting to Any", () =>
            {
                orientationBindable.Value = ScreenOrientation.Any;
            });
            AddAssert("Test if current screen is Any", () =>
            {
                var nativeOrientation = manager.SettingToNativeOrientation(ScreenOrientation.Any);
                if (gameActivity.RequestedOrientation != nativeOrientation)
                    return false;
                return true;
            });
            AddWaitStep("Try rotating with auto-rotate locked", 20);
            AddStep("Change orientation back to original", () =>
            {
                gameActivity.RequestedOrientation = originalOrientation;
            });
        }

        [Test]
        public void TestLock()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            gameActivity.RequestedOrientation = PM.ScreenOrientation.Locked;
            var a = gameActivity.RequestedOrientation;

            AddStep("Unlock and change setting to Any", () =>
            {
                manager.SetOrientationLock(false);
                orientationBindable.Value = ScreenOrientation.Any;
            });
            AddAssert("Test if current screen is Any", () =>
            {
                var nativeOrientation = manager.SettingToNativeOrientation(ScreenOrientation.Any);
                if (gameActivity.RequestedOrientation != nativeOrientation)
                    return false;
                return true;
            });
            AddStep("Lock orientation", () =>
            {
                manager.SetOrientationLock(true);
            });
            AddAssert("Test if orientation is locked", () =>
            {
                if (gameActivity.RequestedOrientation != PM.ScreenOrientation.Locked)
                    return false;
                return true;
            });
            AddStep("Change setting while locked", () =>
            {
                orientationBindable.Value = ScreenOrientation.LandscapeRight;
            });
            AddAssert("Test if orientation is still locked", () =>
            {
                if (gameActivity.RequestedOrientation != PM.ScreenOrientation.Locked)
                    return false;
                return true;
            });
            AddAssert("Test if setting has changed", () =>
            {
                return orientationBindable.Value == ScreenOrientation.LandscapeRight;
            });
            AddStep("Unlock orientation", () =>
            {
                manager.SetOrientationLock(false);
            });
            AddAssert("Test if orientation is back to setting", () =>
            {
                var nativeOrientation = manager.SettingToNativeOrientation(orientationBindable.Value);
                if (gameActivity.RequestedOrientation != nativeOrientation)
                    return false;
                return true;
            });
            AddStep("Change orientation back to original", () =>
            {
                gameActivity.RequestedOrientation = originalOrientation;
            });
        }
    }
}
