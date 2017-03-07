// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Screens.Testing
{
    public abstract class TestCase : Container
    {
        public virtual string Description => @"The base class for a test case";

        protected FillFlowContainer ButtonsContainer;

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

        public virtual void Reset()
        {
            if (Contents == null)
            {
                InternalChildren = new Drawable[]
                {
                    Contents = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    ButtonsContainer = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(15, 5),
                        Width = 150,
                        AutoSizeAxes = Axes.Y,
                    },
                };
            }
            else
            {
                Contents.Clear();
                ButtonsContainer.Clear();
            }
        }

        public Button AddButton(string text, Action action)
        {
            Button b;
            ButtonsContainer.Add(b = new Button
            {
                BackgroundColour = Color4.DarkBlue,
                Size = new Vector2(150, 50),
                Text = text
            });

            b.Action += action;

            return b;
        }

        public ToggleButton AddToggle(string text, Action<bool> action)
        {
            ToggleButton b;
            ButtonsContainer.Add(b = new ToggleButton(action)
            {
                Size = new Vector2(150, 50),
                Text = text
            });
            return b;
        }
    }

    public class ToggleButton : Button
    {
        private readonly Action<bool> reloadCallback;
        private static readonly Color4 off_colour = Color4.Red;
        private static readonly Color4 on_colour = Color4.YellowGreen;

        public bool State;

        public ToggleButton(Action<bool> reloadCallback)
        {
            this.reloadCallback = reloadCallback;

            Size = new Vector2(100, 50);
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
