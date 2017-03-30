// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
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

        private const float steps_width = 180;
        private const float padding = 10;

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

        public void AddAssert(string description, Func<bool> assert, string extendedDescription = null)
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
