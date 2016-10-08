// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.GameModes.Testing
{
    public class TestBrowser : GameMode
    {
        private Container leftContainer;
        private Container leftFlowContainer;
        private Container leftScrollContainer;
        private TestCase loadedTest;
        private Container testContainer;

        List<TestCase> testCases = new List<TestCase>();

        private DrawVisualiser drawVis;

        public int TestCount => testCases.Count;

        private List<TestCase> tests = new List<TestCase>();

        public TestBrowser()
        {
            //we want to build the lists here because we're interested in the assembly we were *created* on.
            Assembly asm = Assembly.GetCallingAssembly();
            foreach (Type type in asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase))))
                tests.Add((TestCase)Activator.CreateInstance(type));
        }

        public override void Load()
        {
            base.Load();

            Add(leftContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.15f, 1)
            });

            leftContainer.Add(new Box
            {
                Colour = Color4.DimGray,
                RelativeSizeAxes = Axes.Both
            });

            leftContainer.Add(leftScrollContainer = new ScrollContainer());

            leftScrollContainer.Add(leftFlowContainer = new FlowContainer
            {
                Direction = FlowDirection.VerticalOnly,
                RelativeSizeAxes = Axes.X,
                Padding = new Vector2(0, 5)
            });

            //this is where the actual tests are loaded.
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.85f, 1),
                RelativePositionAxes = Axes.Both,
                Position = new Vector2(0.15f, 0),
            });

            testContainer.Size = new Vector2(0.6f, 1);
            Add(drawVis = new DrawVisualiser()
            {
                RelativePositionAxes = Axes.Both,
                Position = new Vector2(0.75f, 0),
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.25f, 1)
            });

            tests.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
            foreach (var testCase in tests)
                addTest(testCase);

            loadTest();
        }

        private void addTest(TestCase testCase)
        {
            TestCaseButton button = new TestCaseButton(testCase);
            button.Action += delegate { loadTest(testCase); };
            leftFlowContainer.Add(button);
            testCases.Add(testCase);
        }

        public void LoadTest(int testIndex)
        {
            loadTest(testCases[testIndex]);
        }

        private void loadTest(TestCase testCase = null)
        {
            if (testCase == null && testCases.Count > 0)
                testCase = testCases[0];

            if (loadedTest != null)
                testContainer.Remove(loadedTest);

            if (testCase != null)
            {
                testContainer.Add(loadedTest = testCase);
                testCase.Reset();

                drawVis.Target = testCase.Contents;
            }
        }

        class TestCaseButton : ClickableContainer
        {
            private Box box;
            private TestCase test;

            public TestCaseButton(TestCase test)
            {
                this.test = test;
            }

            public override void Load()
            {
                base.Load();

                RelativeSizeAxes = Axes.X;
                Size = new Vector2(1, 60);

                Add(box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.2f
                });

                Add(new SpriteText
                {
                    Text = test.Name,
                    RelativeSizeAxes = Axes.X,
                    //TextBold = true
                });

                Add(new SpriteText
                {
                    Text = test.Description,
                    TextSize = 15,
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft
                });
            }

            protected override bool OnHover(InputState state)
            {
                box.FadeTo(0.4f, 150);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                box.FadeTo(0.2f, 150);
                base.OnHoverLost(state);
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                box.FlashColour(Color4.White, 50);
                return true;
            }
        }
    }
}
