// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing.Drawables.StepButtons;
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
        private const float padding = 10;

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
                    content = new Container
                    {
                        Padding = new MarginPadding
                        {
                            Left = steps_width + padding,
                            Right = padding,
                            Top = padding,
                            Bottom = padding,
                        },
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }
            else
            {
                content.Clear();
                StepsContainer.Clear();
            }
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
