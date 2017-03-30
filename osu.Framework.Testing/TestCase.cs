// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public abstract class TestCase : Container
    {
        public virtual string Description => @"The base class for a test case";

        public FillFlowContainer<StepButton> StepsContainer;

        // TODO: Figure out how to make this private (e.g. through reflection).
        //       Right now this is required for DrawVis to inspect the Drawable tree.
        public Container Contents;

        protected override Container<Drawable> Content => Contents;

        protected DependencyContainer Dependencies { get; private set; }

        protected TestCase()
        {
            Name = GetType().ReadableName().Substring(8); // Skip the "TestCase prefix

            RelativeSizeAxes = Axes.Both;
            Masking = true;
        }

        [BackgroundDependencyLoader]
        private void load(DependencyContainer deps)
        {
            Dependencies = deps;
        }

        const float steps_width = 180;

        public virtual void Reset()
        {
            if (Contents == null)
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
                    Contents = new Container
                    {
                        Padding = new MarginPadding { Left = steps_width },
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }
            else
            {
                Contents.Clear();
                StepsContainer.Clear();
            }
        }

        public void AddStep(string description, Action action)
        {
            StepsContainer.Add(new StepButton
            {
                Text = description,
                Action = action
            });
        }

        public void AddRepeatStep(string description, Action action, int invocationCount)
        {
            StepsContainer.Add(new RepeatStepButton(invocationCount)
            {
                Text = description,
                Action = action
            });
        }

        public void AddToggleStep(string description, Action<bool> action)
        {
            StepsContainer.Add(new ToggleStepButton(action)
            {
                Text = description
            });
        }

        public void AddWaitStep(int waitCount)
        {
            StepsContainer.Add(new RepeatStepButton(waitCount)
            {
                Text = @"Wait",
                BackgroundColour = Color4.Gray
            });
        }
    }

    public class StepButton : Button
    {
        public virtual int RequiredRepetitions => 1;

        public StepButton()
        {
            Height = 25;
            RelativeSizeAxes = Axes.X;

            BackgroundColour = Color4.BlueViolet;

            CornerRadius = 2;
            Masking = true;

            SpriteText.Anchor = Anchor.CentreLeft;
            SpriteText.Origin = Anchor.CentreLeft;
            SpriteText.Padding = new MarginPadding(5);
        }
    }

    public class RepeatStepButton : StepButton
    {
        private readonly int count;
        private int invocations;

        public override int RequiredRepetitions => count;

        public new Action Action;

        private string text;
        public new string Text
        {
            get { return text; }
            set { base.Text = text = value; }
        }

        public RepeatStepButton(int count = 1)
        {
            this.count = count;

            updateText();

            BackgroundColour = Color4.Sienna;

            base.Action = () =>
            {
                invocations++;
                updateText();

                Action?.Invoke();
            };
        }

        private void updateText()
        {
            base.Text = $@"{Text} {invocations}/{count}";
        }
    }

    public class ToggleStepButton : StepButton
    {
        private readonly Action<bool> reloadCallback;
        private static readonly Color4 off_colour = Color4.Red;
        private static readonly Color4 on_colour = Color4.YellowGreen;

        public bool State;

        public override int RequiredRepetitions => 2;

        public ToggleStepButton(Action<bool> reloadCallback)
        {
            this.reloadCallback = reloadCallback;

            BackgroundColour = off_colour;
            Action += clickAction;
        }

        private void clickAction()
        {
            State = !State;
            BackgroundColour = State ? on_colour : off_colour;
            reloadCallback?.Invoke(State);
        }
    }
}