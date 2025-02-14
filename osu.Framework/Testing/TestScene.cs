// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;
using osuTK.Graphics;
using Logger = osu.Framework.Logging.Logger;
using Vector2 = osuTK.Vector2;

namespace osu.Framework.Testing
{
    [TestFixture]
    [UseTestSceneRunner]
    public abstract partial class TestScene : Container
    {
        public readonly FillFlowContainer<Drawable> StepsContainer;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        protected virtual ITestSceneTestRunner CreateRunner() => new TestSceneTestRunner();

        /// <summary>
        /// Delay between invoking two <see cref="StepButton"/>s in automatic runs.
        /// </summary>
        protected virtual double TimePerAction => 200;

        /// <summary>
        /// Whether to automatically run the the first actual <see cref="StepButton"/> (one that is not part of <see cref="SetUpAttribute">[SetUp]</see> or <see cref="SetUpStepsAttribute">[SetUpSteps]</see>)
        /// when the test is first loaded.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>. Should be set to <c>false</c> if the first step in the first <see cref="TestAttribute">test</see> has unwanted-by-default behaviour.
        /// </remarks>
        public virtual bool AutomaticallyRunFirstStep => true;

        private GameHost host;
        private Task runTask;
        private ITestSceneTestRunner runner;

        private readonly Box backgroundFill;

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
            ArgumentNullException.ThrowIfNull(game);

            exitNestedGame();

            nestedGame = game;
            nestedGame.SetupLogging(host.Storage, host.CacheStorage);
            nestedGame.SetHost(host);

            base.Add(nestedGame);
        }

        public override void Add(Drawable drawable)
        {
            ArgumentNullException.ThrowIfNull(drawable);

            if (drawable is Game)
                throw new InvalidOperationException($"Use {nameof(AddGame)} when testing a game instance.");

            base.Add(drawable);
        }

        protected override void AddInternal(Drawable drawable)
        {
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Add)} instead.");
        }

        protected internal override void ClearInternal(bool disposeChildren = true) =>
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Clear)} instead.");

        protected internal override bool RemoveInternal(Drawable drawable, bool disposeImmediately) =>
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
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                backgroundFill = new Box
                                {
                                    Colour = Color4.Black,
                                    RelativeSizeAxes = Axes.Both,
                                },
                                content = new DrawFrameRecordingContainer
                                {
                                    Masking = true,
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        },
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

        public void RunAllSteps(Action onCompletion = null, Action<StepButton, Exception> onError = null, Func<StepButton, bool> stopCondition = null, StepButton startFromStep = null)
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

        private void runNextStep(Action onCompletion, Action<StepButton, Exception> onError, Func<StepButton, bool> stopCondition)
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
                onError?.Invoke(loadableStep, e);
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

        public void ChangeBackgroundColour(ColourInfo colour)
            => backgroundFill.FadeColour(colour, 200, Easing.OutQuint);

        private bool addStepsAsSetupSteps;

        public void AddStep(StepButton step)
        {
            schedule(() =>
            {
                StepsContainer.Add(step);
            });
        }

        public void AddStep([NotNull] string description, [NotNull] Action action)
        {
            AddStep(new SingleStepButton
            {
                Text = description,
                Action = action,
                IsSetupStep = addStepsAsSetupSteps
            });
        }

        public void AddLabel([NotNull] string description)
        {
            AddStep(new LabelStep
            {
                Text = description,
                IsSetupStep = false,
                Action = step =>
                {
                    Logger.Log($@"💨 {this} {description}");

                    // kinda hacky way to avoid this doesn't get triggered by automated runs.
                    if (step.IsHovered)
                        RunAllSteps(startFromStep: step, stopCondition: s => s is LabelStep, onError: (s, e) => Logger.Error(e, $"Step {s} triggered error"));
                },
            });
        }

        protected void AddRepeatStep([NotNull] string description, [NotNull] Action action, int invocationCount)
        {
            AddStep(new RepeatStepButton
            {
                Text = description,
                IsSetupStep = addStepsAsSetupSteps,
                Action = action,
                Count = invocationCount
            });
        }

        protected void AddToggleStep([NotNull] string description, [NotNull] Action<bool> action)
        {
            AddStep(new ToggleStepButton
            {
                Text = description,
                IsSetupStep = addStepsAsSetupSteps,
                Action = action,
            });
        }

        protected void AddUntilStep([CanBeNull] string description, [NotNull] Func<bool> waitUntilTrueDelegate)
        {
            AddStep(new UntilStepButton
            {
                Text = description ?? @"Until",
                IsSetupStep = addStepsAsSetupSteps,
                CallStack = new StackTrace(1, true),
                Assertion = waitUntilTrueDelegate,
            });
        }

        protected void AddUntilStep<T>([CanBeNull] string description, [NotNull] ActualValueDelegate<T> actualValue, [NotNull] Func<IResolveConstraint> constraint)
        {
            ConstraintResult lastResult = null;

            AddStep(new UntilStepButton
            {
                Text = description ?? @"Until",
                IsSetupStep = addStepsAsSetupSteps,
                CallStack = new StackTrace(1, true),
                Assertion = () =>
                {
                    lastResult = constraint().Resolve().ApplyTo(actualValue());
                    return lastResult.IsSuccess;
                },
                GetFailureMessage = () =>
                {
                    if (lastResult == null)
                        return string.Empty;

                    var writer = new TextMessageWriter(string.Empty);
                    lastResult.WriteMessageTo(writer);
                    return writer.ToString().TrimStart();
                }
            });
        }

        protected void AddWaitStep([CanBeNull] string description, int waitCount)
        {
            AddStep(new RepeatStepButton
            {
                Text = description ?? @"Wait",
                IsSetupStep = addStepsAsSetupSteps,
                Count = waitCount
            });
        }

        protected void AddSliderStep<T>([NotNull] string description, T min, T max, T start, [NotNull] Action<T> valueChanged) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            schedule(() =>
            {
                StepsContainer.Add(new StepSlider<T>(description, min, max, start)
                {
                    ValueChanged = valueChanged,
                });
            });
        }

        protected void AddAssert([NotNull] string description, [NotNull] Func<bool> assert, [CanBeNull] string extendedDescription = null)
        {
            AddStep(new AssertButton
            {
                Text = description,
                IsSetupStep = addStepsAsSetupSteps,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1, true),
                Assertion = assert,
            });
        }

        protected void AddAssert<T>([NotNull] string description, [NotNull] ActualValueDelegate<T> actualValue, [NotNull] Func<IResolveConstraint> constraint,
                                    [CanBeNull] string extendedDescription = null)
        {
            ConstraintResult lastResult = null;

            AddStep(new AssertButton
            {
                Text = description,
                IsSetupStep = addStepsAsSetupSteps,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1, true),
                Assertion = () =>
                {
                    lastResult = constraint().Resolve().ApplyTo(actualValue());
                    return lastResult.IsSuccess;
                },
                GetFailureMessage = () =>
                {
                    if (lastResult == null)
                        return string.Empty;

                    var writer = new TextMessageWriter(string.Empty);
                    lastResult.WriteMessageTo(writer);
                    return writer.ToString().TrimStart();
                }
            });
        }

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
            // important that we do a synchronous disposal.
            // using Expire() will cause a deadlock in AsyncDisposalQueue.
            nestedGame?.Parent?.RemoveInternal(nestedGame, true);
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

        internal virtual void RunAfterTest()
        {
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

        private class UseTestSceneRunnerAttribute : TestActionAttribute
        {
            public override void BeforeTest(ITest test)
            {
                if (test.Fixture is not TestScene testScene)
                    return;

                bool hasSoloAttribute = test.Method?.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(SoloAttribute)) == true;
                if (DebugUtils.IsNUnitRunning && hasSoloAttribute)
                    throw new InvalidOperationException($"{nameof(SoloAttribute)} should not be specified on tests running under NUnit.");

                // Since the host is created in OneTimeSetUp, all game threads will have the fixture's execution context
                // This is undesirable since each test is run using those same threads, so we must make sure the execution context
                // for the game threads refers to the current _test_ execution context for each test
                var executionContext = TestExecutionContext.CurrentContext;

                foreach (var thread in testScene.host.Threads)
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

                if (TestContext.CurrentContext.Test.MethodName != nameof(TestScene.TestConstructor))
                    testScene.Schedule(() => testScene.StepsContainer.Clear());

                testScene.RunSetUpSteps();
            }

            public override void AfterTest(ITest test)
            {
                if (test.Fixture is not TestScene testScene)
                    return;

                testScene.RunTearDownSteps();

                testScene.checkForErrors();
                testScene.runner.RunTestBlocking(testScene);
                testScene.checkForErrors();

                if (FrameworkEnvironment.ForceTestGC)
                {
                    // Force any unobserved exceptions to fire against the current test run.
                    // Without this they could be delayed until a future test scene is running, making tracking down the cause difficult.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                testScene.RunAfterTest();
            }

            public override ActionTargets Targets => ActionTargets.Test;
        }
    }

    #endregion
}
