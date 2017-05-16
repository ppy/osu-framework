// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing.Drawables.StepButtons;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public abstract class TestCase : Container
    {
        public virtual string Description => @"The base class for a test case";

        public FillFlowContainer<StepButton> StepsContainer;

        private Container content;

        protected override Container<Drawable> Content => content;

        protected TestCase()
        {
            Name = GetType().ReadableName().Substring(8); // Skip the "TestCase prefix

            RelativeSizeAxes = Axes.Both;
            Masking = true;
        }

        private const float steps_width = 180;
        private const float padding = 0;

        public virtual void Reset()
        {
            if (content == null)
            {
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = new Color4(25, 25, 25, 255),
                        RelativeSizeAxes = Axes.Y,
                        Width = steps_width,
                    },
                    StepsContainer = new FillFlowContainer<StepButton>
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
                        Children = new []
                        {
                            content = new Container
                            {
                                Masking = true,
                                RelativeSizeAxes = Axes.Both
                            }
                        }
                    },
                };
            }
            else
            {
                content.Clear();
                StepsContainer.Clear();
            }
        }

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

        private StepButton loadableStep => actionIndex >= 0 ? StepsContainer.Children.Skip(actionIndex).FirstOrDefault() : null;

        protected virtual double TimePerAction => 200;

        private void runNextStep(Action onCompletion)
        {
            loadableStep?.TriggerClick();

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

            if (actionRepetition > (loadableStep?.RequiredRepetitions ?? 1) - 1)
            {
                Console.WriteLine();
                actionIndex++;
                actionRepetition = 0;
            }

            if (actionIndex > StepsContainer.Children.Count() - 1)
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

        protected void AddAssert(string description, Func<bool> assert, string extendedDescription = null)
        {
            StepsContainer.Add(new AssertButton()
            {
                Text = description,
                ExtendedDescription = extendedDescription,
                Assertion = assert,
            });
        }
    }
}
