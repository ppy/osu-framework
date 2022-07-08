// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneScreenStack : FrameworkTestScene
    {
        private TestScreen baseScreen;
        private ScreenStack stack;

        private readonly List<TestScreenSlow> slowLoaders = new List<TestScreenSlow>();

        [SetUp]
        public void SetupTest() => Schedule(() =>
        {
            Clear();

            Add(stack = new ScreenStack(baseScreen = new TestScreen())
            {
                RelativeSizeAxes = Axes.Both
            });

            stack.ScreenPushed += (_, current) =>
            {
                if (current is TestScreenSlow slow)
                    slowLoaders.Add(slow);
            };
        });

        [TearDownSteps]
        public void Teardown()
        {
            AddStep("unblock any slow loaders", () =>
            {
                foreach (var slow in slowLoaders)
                    slow.AllowLoad.Set();

                slowLoaders.Clear();
            });
        }

        [Test]
        public void TestPushFocusLost()
        {
            TestScreen screen1 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen { EagerFocus = true });
            AddUntilStep("wait for focus grab", () => GetContainingInputManager().FocusedDrawable == screen1);

            pushAndEnsureCurrent(() => new TestScreen(), () => screen1);

            AddUntilStep("focus lost", () => GetContainingInputManager().FocusedDrawable != screen1);
        }

        [Test]
        public void TestPushFocusTransferred()
        {
            TestScreen screen1 = null, screen2 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen { EagerFocus = true });
            AddUntilStep("wait for focus grab", () => GetContainingInputManager().FocusedDrawable == screen1);

            pushAndEnsureCurrent(() => screen2 = new TestScreen { EagerFocus = true }, () => screen1);

            AddUntilStep("focus transferred", () => GetContainingInputManager().FocusedDrawable == screen2);
        }

        [Test]
        public void TestPushStackTwice()
        {
            TestScreen testScreen = null;

            AddStep("public push", () => stack.Push(testScreen = new TestScreen()));
            AddStep("ensure succeeds", () => Assert.IsTrue(stack.CurrentScreen == testScreen));
            AddStep("ensure internal throws", () => Assert.Throws<InvalidOperationException>(() => stack.Push(null, new TestScreen())));
        }

        [Test]
        public void TestAddScreenWithoutStackFails()
        {
            AddStep("ensure throws", () => Assert.Throws<InvalidOperationException>(() => Add(new TestScreen())));
        }

        [Test]
        public void TestPushInstantExitScreen()
        {
            AddStep("push non-valid screen", () => baseScreen.Push(new TestScreen { ValidForPush = false }));
            AddAssert("stack is single", () => stack.InternalChildren.Count == 1);
        }

        [Test]
        public void TestPushInstantExitScreenEmpty()
        {
            AddStep("fresh stack with non-valid screen", () =>
            {
                Clear();
                Add(stack = new ScreenStack(baseScreen = new TestScreen { ValidForPush = false })
                {
                    RelativeSizeAxes = Axes.Both
                });
            });

            AddAssert("stack is empty", () => stack.InternalChildren.Count == 0);
        }

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

            AddAssert("ensure child", () => screen1.GetChildScreen() == screen2);
            AddAssert("ensure parent 1", () => screen1.GetParentScreen() == baseScreen);
            AddAssert("ensure parent 2", () => screen2.GetParentScreen() == screen1);

            AddStep("pop", () => screen2.Exit());

            AddAssert("screen1 resumed from screen2", () => screen1.ResumedFrom == screen2);
            AddAssert("screen2 exited to screen1", () => screen2.ExitedTo == screen1);
            AddAssert("screen2 has lifetime end", () => screen2.LifetimeEnd != double.MaxValue);

            AddAssert("ensure child gone", () => screen1.GetChildScreen() == null);
            AddAssert("ensure parent gone", () => screen2.GetParentScreen() == null);
            AddAssert("ensure not current", () => !screen2.IsCurrentScreen());

            AddStep("pop", () => screen1.Exit());

            AddAssert("baseScreen resumed from screen1", () => baseScreen.ResumedFrom == screen1);
            AddAssert("screen1 exited to baseScreen", () => screen1.ExitedTo == baseScreen);
            AddAssert("screen1 has lifetime end", () => screen1.LifetimeEnd != double.MaxValue);
            AddUntilStep("screen1 is removed", () => screen1.Parent == null);
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
            AddAssert("screen 2 is not alive", () => !screen2.AsDrawable().IsAlive);

            AddAssert("ensure child gone", () => screen1.GetChildScreen() == null);
            AddAssert("ensure current", () => screen1.IsCurrentScreen());

            AddAssert("ensure not current", () => !screen2.IsCurrentScreen());
            AddAssert("ensure not current", () => !screen3.IsCurrentScreen());
        }

        [Test]
        public void TestAsyncPush()
        {
            TestScreenSlow screen1 = null;

            AddStep("push slow", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddAssert("base screen registered suspend", () => baseScreen.SuspendedTo == screen1);
            AddAssert("ensure not current", () => !screen1.IsCurrentScreen());
            AddStep("allow load", () => screen1.AllowLoad.Set());
            AddUntilStep("ensure current", () => screen1.IsCurrentScreen());
        }

        [Test]
        public void TestAsyncPreloadPush()
        {
            TestScreenSlow screen1 = null;
            AddStep("preload slow", () =>
            {
                screen1 = new TestScreenSlow();
                screen1.AllowLoad.Set();

                LoadComponentAsync(screen1);
            });
            pushAndEnsureCurrent(() => screen1);
        }

        [Test]
        public void TestExitBeforePush()
        {
            TestScreenSlow screen1 = null;
            TestScreen screen2 = null;

            AddStep("push screen1", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddStep("exit screen1", () => screen1.Exit());

            AddAssert("base not current (waiting load of screen1)", () => !baseScreen.IsCurrentScreen());

            AddStep("allow load", () => screen1.AllowLoad.Set());
            AddUntilStep("wait for screen to load", () => screen1.LoadState >= LoadState.Ready);

            AddUntilStep("base became current again", () => baseScreen.IsCurrentScreen());
            AddAssert("base screen was suspended", () => baseScreen.SuspendedTo == screen1);
            AddAssert("base screen was resumed", () => baseScreen.ResumedFrom == screen1);

            AddAssert("screen1 not current", () => !screen1.IsCurrentScreen());
            AddAssert("screen1 was not added to hierarchy", () => !screen1.IsLoaded);
            AddAssert("screen1 was not entered", () => screen1.EnteredFrom == null);
            AddAssert("screen1 was not exited", () => screen1.ExitedTo == null);

            AddStep("push fast", () => baseScreen.Push(screen2 = new TestScreen()));
            AddUntilStep("ensure new current", () => screen2.IsCurrentScreen());
        }

        [Test]
        public void TestScreenPushedAfterExiting()
        {
            TestScreen screen1 = null;

            AddStep("push", () => stack.Push(screen1 = new TestScreen()));

            AddUntilStep("wait for current", () => screen1.IsCurrentScreen());
            AddStep("exit screen1", () => screen1.Exit());
            AddUntilStep("ensure exited", () => !screen1.IsCurrentScreen());

            AddStep("push again", () => Assert.Throws<InvalidOperationException>(() => stack.Push(screen1)));
        }

        [Test]
        public void TestPushToNonLoadedScreenFails()
        {
            TestScreenSlow screen1 = null;

            AddStep("push slow", () => stack.Push(screen1 = new TestScreenSlow()));
            AddStep("push second slow", () => Assert.Throws<InvalidOperationException>(() => screen1.Push(new TestScreenSlow())));
        }

        [Test]
        public void TestPushAlreadyLoadedScreenFails()
        {
            TestScreen screen1 = null;

            AddStep("push once", () => stack.Push(screen1 = new TestScreen()));
            AddUntilStep("wait for screen to be loaded", () => screen1.IsLoaded);
            AddStep("exit", () => screen1.Exit());
            AddStep("push again fails", () => Assert.Throws<InvalidOperationException>(() => stack.Push(screen1)));
            AddAssert("stack in valid state", () => stack.CurrentScreen == baseScreen);
        }

        [Test]
        public void TestEventOrder()
        {
            List<int> order = new List<int>();

            var screen1 = new TestScreen
            {
                Entered = () => order.Add(1),
                Suspended = () => order.Add(2),
                Resumed = () => order.Add(5),
            };

            var screen2 = new TestScreen
            {
                Entered = () => order.Add(3),
                Exited = () => order.Add(4),
            };

            AddStep("push screen1", () => stack.Push(screen1));
            AddUntilStep("ensure current", () => screen1.IsCurrentScreen());

            AddStep("preload screen2", () => LoadComponentAsync(screen2));
            AddUntilStep("wait for load", () => screen2.LoadState == LoadState.Ready);

            AddStep("push screen2", () => screen1.Push(screen2));
            AddUntilStep("ensure current", () => screen2.IsCurrentScreen());

            AddStep("exit screen2", () => screen2.Exit());
            AddUntilStep("ensure exited", () => !screen2.IsCurrentScreen());

            AddStep("push screen2", () => screen1.Exit());
            AddUntilStep("ensure exited", () => !screen1.IsCurrentScreen());

            AddAssert("order is correct", () => order.SequenceEqual(order.OrderBy(i => i)));
        }

        [Test]
        public void TestComeVisibleFromHidden()
        {
            TestScreen screen1 = null;
            pushAndEnsureCurrent(() => screen1 = new TestScreen { Alpha = 0 });

            AddUntilStep("screen1 is visible", () => screen1.Alpha > 0);

            pushAndEnsureCurrent(() => new TestScreen { Alpha = 0 }, () => screen1);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestAsyncEventOrder(bool earlyExit, bool suspendImmediately)
        {
            TestScreenSlow screen1 = null;
            TestScreenSlow screen2 = null;
            List<int> order = null;

            if (!suspendImmediately)
            {
                AddStep("override stack", () =>
                {
                    // we can't use the [SetUp] screen stack as we need to change the ctor parameters.
                    Clear();
                    Add(stack = new ScreenStack(baseScreen = new TestScreen(id: 0))
                    {
                        RelativeSizeAxes = Axes.Both
                    });
                });
            }

            AddStep("Perform setup", () =>
            {
                order = new List<int>();
                screen1 = new TestScreenSlow(1)
                {
                    Entered = () => order.Add(1),
                    Suspended = () => order.Add(2),
                    Resumed = () => order.Add(5),
                };
                screen2 = new TestScreenSlow(2)
                {
                    Entered = () => order.Add(3),
                    Exited = () => order.Add(4),
                };
            });

            AddStep("push slow", () => stack.Push(screen1));
            AddStep("push second slow", () => stack.Push(screen2));

            AddStep("allow load 1", () => screen1.AllowLoad.Set());

            AddUntilStep("ensure screen1 not current", () => !screen1.IsCurrentScreen());
            AddUntilStep("ensure screen2 not current", () => !screen2.IsCurrentScreen());

            // but the stack has a different idea of "current"
            AddAssert("ensure screen2 is current at the stack", () => stack.CurrentScreen == screen2);

            if (suspendImmediately)
                AddUntilStep("screen1's suspending fired", () => screen1.SuspendedTo == screen2);
            else
                AddUntilStep("screen1's entered and suspending fired", () => screen1.EnteredFrom != null);

            if (earlyExit)
                AddStep("early exit 2", () => screen2.Exit());

            AddStep("allow load 2", () => screen2.AllowLoad.Set());

            if (earlyExit)
            {
                AddAssert("screen2's entered did not fire", () => screen2.EnteredFrom == null);
                AddAssert("screen2's exited did not fire", () => screen2.ExitedTo == null);
            }
            else
            {
                AddUntilStep("ensure screen2 is current", () => screen2.IsCurrentScreen());
                AddAssert("screen2's entered fired", () => screen2.EnteredFrom == screen1);
                AddStep("exit 2", () => screen2.Exit());
                AddUntilStep("ensure screen1 is current", () => screen1.IsCurrentScreen());
                AddAssert("screen2's exited fired", () => screen2.ExitedTo == screen1);
            }

            AddAssert("order is correct", () => order.SequenceEqual(order.OrderBy(i => i)));
        }

        [Test]
        public void TestEventsNotFiredBeforeScreenLoad()
        {
            Screen screen1 = null;
            bool wasLoaded = true;

            pushAndEnsureCurrent(() => screen1 = new TestScreen
            {
                // ReSharper disable once AccessToModifiedClosure
                Entered = () => wasLoaded &= screen1?.IsLoaded == true,
                // ReSharper disable once AccessToModifiedClosure
                Suspended = () => wasLoaded &= screen1?.IsLoaded == true,
            });

            pushAndEnsureCurrent(() => new TestScreen(), () => screen1);

            AddAssert("was loaded before events", () => wasLoaded);
        }

        [Test]
        public void TestAsyncDoublePush()
        {
            TestScreenSlow screen1 = null;
            TestScreenSlow screen2 = null;

            AddStep("push slow", () => stack.Push(screen1 = new TestScreenSlow()));
            // important to note we are pushing to the stack here, unlike the failing case above.
            AddStep("push second slow", () => stack.Push(screen2 = new TestScreenSlow()));

            AddAssert("base screen registered suspend", () => baseScreen.SuspendedTo == screen1);

            AddAssert("screen1 is not current", () => !screen1.IsCurrentScreen());
            AddAssert("screen2 is not current", () => !screen2.IsCurrentScreen());

            AddAssert("screen2 is current to stack", () => stack.CurrentScreen == screen2);

            AddAssert("screen1 not registered suspend", () => screen1.SuspendedTo == null);
            AddAssert("screen2 not registered entered", () => screen2.EnteredFrom == null);

            AddStep("allow load 2", () => screen2.AllowLoad.Set());

            // screen 2 won't actually be loading since the load is only triggered after screen1 is loaded.
            AddWaitStep("wait for load", 10);

            // furthermore, even though screen 2 is able to load, screen 1 has not yet so we shouldn't has received any events.
            AddAssert("screen1 is not current", () => !screen1.IsCurrentScreen());
            AddAssert("screen2 is not current", () => !screen2.IsCurrentScreen());
            AddAssert("screen1 not registered suspend", () => screen1.SuspendedTo == null);
            AddAssert("screen2 not registered entered", () => screen2.EnteredFrom == null);

            AddStep("allow load 1", () => screen1.AllowLoad.Set());
            AddUntilStep("screen1 is loaded", () => screen1.LoadState == LoadState.Loaded);
            AddUntilStep("screen2 is loaded", () => screen2.LoadState == LoadState.Loaded);

            AddUntilStep("screen1 is expired", () => !screen1.IsAlive);

            AddUntilStep("screen1 is not current", () => !screen1.IsCurrentScreen());
            AddUntilStep("screen2 is current", () => screen2.IsCurrentScreen());

            AddAssert("screen1 registered suspend", () => screen1.SuspendedTo == screen2);
            AddAssert("screen2 registered entered", () => screen2.EnteredFrom == screen1);
        }

        [Test]
        public void TestAsyncPushWithNonImmediateSuspend()
        {
            AddStep("override stack", () =>
            {
                // we can't use the [SetUp] screen stack as we need to change the ctor parameters.
                Clear();
                Add(stack = new ScreenStack(baseScreen = new TestScreen(), false)
                {
                    RelativeSizeAxes = Axes.Both
                });
            });

            TestScreenSlow screen1 = null;

            AddStep("push slow", () => baseScreen.Push(screen1 = new TestScreenSlow()));
            AddAssert("base screen not yet registered suspend", () => baseScreen.SuspendedTo == null);
            AddAssert("ensure notcurrent", () => !screen1.IsCurrentScreen());
            AddStep("allow load", () => screen1.AllowLoad.Set());
            AddUntilStep("ensure current", () => screen1.IsCurrentScreen());
            AddAssert("base screen registered suspend", () => baseScreen.SuspendedTo == screen1);
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
            AddAssert("screen 3 exited fired", () => screen3.ExitedTo == screen2);
            AddAssert("screen 3 destination is screen 1", () => screen3.Destination == screen1);
            AddAssert("screen 2 resumed not fired", () => screen2.ResumedFrom == null);
            AddAssert("screen 3 doesn't have lifetime end", () => screen3.LifetimeEnd == double.MaxValue);
            AddAssert("screen 2 valid for resume", () => screen2.ValidForResume);
            AddAssert("screen 1 valid for resume", () => screen1.ValidForResume);

            AddStep("don't block exit", () => screen3.Exiting = () => false);
            AddStep("make screen 1 current", () => screen1.MakeCurrent());
            AddAssert("screen 1 current", () => screen1.IsCurrentScreen());
            AddAssert("screen 3 exited fired", () => screen3.ExitedTo == screen2);
            AddAssert("screen 3 destination is screen 1", () => screen3.Destination == screen1);
            AddAssert("screen 2 exited fired", () => screen2.ExitedTo == screen1);
            AddAssert("screen 2 destination is screen 1", () => screen2.Destination == screen1);
            AddAssert("screen 1 resumed fired", () => screen1.ResumedFrom == screen2);
            AddAssert("screen 1 doesn't have lifetime end", () => screen1.LifetimeEnd == double.MaxValue);
            AddAssert("screen 3 has lifetime end", () => screen3.LifetimeEnd != double.MaxValue);
            AddAssert("screen 2 is not alive", () => !screen2.AsDrawable().IsAlive);
        }

        [Test]
        public void TestCallingExitFromBlockingExit()
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;
            int screen1ResumedCount = 0;

            bool blocking = true;

            pushAndEnsureCurrent(() => screen1 = new TestScreen(id: 1)
            {
                Resumed = () => screen1ResumedCount++
            });

            pushAndEnsureCurrent(() => screen2 = new TestScreen(id: 2)
            {
                Exiting = () =>
                {
                    if (blocking)
                    {
                        blocking = false;

                        // ReSharper disable once AccessToModifiedClosure
                        screen2.Exit();
                        return true;
                    }

                    // this call should fail in a way the user can understand.
                    return false;
                }
            }, () => screen1);

            AddStep("make screen 1 current", () => screen1.MakeCurrent());
            AddAssert("screen 1 resumed only once", () => screen1ResumedCount == 1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestMakeCurrentMidwayExitBlocking(bool validForResume)
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;
            TestScreen screen3 = null;
            TestScreen screen4 = null;
            int screen3ResumedCount = 0;

            pushAndEnsureCurrent(() => screen1 = new TestScreen(id: 1));
            pushAndEnsureCurrent(() => screen2 = new TestScreen(id: 2), () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(id: 3)
            {
                Resumed = () => screen3ResumedCount++
            }, () => screen2);
            pushAndEnsureCurrent(() => screen4 = new TestScreen(id: 4), () => screen3);

            AddStep("block exit screen3", () =>
            {
                screen3.Exiting = () => true;
                screen3.ValidForResume = validForResume;
            });

            AddStep("make screen1 current", () => screen1.MakeCurrent());

            // check the exit worked for one level
            AddUntilStep("screen4 is not alive", () => !screen4.AsDrawable().IsAlive);
            AddAssert("screen4 has lifetime end", () => screen4.LifetimeEnd != double.MaxValue);

            if (validForResume)
            {
                // check we blocked at screen 3
                AddAssert("screen 3 valid for resume", () => screen3.ValidForResume);
                AddAssert("screen3 is current", () => screen3.IsCurrentScreen());
                AddAssert("screen3 resumed", () => screen3ResumedCount == 1);

                // check the ValidForResume state wasn't changed on parents
                AddAssert("screen 1 still valid for resume", () => screen1.ValidForResume);
                AddAssert("screen 2 still valid for resume", () => screen2.ValidForResume);

                AddStep("make screen 1 current", () => screen1.MakeCurrent());

                // check blocking is consistent on a second attempt
                AddAssert("screen3 not resumed again", () => screen3ResumedCount == 1);
                AddAssert("screen3 is still current", () => screen3.IsCurrentScreen());

                AddStep("stop blocking exit", () => screen3.Exiting = () => false);

                AddStep("make screen1 current", () => screen1.MakeCurrent());
            }
            else
            {
                AddAssert("screen 3 not valid for resume", () => !screen3.ValidForResume);
                AddAssert("screen3 not current", () => !screen3.IsCurrentScreen());
                AddAssert("screen3 did not resume", () => screen3ResumedCount == 0);
            }

            AddAssert("screen1 current", () => screen1.IsCurrentScreen());
            AddAssert("screen1 doesn't have lifetime end", () => screen1.LifetimeEnd == double.MaxValue);
            AddUntilStep("screen3 is not alive", () => !screen3.AsDrawable().IsAlive);
        }

        [Test]
        public void TestMakeCurrentUnbindOrder()
        {
            List<TestScreen> screens = null;

            AddStep("Setup screens", () =>
            {
                screens = new List<TestScreen>();

                for (int i = 0; i < 5; i++)
                {
                    var screen = new TestScreen();

                    screen.OnUnbindAllBindables += () =>
                    {
                        if (screens.Last() != screen)
                            throw new InvalidOperationException("Unbind order was wrong");

                        screens.Remove(screen);
                    };

                    screens.Add(screen);
                }
            });

            for (int i = 0; i < 5; i++)
            {
                int local = i; // needed to store the correct value for our delegate
                pushAndEnsureCurrent(() => screens[local], () => local > 0 ? screens[local - 1] : null);
            }

            AddStep("make first screen current", () => screens.First().MakeCurrent());
            AddUntilStep("All screens unbound in correct order", () => screens.Count == 1);
        }

        [Test]
        public void TestScreensUnboundAndDisposedOnStackDisposal()
        {
            const int screen_count = 5;
            const int exit_count = 2;

            List<TestScreen> screens = null;
            int disposedScreens = 0;

            AddStep("Setup screens", () =>
            {
                screens = new List<TestScreen>();
                disposedScreens = 0;

                for (int i = 0; i < screen_count; i++)
                {
                    var screen = new TestScreen(id: i);

                    screen.OnDispose += () => disposedScreens++;

                    screen.OnUnbindAllBindables += () =>
                    {
                        if (screens.Last() != screen)
                            throw new InvalidOperationException("Unbind order was wrong");

                        screens.Remove(screen);
                    };

                    screens.Add(screen);
                }
            });

            for (int i = 0; i < screen_count; i++)
            {
                int local = i; // needed to store the correct value for our delegate
                pushAndEnsureCurrent(() => screens[local], () => local > 0 ? screens[local - 1] : null);
            }

            AddStep("remove and dispose stack", () =>
            {
                // We must exit a few screens just before the stack is disposed, otherwise the stack will update for one more frame and dispose screens itself
                for (int i = 0; i < exit_count; i++)
                    stack.Exit();

                Remove(stack);
                stack.Dispose();
            });

            AddUntilStep("All screens unbound in correct order", () => screens.Count == 0);
            AddAssert("All screens disposed", () => disposedScreens == screen_count);
        }

        /// <summary>
        /// Make sure that all bindables are returned before OnResuming is called for the next screen.
        /// </summary>
        [Test]
        public void TestReturnBindsBeforeResume()
        {
            TestScreen screen1 = null, screen2 = null;
            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            pushAndEnsureCurrent(() => screen2 = new TestScreen(true), () => screen1);
            AddStep("Exit screen", () => screen2.Exit());
            AddUntilStep("Wait until base is current", () => screen1.IsCurrentScreen());
            AddAssert("Bindables have been returned by new screen", () => !screen2.DummyBindable.Disabled && !screen2.LeasedCopy.Disabled);
        }

        [Test]
        public void TestMakeCurrentDuringLoad()
        {
            TestScreen screen1 = null;
            TestScreenSlow screen2 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            AddStep("push slow", () => screen1.Push(screen2 = new TestScreenSlow()));

            AddStep("make screen1 current", () => screen1.MakeCurrent());
            AddStep("allow load of screen2", () => screen2.AllowLoad.Set());
            AddUntilStep("wait for screen2 to load", () => screen2.LoadState == LoadState.Ready);

            AddAssert("screen1 is current screen", () => screen1.IsCurrentScreen());
            AddAssert("screen2 did not receive OnEntering", () => screen2.EnteredFrom == null);
            AddAssert("screen2 did not receive OnExiting", () => screen2.ExitedTo == null);
        }

        [Test]
        public void TestMakeCurrentDuringLoadOfMany()
        {
            TestScreen screen1 = null;
            TestScreenSlow screen2 = null;
            TestScreenSlow screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen(id: 1));
            AddStep("push slow screen 2", () => stack.Push(screen2 = new TestScreenSlow(id: 2)));
            AddStep("push slow screen 3", () => stack.Push(screen3 = new TestScreenSlow(id: 3)));

            AddAssert("Screen 1 is not current", () => !screen1.IsCurrentScreen());
            AddStep("Make current screen 1", () => screen1.MakeCurrent());
            AddAssert("Screen 1 is current", () => screen1.IsCurrentScreen());

            // Allow the screens to load out of order to test whether or not screen 3 tried to load.
            // The load should be blocked since screen 2 is already exited by MakeCurrent.
            AddStep("allow screen 3 to load", () => screen3.AllowLoad.Set());
            AddStep("allow screen 2 to load", () => screen2.AllowLoad.Set());

            AddAssert("Screen 1 is current", () => screen1.IsCurrentScreen());
            AddAssert("Screen 2 did not load", () => !screen2.IsLoaded);
            AddAssert("Screen 3 did not load", () => !screen3.IsLoaded);
        }

        [Test]
        public void TestMakeCurrentOnSameScreen()
        {
            TestScreen screen1 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            AddStep("Make current the same screen", () => screen1.MakeCurrent());
            AddAssert("Screen 1 is current", () => screen1.IsCurrentScreen());
        }

        [Test]
        public void TestPushOnExiting()
        {
            TestScreen screen1 = null;

            pushAndEnsureCurrent(() =>
            {
                screen1 = new TestScreen(id: 1);
                screen1.Exiting = () =>
                {
                    screen1.Push(new TestScreen(id: 2));
                    return true;
                };
                return screen1;
            });

            AddStep("Exit screen 1", () => screen1.Exit());
            AddAssert("Screen 1 is not current", () => !screen1.IsCurrentScreen());
            AddAssert("Stack is not empty", () => stack.CurrentScreen != null);
        }

        [Test]
        public void TestInvalidPushBlocksNonImmediateSuspend()
        {
            TestScreen screen1 = null;
            TestScreenSlow screen2 = null;

            AddStep("override stack", () =>
            {
                // we can't use the [SetUp] screen stack as we need to change the ctor parameters.
                Clear();
                Add(stack = new ScreenStack(baseScreen = new TestScreen(), false)
                {
                    RelativeSizeAxes = Axes.Both
                });
            });

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            AddStep("push slow", () => screen1.Push(screen2 = new TestScreenSlow()));
            AddStep("exit slow", () => screen2.Exit());
            AddStep("allow load", () => screen2.AllowLoad.Set());
            AddUntilStep("wait for screen 2 to load", () => screen2.LoadState >= LoadState.Ready);
            AddAssert("screen 1 did not receive suspending", () => screen1.SuspendedTo == null);
            AddAssert("screen 1 did not receive resuming", () => screen1.ResumedFrom == null);
        }

        [Test]
        public void TestInvalidPushDoesNotBlockImmediateSuspend()
        {
            TestScreen screen1 = null;
            TestScreenSlow screen2 = null;

            AddStep("override stack", () =>
            {
                // we can't use the [SetUp] screen stack as we need to change the ctor parameters.
                Clear();
                Add(stack = new ScreenStack(baseScreen = new TestScreen(), true)
                {
                    RelativeSizeAxes = Axes.Both
                });
            });

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            AddStep("push slow", () => screen1.Push(screen2 = new TestScreenSlow()));
            AddStep("exit slow", () => screen2.Exit());

            AddAssert("ensure screen 1 not current", () => !screen1.IsCurrentScreen());

            AddStep("allow load", () => screen2.AllowLoad.Set());
            AddUntilStep("wait for screen 2 to load", () => screen2.LoadState >= LoadState.Ready);

            AddUntilStep("wait for screen 1 to become current again", () => screen1.IsCurrentScreen());
            AddAssert("screen 1 did receive suspending", () => screen1.SuspendedTo == screen2);
            AddAssert("screen 1 did receive resumed", () => screen1.ResumedFrom == screen2);
        }

        /// <summary>
        /// Push two screens and check that they only handle input when they are respectively loaded and current.
        /// </summary>
        [Test]
        public void TestNonCurrentScreenDoesNotAcceptInput()
        {
            ManualInputManager inputManager = null;

            AddStep("override stack", () =>
            {
                // we can't use the [SetUp] screen stack as we need to change the ctor parameters.
                Clear();

                Add(inputManager = new ManualInputManager
                {
                    Child = stack = new ScreenStack(baseScreen = new TestScreen())
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                });
            });

            TestScreen screen1 = null;
            TestScreenSlow screen2 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen());
            AddStep("Click center of screen", () => clickScreen(inputManager, screen1));
            AddAssert("screen 1 clicked", () => screen1.ClickCount == 1);

            AddStep("push slow", () => screen1.Push(screen2 = new TestScreenSlow()));
            AddStep("Click center of screen", () => inputManager.Click(MouseButton.Left));
            AddAssert("screen 1 not clicked", () => screen1.ClickCount == 1);
            AddAssert("Screen 2 not clicked", () => screen2.ClickCount == 0 && !screen2.IsLoaded);

            AddStep("Allow screen to load", () => screen2.AllowLoad.Set());
            AddUntilStep("ensure current", () => screen2.IsCurrentScreen());
            AddStep("Click center of screen", () => clickScreen(inputManager, screen2));
            AddAssert("screen 1 not clicked", () => screen1.ClickCount == 1);
            AddAssert("Screen 2 clicked", () => screen2.ClickCount == 1 && screen2.IsLoaded);
        }

        [Test]
        public void TestMakeCurrentIntermediateResumes()
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;
            TestScreen screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen(id: 1));
            pushAndEnsureCurrent(() => screen2 = new TestScreen(id: 2)
            {
                Exiting = () => true
            }, () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(id: 3), () => screen2);

            AddStep("make screen1 current", () => screen1.MakeCurrent());

            AddAssert("screen3 exited to screen2", () => screen3.ExitedTo == screen2);
            AddAssert("screen2 resumed from screen3", () => screen2.ResumedFrom == screen3);
        }

        [Test]
        public void TestGetChildScreenAndGetParentScreenReturnNullWhenNotInStack()
        {
            TestScreen screen1 = null;
            TestScreen screen2 = null;
            TestScreen screen3 = null;

            pushAndEnsureCurrent(() => screen1 = new TestScreen(id: 1));
            pushAndEnsureCurrent(() => screen2 = new TestScreen(id: 2), () => screen1);
            pushAndEnsureCurrent(() => screen3 = new TestScreen(id: 3), () => screen2);

            AddStep("exit from screen 3", () => screen3.Exit());
            AddAssert("screen 3 parent is null", () => screen3.GetParentScreen() == null);
            AddAssert("screen 3 child is null", () => screen3.GetChildScreen() == null);
        }

        /// <summary>
        /// Ensure that an intermediary screen doesn't block and doesn't attempt to fire events when not loaded.
        /// </summary>
        [Test]
        public void TestMakeCurrentWhileScreensStillLoading()
        {
            TestScreen root = null;

            pushAndEnsureCurrent(() => root = new TestScreen(id: 1));
            AddStep("push slow", () => stack.Push(new TestScreenSlow { Exiting = () => true }));
            AddStep("push second slow", () => stack.Push(new TestScreenSlow()));

            AddStep("make screen1 current", () => root.MakeCurrent());
        }

        private void clickScreen(ManualInputManager inputManager, TestScreen screen)
        {
            inputManager.MoveMouseTo(screen);
            inputManager.Click(MouseButton.Left);
        }

        private void pushAndEnsureCurrent(Func<IScreen> screenCtor, Func<IScreen> target = null)
        {
            IScreen screen = null;
            AddStep("push", () => (target?.Invoke() ?? baseScreen).Push(screen = screenCtor()));
            AddUntilStep("ensure current", () => screen.IsCurrentScreen());
        }

        private class TestScreenSlow : TestScreen
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            public TestScreenSlow(int? id = null)
                : base(false, id)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }

        private class TestScreen : Screen
        {
            public Func<bool> Exiting;

            public Action Entered;
            public Action Suspended;
            public Action Resumed;
            public Action Exited;

            public IScreen EnteredFrom;
            public IScreen ExitedTo;
            public IScreen Destination;

            public IScreen SuspendedTo;
            public IScreen ResumedFrom;

            public static int Sequence;
            private BasicButton popButton;

            private const int transition_time = 500;

            public bool EagerFocus;

            public int ClickCount { get; private set; }

            public override bool RequestsFocus => EagerFocus;

            public override bool AcceptsFocus => EagerFocus;

            public override bool HandleNonPositionalInput => true;

            public LeasedBindable<bool> LeasedCopy;

            public readonly Bindable<bool> DummyBindable = new Bindable<bool>();

            private readonly bool shouldTakeOutLease;

            public TestScreen(bool shouldTakeOutLease = false, int? id = null)
            {
                this.shouldTakeOutLease = shouldTakeOutLease;

                if (id != null)
                    Name = id.ToString();
            }

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
                        Font = new FontUsage(size: 50)
                    },
                    popButton = new BasicButton
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
                    new BasicButton
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

                BorderColour = Color4.Red;
                Masking = true;
            }

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                BorderThickness = 10;
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                BorderThickness = 0;
            }

            public override void OnEntering(ScreenTransitionEvent e)
            {
                attemptTransformMutation();

                EnteredFrom = e.Last;
                Entered?.Invoke();

                if (shouldTakeOutLease)
                {
                    DummyBindable.BindTo(((TestScreen)e.Last).DummyBindable);
                    LeasedCopy = DummyBindable.BeginLease(true);
                }

                base.OnEntering(e);

                if (e.Last != null)
                {
                    //only show the pop button if we are entered form another screen.
                    popButton.Alpha = 1;
                }

                this.MoveTo(new Vector2(0, -DrawSize.Y));
                this.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
                this.FadeIn(1000);
            }

            public override bool OnExiting(ScreenExitEvent e)
            {
                attemptTransformMutation();

                ExitedTo = e.Next;
                Destination = e.Destination;

                Exited?.Invoke();

                if (Exiting?.Invoke() == true)
                    return true;

                this.MoveTo(new Vector2(0, -DrawSize.Y), transition_time, Easing.OutQuint);
                return base.OnExiting(e);
            }

            public override void OnSuspending(ScreenTransitionEvent e)
            {
                attemptTransformMutation();

                SuspendedTo = e.Next;
                Suspended?.Invoke();

                base.OnSuspending(e);
                this.MoveTo(new Vector2(0, DrawSize.Y), transition_time, Easing.OutQuint);
            }

            public override void OnResuming(ScreenTransitionEvent e)
            {
                attemptTransformMutation();

                ResumedFrom = e.Last;
                Resumed?.Invoke();

                base.OnResuming(e);
                this.MoveTo(Vector2.Zero, transition_time, Easing.OutQuint);
            }

            private void attemptTransformMutation()
            {
                // all callbacks should be in a state where transforms are able to be run.
                this.FadeIn();
            }

            protected override bool OnClick(ClickEvent e)
            {
                ClickCount++;
                return base.OnClick(e);
            }
        }
    }
}
