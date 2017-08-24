// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Desktop.Platform;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    [TestFixture]
    public abstract class TestCase : Container
    {
        public virtual string Description => @"The base class for a test case";

        public readonly FillFlowContainer<Drawable> StepsContainer;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        [Test]
        public virtual void RunTest()
        {
            using (var host = new HeadlessGameHost(realtime: false))
                host.Run(new TestCaseTestRunner(this));
        }

        protected TestCase()
        {
            Name = GetType().ReadableName().Substring(8); // Skip the "TestCase prefix

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
                StepsContainer = new FillFlowContainer<Drawable>
                {
                    Direction = FillDirection.Vertical,
                    Depth = float.MinValue,
                    Padding = new MarginPadding(5),
                    Spacing = new Vector2(5),
                    AutoSizeAxes = Axes.Y,
                    Width = steps_width,
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

        public void RunAllSteps(Action onCompletion = null)
        {
            stepRunner?.Cancel();

            actionIndex = -1;
            actionRepetition = 0;
            runNextStep(onCompletion);
        }

        private Drawable loadableStep => actionIndex >= 0 ? StepsContainer.Children.ElementAtOrDefault(actionIndex) : null;

        protected virtual double TimePerAction => 200;

        private void runNextStep(Action onCompletion)
        {
            loadableStep?.TriggerOnClick();

            string text = ".";

            if (actionRepetition == 0)
            {
                text = $"{(int)Time.Current}: ".PadLeft(7);

                if (actionIndex < 0)
                    text += $"{GetType().ReadableName()}";
                else
                {
                    text += $"step {actionIndex + 1}";
                    text = text.PadRight(16) + $"{loadableStep?.ToString() ?? string.Empty}";
                }
            }

            Console.Write(text);

            actionRepetition++;

            if (actionRepetition > ((loadableStep as StepButton)?.RequiredRepetitions ?? 1) - 1)
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
                stepRunner = Scheduler.AddDelayed(() => runNextStep(onCompletion), TimePerAction);
        }

        protected void AddStep(string description, Action action)
        {
            StepsContainer.Add(new SingleStepButton
            {
                Text = description,
                Action = action
            });
        }

        protected void AddRepeatStep(string description, Action action, int invocationCount)
        {
            StepsContainer.Add(new RepeatStepButton(invocationCount)
            {
                Text = description,
                Action = action
            });
        }

        protected void AddToggleStep(string description, Action<bool> action)
        {
            StepsContainer.Add(new ToggleStepButton(action)
            {
                Text = description
            });
        }

        protected void AddWaitStep(int waitCount)
        {
            StepsContainer.Add(new RepeatStepButton(waitCount)
            {
                Text = @"Wait",
                BackgroundColour = Color4.Gray
            });
        }

        protected void AddSliderStep<T>(string description, T min, T max, T start, Action<T> valueChanged)
            where T : struct
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
                Assertion = assert,
            });
        }
    }
}
