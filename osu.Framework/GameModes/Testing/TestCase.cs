// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;

namespace osu.Framework.GameModes.Testing
{
    public abstract class TestCase : Container
    {
        public override string Name => @"Test Case";
        public virtual string Description => @"The base class for a test case";
        public virtual int DisplayOrder => 0;

        Container buttonsContainer;

        public Container Contents;

        protected override Container AddTarget => Contents;

        public TestCase()
        {
            RelativeCoords = Axis.Both;
        }

        public virtual void Reset()
        {

            if (Contents == null)
            {
                AddTopLevel(new ScrollContainer
                {
                    Children = new[]
                    {
                        buttonsContainer = new FlowContainer
                        {
                            Direction = FlowDirection.VerticalOnly,
                            Padding = new Vector2(15, 5)
                        }
                    }
                });

                AddTopLevel(Contents = new Container()
                {
                    RelativeCoords = Axis.Both,
                });
            }
            else
            {
                Contents.Clear();
                buttonsContainer.Clear();
            }
        }

        public Button AddButton(string text, Action action)
        {
            Button b;
            buttonsContainer.Add(b = new Button
            {
                Colour = Color4.LightBlue,
                Size = new Vector2(100, 50),
                Text = text
            });

            b.Action += action;

            return b;
        }

        public ToggleButton AddToggle(string text, Action action)
        {
            return buttonsContainer.Add(new ToggleButton(action)
            {
                Text = text
            }) as ToggleButton;
        }
    }

    public class ToggleButton : Button
    {
        private readonly Action reloadCallback;
        private static Color4 offColour = Color4.Red;
        private static Color4 onColour = Color4.YellowGreen;

        public bool State;

        public ToggleButton(Action reloadCallback)
        {
            this.reloadCallback = reloadCallback;
        }

        public override void Load()
        {
            base.Load();

            Size = new Vector2(100, 50);
            Colour = offColour;
            Action += clickAction;
        }

        private void clickAction()
        {
            State = !State;
            Colour = State ? onColour : offColour;
            reloadCallback?.Invoke();
        }
    }
}
