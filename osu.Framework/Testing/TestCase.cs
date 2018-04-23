// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using System.Threading.Tasks;
using System.Threading;

namespace osu.Framework.Testing
{
    [TestFixture]
    public abstract class TestCase : Container, IDynamicallyCompile
    {
        public readonly FillFlowContainer<Drawable> StepsContainer;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        protected virtual ITestCaseTestRunner CreateRunner() => new TestCaseTestRunner();

        private GameHost host;
        private Task runTask;
        private ITestCaseTestRunner runner;
        private bool isNUnitRunning;

        [OneTimeSetUp]
        public void SetupGameHost()
        {
            isNUnitRunning = true;

            host = new HeadlessGameHost($"test-{Guid.NewGuid()}", realtime: false);
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

        [OneTimeTearDown]
        public void DestroyGameHost()
        {
            host.Exit();
            runTask.Wait();
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

        /// <summary>
        /// Runs prior to all tests except <see cref="TestConstructor"/> to ensure that the <see cref="TestCase"/>
        /// is reverted to a clean state for all tests.
        /// </summary>
        [SetUp]
        public void SetupTest()
        {
            if (isNUnitRunning && TestContext.CurrentContext.Test.MethodName != nameof(TestConstructor))
                StepsContainer.Clear();
        }

        [TearDown]
        public void RunTests()
        {
            checkForErrors();
            runner.RunTestBlocking(this);
            checkForErrors();
        }

        private void checkForErrors()
        {
            if (runTask.Exception != null)
                throw runTask.Exception;
        }

        /// <summary>
        /// Most derived usages of this start with TestCase. This will be removed for display purposes.
        /// </summary>
        private const string prefix = "TestCase";

        /// <summary>
        /// Tests any steps and assertions in the constructor of this <see cref="TestCase"/>.
        /// This test must run before any other tests, as it relies on <see cref="StepsContainer"/> not being cleared and not having any elements.
        /// </summary>
        [Test, Order(int.MinValue)]
        public void TestConstructor()
        {
        }

        protected TestCase()
        {
            Name = GetType().ReadableName();

            // Skip the "TestCase" prefix
            if (Name.StartsWith(prefix)) Name = Name.Replace(prefix, string.Empty);

            RelativeSizeAxes = Axes.Both;
            Masking = true;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = new Color4(25, 25, 25, 255),
                    RelativeSizeAxes = Axes.Y,
                    Width = steps_width,
                },
                scroll = new ScrollContainer
                {
                    Width = steps_width,
                    Depth = float.MinValue,
                    RelativeSizeAxes = Axes.Y,
                    Padding = new MarginPadding(5),
                    Child = StepsContainer = new FillFlowContainer<Drawable>
                    {
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(5),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
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
                    Child = content = new Container
                    {
                        Masking = true,
                        RelativeSizeAxes = Axes.Both
                    }
                },
            };
        }

        private const float steps_width = 180;
        private const float padding = 0;

        private int actionIndex;
        private int actionRepetition;
        private ScheduledDelegate stepRunner;
        private readonly ScrollContainer scroll;

        public void RunAllSteps(Action onCompletion = null, Action<Exception> onError = null)
        {
            // schedule once as we want to ensure we have run our LoadComplete before atttempting to execute steps.
            // a user may be adding a step in LoadComplete.
            Schedule(() =>
            {
                stepRunner?.Cancel();
                foreach (var step in StepsContainer.OfType<StepButton>())
                    step.Reset();

                actionIndex = -1;
                actionRepetition = 0;
                runNextStep(onCompletion, onError);
            });
        }

        public void RunFirstStep()
        {
            stepRunner?.Cancel(); // Fixes RunAllSteps not working when toggled off
            foreach (var step in StepsContainer.OfType<StepButton>())
                step.Reset();

            actionIndex = 0;
            try
            {
                loadableStep?.PerformStep();
            }
            catch (Exception e)
            {
                Logging.Logger.Error(e, "Error on running first step");
            }
        }

        private StepButton loadableStep => actionIndex >= 0 ? StepsContainer.Children.ElementAtOrDefault(actionIndex) as StepButton : null;

        protected virtual double TimePerAction => 200;

        private void runNextStep(Action onCompletion, Action<Exception> onError)
        {
            try
            {
                if (loadableStep != null)
                {
                    if (loadableStep.IsMaskedAway)
                        scroll.ScrollTo(loadableStep);
                    loadableStep.PerformStep();
                }
            }
            catch (Exception e)
            {
                onError?.Invoke(e);
                return;
            }

            string text = ".";

            if (actionRepetition == 0)
            {
                text = $"{(int)Time.Current}: ".PadLeft(7);

                if (actionIndex < 0)
                    text += $"{GetType().ReadableName()}";
                else
                    text += $"step {actionIndex + 1} {loadableStep?.ToString() ?? string.Empty}";
            }

            Console.Write(text);

            actionRepetition++;

            if (actionRepetition > (loadableStep?.RequiredRepetitions ?? 1) - 1)
            {
                Console.WriteLine();
                actionIndex++;
                actionRepetition = 0;
            }

            if (actionIndex > StepsContainer.Children.Count - 1)
            {
                onCompletion?.Invoke();
                return;
            }

            if (Parent != null)
                stepRunner = Scheduler.AddDelayed(() => runNextStep(onCompletion, onError), TimePerAction);
        }

        public StepButton AddStep(string description, Action action)
        {
            var step = new SingleStepButton
            {
                Text = description,
                Action = action
            };

            StepsContainer.Add(step);

            return step;
        }

        protected void AddRepeatStep(string description, Action action, int invocationCount)
        {
            StepsContainer.Add(new RepeatStepButton(action, invocationCount)
            {
                Text = description,
            });
        }

        protected void AddToggleStep(string description, Action<bool> action)
        {
            StepsContainer.Add(new ToggleStepButton(action)
            {
                Text = description
            });
        }

        protected void AddUntilStep(Func<bool> waitUntilTrueDelegate, string description = null)
        {
            StepsContainer.Add(new UntilStepButton(waitUntilTrueDelegate)
            {
                Text = description ?? @"Until",
            });
        }

        protected void AddWaitStep(int waitCount, string description = null)
        {
            StepsContainer.Add(new RepeatStepButton(() => { }, waitCount)
            {
                Text = description ?? @"Wait",
            });
        }

        protected void AddSliderStep<T>(string description, T min, T max, T start, Action<T> valueChanged)
            where T : struct, IComparable, IConvertible
        {
            StepsContainer.Add(new StepSlider<T>(description, min, max, start)
            {
                ValueChanged = valueChanged,
            });
        }

        protected void AddAssert(string description, Func<bool> assert, string extendedDescription = null)
        {
            StepsContainer.Add(new AssertButton
            {
                Text = description,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1),
                Assertion = assert,
            });
        }

        public virtual IReadOnlyList<Type> RequiredTypes => new Type[] { };
    }
}
