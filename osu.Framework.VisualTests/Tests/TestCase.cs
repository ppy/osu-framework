//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCase : LargeContainer
    {
        internal virtual string Name => @"Test Case";
        internal virtual string Description => @"The base class for a test case";
        internal virtual int DisplayOrder => 0;

        Container buttonsContainer = new FlowContainer()
        {
            Direction = FlowDirection.VerticalOnly,
            Padding = new Vector2(15, 5)
        };

        internal virtual void Reset()
        {
            Clear();
            buttonsContainer.Clear();

            ScrollContainer scroll = new ScrollContainer() { Depth = 0 };
            scroll.Add(buttonsContainer);
            Add(scroll);
        }

        internal Button AddButton(string text, Action action)
        {
            Button b;
            buttonsContainer.Add(b = new Button()
            {
                Colour = Color4.LightBlue,
                Size = new Vector2(100, 50),
                Text = text
            });

            b.Click += action;

            return b;
        }

        internal ToggleButton AddToggle(string text, Action action)
        {
            return buttonsContainer.Add(new ToggleButton(action) { Text = text }) as ToggleButton;
        }
    }

    class ToggleButton : Button
    {
        private readonly Action reloadCallback;
        private static Color4 offColour = Color4.Red;
        private static Color4 onColour = Color4.YellowGreen;

        internal bool State = false;

        public ToggleButton(Action reloadCallback)
        {
            Size = new Vector2(100, 50);
            Colour = offColour;

            Click += clickAction;

            this.reloadCallback = reloadCallback;
        }

        private void clickAction()
        {
            State = !State;
            Colour = State ? onColour : offColour;
            reloadCallback?.Invoke();
        }
    }
}
