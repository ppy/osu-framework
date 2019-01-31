// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseScreen : TestCase
    {
        private TestScreen baseScreen;

        [SetUp]
        public new void SetupTest() => Schedule(() =>
        {
            Clear();
            Add(new ScreenStack(baseScreen = new TestScreen())
            {
                RelativeSizeAxes = Axes.Both
            });
        });

        [Test]
        public void TestPushPop()
        {
            TestScreen screen1 = null, screen2 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());

            AddAssert("baseScreen suspended to screen1", () => baseScreen.SuspendedTo == screen1);
            AddAssert("screen1 entered from baseScreen", () => screen1.EnteredFrom == baseScreen);

            // we don't support pushing a screen that has been entered
            AddStep("bad push", () => Assert.Throws(typeof(ScreenStack.ScreenAlreadyEnteredException), () => screen1.Push(screen1)));

            pushAndEnsureCurrent(() => screen2 = new TestScreen(), () => screen1);

            AddAssert("screen1 suspended to screen2", () => screen1.SuspendedTo == screen2);
            AddAssert("screen2 entered from screen1", () => screen2.EnteredFrom == screen1);

            AddAssert("ensure child", () => screen1.GetChildScreen() != null);

            AddStep("pop", () => screen2.Exit());

            AddAssert("screen1 resumed from screen2", () => screen1.ResumedFrom == screen2);
            AddAssert("screen2 exited to screen1", () => screen2.ExitedTo == screen1);
            AddAssert("screen2 has lifetime end", () => screen2.LifetimeEnd != double.MaxValue);

            AddAssert("ensure child gone", () => screen1.GetChildScreen() == null);
            AddAssert("ensure not current", () => !screen2.IsCurrentScreen());

            AddStep("pop", () => screen1.Exit());

            AddAssert("baseScreen resumed from screen1", () => baseScreen.ResumedFrom == screen1);
            AddAssert("screen1 exited to baseScreen", () => screen1.ExitedTo == baseScreen);
            AddAssert("screen1 has lifetime end", () => screen1.LifetimeEnd != double.MaxValue);
            AddUntilStep(() => screen1.Parent == null, "screen1 is removed");
        }

        [Test]
        public void TestMultiLevelExit()
        {
            TestScreen screen1 = null, screen2 = null, screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            pushAndEnsureCurrent(() => screen2 = new TestScreen { ValidForResume = false }, () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(), () => screen2);

            AddStep("bad exit", () => Assert.Throws(typeof(ScreenStack.ScreenHasChildException), () => screen1.Exit()));
            AddStep("exit", () => screen3.Exit());

            AddAssert("screen3 exited to screen2", () => screen3.ExitedTo == screen2);
            AddAssert("screen2 not resumed from screen3", () => screen2.ResumedFrom == null);
            AddAssert("screen2 exited to screen1", () => screen2.ExitedTo == screen1);
            AddAssert("screen1 resumed from screen2", () => screen1.ResumedFrom == screen2);

            AddAssert("screen3 has lifetime end", () => screen3.LifetimeEnd != double.MaxValue);
            AddAssert("screen2 has lifetime end", () => screen2.LifetimeEnd != double.MaxValue);
            AddAssert("screens 2 & 3 share lifetime end", () => screen2.LifetimeEnd == screen3.LifetimeEnd);

            AddAssert("ensure child gone", () => screen1.GetChildScreen() == null);
            AddAssert("ensure current", () => screen1.IsCurrentScreen());

            AddAssert("ensure not current", () => !screen2.IsCurrentScreen());
            AddAssert("ensure not current", () => !screen3.IsCurrentScreen());
        }

        [Test]
        public void TestAsyncPush()
        {
            TestScreen screen1 = null;

            AddStep("push slow", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddAssert("ensure current", () => !screen1.IsCurrentScreen());
            AddWaitStep(1);
            AddUntilStep(() => screen1.IsCurrentScreen(), "ensure current");
        }

        [Test]
        public void TestAsyncPreloadPush()
        {
            TestScreen screen1 = null;
            AddStep("preload slow", () => LoadComponentAsync(screen1 = new TestScreenSlow()));
            pushAndEnsureCurrent(() => screen1);
        }

        [Test]
        public void TestExitBeforePush()
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;

            AddStep("push slow", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddStep("exit slow", () => screen1.Exit());
            AddUntilStep(() => screen1.LoadState >= LoadState.Ready, "wait for screen to load");
            AddAssert("ensure not current", () => !screen1.IsCurrentScreen());
            AddAssert("ensure base still current", () => baseScreen.IsCurrentScreen());
            AddStep("push fast", () => baseScreen.Push(screen2 = new TestScreen()));
            AddUntilStep(() => screen2.IsCurrentScreen(), "ensure new current");
        }

        [Test]
        public void TestMakeCurrent()
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;
            TestScreen screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            pushAndEnsureCurrent(() => screen2 = new TestScreen(), () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(), () => screen2);

            AddStep("block exit", () => screen3.Exiting = () => true);
            AddStep("make screen 1 current", () => screen1.MakeCurrent());
            AddAssert("screen 3 still current", () => screen3.IsCurrentScreen());
            AddAssert("screen 3 doesn't have lifetime end", () => screen3.LifetimeEnd == double.MaxValue);
            AddAssert("screen 2 valid for resume", () => screen2.ValidForResume);
            AddAssert("screen 1 valid for resume", () => screen1.ValidForResume);

            AddStep("don't block exit", () => screen3.Exiting = () => false);
            AddStep("make screen 1 current", () => screen1.MakeCurrent());
            AddAssert("screen 1 current", () => screen1.IsCurrentScreen());
            AddAssert("screen 1 doesn't have lifetime end", () => screen1.LifetimeEnd == double.MaxValue);
            AddAssert("screen 3 has lifetime end", () => screen3.LifetimeEnd != double.MaxValue);
            AddAssert("screen 2 & 3 share lifetime end", () => screen2.LifetimeEnd == screen3.LifetimeEnd);
        }

        private void pushAndEnsureCurrent(Func<IScreen> screenCtor, Func<IScreen> target = null)
        {
            IScreen screen = null;
            AddStep("push", () => (target?.Invoke() ?? baseScreen).Push(screen = screenCtor()));
            AddUntilStep(() => screen.IsCurrentScreen(), "ensure current");
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
            public Func<bool> Exiting;

            public IScreen EnteredFrom;
            public IScreen ExitedTo;

            public IScreen SuspendedTo;
            public IScreen ResumedFrom;

            public static int Sequence;
            private Button popButton;

            private const int transition_time = 500;

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
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
                        Action = this.Exit
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
                            this.Push(new TestScreen
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                            });
                        }
                    }
                };
            }

            public override void OnEntering(IScreen last)
            {
                EnteredFrom = last;

                base.OnEntering(last);

                if (last != null)
                {
                    //only show the pop button if we are entered form another screen.
                    popButton.Alpha = 1;
                }

                this.MoveTo(new Vector2(0, -DrawSize.Y));
                this.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
            }

            public override bool OnExiting(IScreen next)
            {
                ExitedTo = next;

                if (Exiting?.Invoke() == true)
                    return true;

                this.MoveTo(new Vector2(0, -DrawSize.Y), transition_time, Easing.OutQuint);
                return base.OnExiting(next);
            }

            public override void OnSuspending(IScreen next)
            {
                SuspendedTo = next;

                base.OnSuspending(next);
                this.MoveTo(new Vector2(0, DrawSize.Y), transition_time, Easing.OutQuint);
            }

            public override void OnResuming(IScreen last)
            {
                ResumedFrom = last;

                base.OnResuming(last);
                this.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
            }
        }
    }
}
