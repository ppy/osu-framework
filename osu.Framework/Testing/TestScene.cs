// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Framework.Testing
{
    [ExcludeFromDynamicCompile]
    [TestFixture]
    public abstract class TestScene : Container
    {
        public readonly FillFlowContainer<Drawable> StepsContainer;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        protected virtual ITestSceneTestRunner CreateRunner() => new TestSceneTestRunner();

        private GameHost host;
        private Task runTask;
        private ITestSceneTestRunner runner;

        /// <summary>
        /// A nested game instance, if added via <see cref="AddGame"/>.
        /// </summary>
        private Game nestedGame;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            // SetupGameHost won't be run for interactive runs so we need to populate this from DI.
            this.host ??= host;
        }

        /// <summary>
        /// Add a full game instance in a nested state for visual testing.
        /// </summary>
        /// <remarks>
        /// Any previous game added via this method will be disposed if called multiple times.
        /// </remarks>
        /// <param name="game">The game to add.</param>
        protected void AddGame([NotNull] Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            exitNestedGame();

            nestedGame = game;
            nestedGame.SetHost(host);

            base.Add(nestedGame);
        }

        public override void Add(Drawable drawable)
        {
            if (drawable is Game)
                throw new InvalidOperationException($"Use {nameof(AddGame)} when testing a game instance.");

            base.Add(drawable);
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Add)} instead.");
        }

        protected internal override void ClearInternal(bool disposeChildren = true) =>
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Clear)} instead.");

        protected internal override bool RemoveInternal(Drawable drawable) =>
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Remove)} instead.");

        /// <summary>
        /// Tests any steps and assertions in the constructor of this <see cref="TestScene"/>.
        /// This test must run before any other tests, as it relies on <see cref="StepsContainer"/> not being cleared and not having any elements.
        /// </summary>
        [Test, Order(int.MinValue)]
        public void TestConstructor()
        {
        }

        protected TestScene()
        {
            Name = RemovePrefix(GetType().ReadableName());

            RelativeSizeAxes = Axes.Both;
            Masking = true;

            base.AddInternal(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = new Color4(25, 25, 25, 255),
                        RelativeSizeAxes = Axes.Y,
                        Width = steps_width,
                    },
                    scroll = new BasicScrollContainer
                    {
                        Width = steps_width,
                        Depth = float.MinValue,
                        RelativeSizeAxes = Axes.Y,
                        Child = StepsContainer = new FillFlowContainer<Drawable>
                        {
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(3),
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding(10),
                            Child = new SpriteText
                            {
                                Font = FrameworkFont.Condensed.With(size: 16),
                                Text = Name,
                                Margin = new MarginPadding { Bottom = 5 },
                            }
                        },
                    },
                    new Container
                    {
                        Masking = true,
                        Padding = new MarginPadding
                        {
                            Left = steps_width + padding,
                            Right = padding,
                            Top = padding,
                            Bottom = padding,
                        },
                        RelativeSizeAxes = Axes.Both,
                        Child = content = new DrawFrameRecordingContainer
                        {
                            Masking = true,
                            RelativeSizeAxes = Axes.Both
                        }
                    },
                }
            });
        }

        private const float steps_width = 180;
        private const float padding = 0;

        private int actionIndex;
        private int actionRepetition;
        private ScheduledDelegate stepRunner;
        private readonly ScrollContainer<Drawable> scroll;

        public void RunAllSteps(Action onCompletion = null, Action<Exception> onError = null, Func<StepButton, bool> stopCondition = null, StepButton startFromStep = null)
        {
            // schedule once as we want to ensure we have run our LoadComplete before attempting to execute steps.
            // a user may be adding a step in LoadComplete.
            Schedule(() =>
            {
                stepRunner?.Cancel();
                foreach (var step in StepsContainer.FlowingChildren.OfType<StepButton>())
                    step.Reset();

                actionIndex = startFromStep != null ? StepsContainer.IndexOf(startFromStep) + 1 : -1;
                actionRepetition = 0;
                runNextStep(onCompletion, onError, stopCondition);
            });
        }

        private StepButton loadableStep => actionIndex >= 0 ? StepsContainer.Children.ElementAtOrDefault(actionIndex) as StepButton : null;

        protected virtual double TimePerAction => 200;

        private void runNextStep(Action onCompletion, Action<Exception> onError, Func<StepButton, bool> stopCondition)
        {
            try
            {
                if (loadableStep != null)
                {
                    if (actionRepetition == 0)
                        Logger.Log($"🔸 Step #{actionIndex + 1} {loadableStep.Text}");

                    scroll.ScrollIntoView(loadableStep);
                    loadableStep.PerformStep();
                }
            }
            catch (Exception e)
            {
                Logger.Log(actionRepetition > 0
                    ? $"💥 Failed (on attempt {actionRepetition:#,0})"
                    : "💥 Failed");

                LoadingComponentsLogger.LogAndFlush();
                onError?.Invoke(e);
                return;
            }

            actionRepetition++;

            if (actionRepetition > (loadableStep?.RequiredRepetitions ?? 1) - 1)
            {
                if (actionIndex >= 0 && actionRepetition > 1)
                    Logger.Log($"✔️ {actionRepetition} repetitions");

                actionIndex++;
                actionRepetition = 0;

                if (loadableStep != null && stopCondition?.Invoke(loadableStep) == true)
                    return;
            }

            if (actionIndex > StepsContainer.Children.Count - 1)
            {
                Logger.Log($"✅ {GetType().ReadableName()} completed");
                onCompletion?.Invoke();
                return;
            }

            if (Parent != null)
                stepRunner = Scheduler.AddDelayed(() => runNextStep(onCompletion, onError, stopCondition), TimePerAction);
        }

        public void AddStep(StepButton step) => schedule(() => StepsContainer.Add(step));

        private bool addStepsAsSetupSteps;

        public StepButton AddStep(string description, Action action)
        {
            var step = new SingleStepButton(addStepsAsSetupSteps)
            {
                Text = description,
                Action = action
            };

            AddStep(step);

            return step;
        }

        public LabelStep AddLabel(string description)
        {
            var step = new LabelStep
            {
                Text = description,
            };

            step.Action = () =>
            {
                Logger.Log($@"💨 {this} {description}");

                // kinda hacky way to avoid this doesn't get triggered by automated runs.
                if (step.IsHovered)
                    RunAllSteps(startFromStep: step, stopCondition: s => s is LabelStep);
            };

            AddStep(step);

            return step;
        }

        protected void AddRepeatStep(string description, Action action, int invocationCount) => schedule(() =>
        {
            StepsContainer.Add(new RepeatStepButton(action, invocationCount, addStepsAsSetupSteps)
            {
                Text = description,
            });
        });

        protected void AddToggleStep(string description, Action<bool> action) => schedule(() =>
        {
            StepsContainer.Add(new ToggleStepButton(action)
            {
                Text = description
            });
        });

        protected void AddUntilStep(string description, Func<bool> waitUntilTrueDelegate) => schedule(() =>
        {
            StepsContainer.Add(new UntilStepButton(waitUntilTrueDelegate, addStepsAsSetupSteps)
            {
                Text = description ?? @"Until",
            });
        });

        protected void AddWaitStep(string description, int waitCount) => schedule(() =>
        {
            StepsContainer.Add(new RepeatStepButton(() => { }, waitCount, addStepsAsSetupSteps)
            {
                Text = description ?? @"Wait",
            });
        });

        protected void AddSliderStep<T>(string description, T min, T max, T start, Action<T> valueChanged) where T : struct, IComparable<T>, IConvertible, IEquatable<T> => schedule(() =>
        {
            StepsContainer.Add(new StepSlider<T>(description, min, max, start)
            {
                ValueChanged = valueChanged,
            });
        });

        protected void AddAssert(string description, Func<bool> assert, string extendedDescription = null) => schedule(() =>
        {
            StepsContainer.Add(new AssertButton(addStepsAsSetupSteps)
            {
                Text = description,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1),
                Assertion = assert,
            });
        });

        internal void RunSetUpSteps()
        {
            addStepsAsSetupSteps = true;
            foreach (var method in ReflectionUtils.GetMethodsWithAttribute(GetType(), typeof(SetUpStepsAttribute), true))
                method.Invoke(this, null);
            addStepsAsSetupSteps = false;
        }

        internal void RunTearDownSteps()
        {
            foreach (var method in ReflectionUtils.GetMethodsWithAttribute(GetType(), typeof(TearDownStepsAttribute), true))
                method.Invoke(this, null);
        }

        /// <summary>
        /// Remove the "TestScene" prefix from a name.
        /// </summary>
        /// <param name="name"></param>
        public static string RemovePrefix(string name)
        {
            return name.Replace("TestCase", string.Empty) // TestScene used to be called TestCase. This handles consumer projects which haven't updated their naming for the near future.
                       .Replace(nameof(TestScene), string.Empty);
        }

        // should run inline where possible. this is to fix RunAllSteps potentially finding no steps if the steps are added in LoadComplete (else they get forcefully scheduled too late)
        private void schedule(Action action) => Scheduler.Add(action, false);

        private void exitNestedGame()
        {
            if (nestedGame?.Parent == null) return;

            // important that we do a synchronous disposal.
            // using Expire() will cause a deadlock in AsyncDisposalQueue.
            nestedGame.Parent.RemoveInternal(nestedGame);
            nestedGame.Dispose();
        }

        #region NUnit execution setup

        [OneTimeSetUp]
        public void SetupGameHostForNUnit()
        {
            host = new TestSceneHost($"{GetType().Name}-{Guid.NewGuid()}", exitNestedGame);
            runner = CreateRunner();

            if (!(runner is Game game))
                throw new InvalidCastException($"The test runner must be a {nameof(Game)}.");

            runTask = Task.Factory.StartNew(() => host.Run(game), TaskCreationOptions.LongRunning);

            while (!game.IsLoaded)
            {
                checkForErrors();
                Thread.Sleep(10);
            }
        }

        [SetUp]
        public void SetUpTestForNUnit()
        {
            if (DebugUtils.IsNUnitRunning)
            {
                // Since the host is created in OneTimeSetUp, all game threads will have the fixture's execution context
                // This is undesirable since each test is run using those same threads, so we must make sure the execution context
                // for the game threads refers to the current _test_ execution context for each test
                var executionContext = TestExecutionContext.CurrentContext;

                foreach (var thread in host.Threads)
                {
                    thread.Scheduler.Add(() =>
                    {
                        TestExecutionContext.CurrentContext.CurrentResult = executionContext.CurrentResult;
                        TestExecutionContext.CurrentContext.CurrentTest = executionContext.CurrentTest;
                        TestExecutionContext.CurrentContext.CurrentCulture = executionContext.CurrentCulture;
                        TestExecutionContext.CurrentContext.CurrentPrincipal = executionContext.CurrentPrincipal;
                        TestExecutionContext.CurrentContext.CurrentRepeatCount = executionContext.CurrentRepeatCount;
                        TestExecutionContext.CurrentContext.CurrentUICulture = executionContext.CurrentUICulture;
                    });
                }

                if (TestContext.CurrentContext.Test.MethodName != nameof(TestConstructor))
                    schedule(() => StepsContainer.Clear());

                RunSetUpSteps();
            }
        }

        [TearDown]
        protected virtual void RunTestsFromNUnit()
        {
            RunTearDownSteps();

            checkForErrors();
            runner.RunTestBlocking(this);
            checkForErrors();
        }

        [OneTimeTearDown]
        public void DestroyGameHostFromNUnit()
        {
            ((TestSceneHost)host).ExitFromRunner();

            try
            {
                runTask.WaitSafely();
            }
            finally
            {
                host.Dispose();

                try
                {
                    // clean up after each run
                    host.Storage.DeleteDirectory(string.Empty);
                }
                catch
                {
                }
            }
        }

        private void checkForErrors()
        {
            if (host.ExecutionState == ExecutionState.Stopping)
                runTask.WaitSafely();

            if (runTask.Exception != null)
                throw runTask.Exception;
        }

        private class TestSceneHost : TestRunHeadlessGameHost
        {
            private readonly Action onExitRequest;

            public TestSceneHost(string name, Action onExitRequest)
                : base(name, new HostOptions())
            {
                this.onExitRequest = onExitRequest;
            }

            protected override void PerformExit(bool immediately)
            {
                // Block base call so nested game instances can't end the testing process.
                // See ExitFromRunner below.
                onExitRequest?.Invoke();
            }

            public void ExitFromRunner() => base.PerformExit(false);
        }
    }

    #endregion
}
