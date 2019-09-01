// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;

namespace osu.Framework.Testing.Drawables
{
    public class StepsContainer : FillFlowContainer<Drawable>
    {
        private int actionIndex;
        private int actionRepetition;
        private ScheduledDelegate stepRunner;

        public void RunAll(Action onCompletion = null, Action<Exception> onError = null, Func<StepButton, bool> stopCondition = null, StepButton startFromStep = null)
        {
            // schedule once as we want to ensure we have run our LoadComplete before attempting to execute steps.
            // a user may be adding a step in LoadComplete.
            Schedule(() =>
            {
                stepRunner?.Cancel();
                foreach (var step in FlowingChildren.OfType<StepButton>())
                    step.Reset();

                actionIndex = startFromStep != null ? IndexOf(startFromStep) + 1 : -1;
                actionRepetition = 0;
                runNext(onCompletion, onError, stopCondition);
            });
        }

        private void runNext(Action onCompletion, Action<Exception> onError, Func<StepButton, bool> stopCondition)
        {
            try
            {
                if (loadableStep != null)
                {
                    if (loadableStep.IsMaskedAway)
                        (Parent.Parent as ScrollContainer<Drawable>)?.ScrollTo(loadableStep);

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
                actionIndex++;
                actionRepetition = 0;
                Console.WriteLine();

                if (loadableStep != null && stopCondition?.Invoke(loadableStep) == true)
                    return;
            }

            if (actionIndex > Children.Count - 1)
            {
                onCompletion?.Invoke();
                return;
            }

            if (Parent != null)
                stepRunner = Scheduler.AddDelayed(() => runNext(onCompletion, onError, stopCondition), TimePerAction);
        }

        public void AddStep(StepButton step) => schedule(() => Add(step));

        public StepButton AddStep(string description, Action action)
        {
            var step = new SingleStepButton
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
                // kinda hacky way to avoid this doesn't get triggered by automated runs.
                if (step.IsHovered)
                    RunAll(startFromStep: step, stopCondition: s => s is LabelStep);
            };

            AddStep(step);

            return step;
        }

        public void AddRepeatStep(string description, Action action, int invocationCount) => schedule(() =>
        {
            Add(new RepeatStepButton(action, invocationCount)
            {
                Text = description,
            });
        });

        public void AddToggleStep(string description, Action<bool> action) => schedule(() =>
        {
            Add(new ToggleStepButton(action)
            {
                Text = description
            });
        });

        [Obsolete("Parameter order didn't match other methods – switch order to fix")] // can be removed 20190919
        public void AddUntilStep(Func<bool> waitUntilTrueDelegate, string description = null)
            => AddUntilStep(description, waitUntilTrueDelegate);

        public void AddUntilStep(string description, Func<bool> waitUntilTrueDelegate) => schedule(() =>
        {
            Add(new UntilStepButton(waitUntilTrueDelegate)
            {
                Text = description ?? @"Until",
            });
        });

        [Obsolete("Parameter order didn't match other methods – switch order to fix")] // can be removed 20190919
        public void AddWaitStep(int waitCount, string description = null)
            => AddWaitStep(description, waitCount);

        public void AddWaitStep(string description, int waitCount) => schedule(() =>
        {
            Add(new RepeatStepButton(() => { }, waitCount)
            {
                Text = description ?? @"Wait",
            });
        });

        public void AddSliderStep<T>(string description, T min, T max, T start, Action<T> valueChanged) where T : struct, IComparable, IConvertible => schedule(() =>
        {
            Add(new StepSlider<T>(description, min, max, start)
            {
                ValueChanged = valueChanged,
            });
        });

        public void AddAssert(string description, Func<bool> assert, string extendedDescription = null) => schedule(() =>
        {
            Add(new AssertButton
            {
                Text = description,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1),
                Assertion = assert,
            });
        });

        // should run inline where possible. this is to fix RunAll potentially finding no steps if the steps are added in LoadComplete (else they get forcefully scheduled too late)
        private void schedule(Action action) => Scheduler.Add(action, false);
        private StepButton loadableStep => actionIndex >= 0 ? Children.ElementAtOrDefault(actionIndex) as StepButton : null;

        public double TimePerAction => 200;
    }
}
