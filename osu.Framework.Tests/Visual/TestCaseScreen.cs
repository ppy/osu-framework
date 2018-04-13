// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Screens;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseScreen : TestCase
    {
        private Screen baseScreen;

        [SetUp]
        public new void SetupTest()
        {
            Clear();
            Add(baseScreen = new TestScreen());
        }

        [Test]
        public void TestPushPop()
        {
            TestScreen screen1 = null, screen2 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());

            // we don't support pushing a screen that has been entered
            AddStep("bad push", () => Assert.Throws(typeof(InvalidOperationException), () => screen1.Push(screen1)));

            pushAndEnsureCurrent(() => screen2 = new TestScreen(), () => screen1);

            AddAssert("ensure child", () => screen1.ChildScreen != null);

            AddStep("pop", () => screen2.Exit());

            AddAssert("ensure child gone", () => screen1.ChildScreen == null);
            AddAssert("ensure not current", () => !screen2.IsCurrentScreen);

            // can't push an exited screen
            AddStep("bad push", () => Assert.Throws(typeof(InvalidOperationException), () => screen1.Push(screen2)));

            AddStep("pop", () => screen1.Exit());
        }

        [Test]
        public void TestMultiLevelExit()
        {
            TestScreen screen1 = null, screen2 = null, screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            pushAndEnsureCurrent(() => screen2 = new TestScreen(), () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(), () => screen2);

            // can't push an exited screen
            AddStep("bad exit", () => Assert.Throws(typeof(InvalidOperationException), () => screen1.Exit()));

            AddStep("make current", () => screen1.MakeCurrent());

            AddAssert("ensure child gone", () => screen1.ChildScreen == null);
            AddAssert("ensure current", () => screen1.IsCurrentScreen);

            AddAssert("ensure not current", () => !screen2.IsCurrentScreen);
            AddAssert("ensure not current", () => !screen3.IsCurrentScreen);
        }

        [Test]
        public void TestAsyncPush()
        {
            TestScreen screen1 = null;

            AddStep("push slow", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddAssert("ensure not current", () => !screen1.IsCurrentScreen);
            AddWaitStep(1);
            AddUntilStep(() => screen1.IsCurrentScreen, "ensure current");
        }

        [Test]
        public void TestAsyncPreloadPush()
        {
            TestScreen screen1 = null;
            AddStep("preload slow", () => LoadComponentAsync(screen1 = new TestScreenSlow()));
            pushAndEnsureCurrent(() => screen1);
        }

        private void pushAndEnsureCurrent(Func<Screen> screenCtor, Func<Screen> target = null)
        {
            Screen screen = null;
            AddStep("push", () => (target?.Invoke() ?? baseScreen).Push(screen = screenCtor()));
            AddUntilStep(() => screen.IsCurrentScreen, "ensure current");
        }

        private class TestScreenSlow : TestScreen
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                Thread.Sleep((int)(500 / Clock.Rate));
            }
        }

        private class TestScreen : Screen
        {
            public static int Sequence;
            private Button popButton;

            private const int transition_time = 500;

            protected override void OnEntering(Screen last)
            {
                if (last != null)
                {
                    //only show the pop button if we are entered form another screen.
                    popButton.Alpha = 1;
                }

                Content.MoveTo(new Vector2(0, -DrawSize.Y));
                Content.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
            }

            protected override bool OnExiting(Screen next)
            {
                Content.MoveTo(new Vector2(0, -DrawSize.Y), transition_time, Easing.OutQuint);
                return base.OnExiting(next);
            }

            protected override void OnSuspending(Screen next)
            {
                Content.MoveTo(new Vector2(0, DrawSize.Y), transition_time, Easing.OutQuint);
            }

            protected override void OnResuming(Screen last)
            {
                Content.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = new Color4(
                            Math.Max(0.5f, RNG.NextSingle()),
                            Math.Max(0.5f, RNG.NextSingle()),
                            Math.Max(0.5f, RNG.NextSingle()),
                            1),
                    },
                    new SpriteText
                    {
                        Text = $@"Screen {Sequence++}",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        TextSize = 50,
                    },
                    popButton = new Button
                    {
                        Text = @"Pop",
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.1f),
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        BackgroundColour = Color4.Red,
                        Alpha = 0,
                        Action = Exit
                    },
                    new Button
                    {
                        Text = @"Push",
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.1f),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        BackgroundColour = Color4.YellowGreen,
                        Action = delegate
                        {
                            Push(new TestScreen
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                            });
                        }
                    }
                };
            }
        }
    }
}
