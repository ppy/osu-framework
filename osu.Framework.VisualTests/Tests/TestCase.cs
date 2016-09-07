//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System;
using System.Diagnostics;
using osu.Framework.Graphics.Containers;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCase : LargeContainer
    {
        internal virtual string Name => @"Test Case";
        internal virtual string Description => @"The base class for a test case";
        internal virtual int DisplayOrder => 0;

        Container buttonsContainer = new FlowContainer(FlowDirection.VerticalOnly) { Padding = new Vector2(15, 5) };

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
            return buttonsContainer.Add(new Button(text, Vector2.Zero, new Vector2(100, 50), 0, Color4.LightBlue, delegate
            {
                action?.Invoke();
                return true;
            }, 12)) as Button;
        }

        internal ToggleButton AddToggle(string text, Action action)
        {
            return buttonsContainer.Add(new ToggleButton(text, Vector2.Zero, action)) as ToggleButton;
        }
    }

    class ToggleButton : Button
    {
        private readonly Action reloadCallback;
        private static Color4 offColour = Color4.Red;
        private static Color4 onColour = Color4.YellowGreen;

        internal bool State = false;

        public ToggleButton(string text, Vector2 position, Action reloadCallback)
            : base(text, position, new Vector2(100, 50), 0, offColour, clickAction, 12, false)
        {
            this.reloadCallback = reloadCallback;
        }

        private static bool clickAction(object sender, EventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;
            Debug.Assert(tb != null);

            tb.State = !tb.State;
            tb.SetColour(tb.State ? onColour : offColour);
            tb.reloadCallback?.Invoke();
            return true;
        }
    }
}
