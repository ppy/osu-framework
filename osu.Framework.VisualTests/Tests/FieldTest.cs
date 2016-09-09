//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Input;

namespace osu.Framework.VisualTests.Tests
{
    class FieldTest : LargeContainer
    {
        private Container leftContainer;
        private Container leftFlowContainer;
        private Container leftScrollContainer;
        private TestCase loadedTest;
        private Container testContainer;

        List<TestCase> testCases = new List<TestCase>();

        public override void Load()
        {
            base.Load();

            Add(leftContainer = new Container()
            {
                SizeMode = InheritMode.XY,
                Size = new Vector2(0.15f, 1)
            });

            leftContainer.Add(new Box()
            {
                Colour = Color4.DimGray,
                SizeMode = InheritMode.XY
            });

            leftContainer.Add(leftScrollContainer = new ScrollContainer());

            leftScrollContainer.Add(leftFlowContainer = new FlowContainer()
            {
                SizeMode = InheritMode.XY,
                Direction = FlowDirection.VerticalOnly,
                Padding = new Vector2(0, 5)
            });

            //this is where the actual tests are loaded.
            Add(testContainer = new LargeContainer()
            {
                Size = new Vector2(0.85f, 1),
                PositionMode = InheritMode.XY,
                Position = new Vector2(0.15f, 0),
            });

#if DEBUG
            testContainer.Size = new Vector2(0.6f, 1);
            Add(new DrawVisualiser(testContainer)
            {
                PositionMode = InheritMode.XY,
                Position = new Vector2(0.75f, 0),
                Size = new Vector2(0.25f, 1)
            });
#endif

            List<TestCase> tests = new List<TestCase>();

            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (Type type in asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase))))
                tests.Add((TestCase)Activator.CreateInstance(type));

            tests.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
            tests.ForEach(addTest);

            loadTest();
        }

        private void addTest(TestCase testCase)
        {
            TestCaseButton button = new TestCaseButton(testCase);
            button.Click += delegate { loadTest(testCase); };
            leftFlowContainer.Add(button);
            testCases.Add(testCase);
        }

        private void loadTest(TestCase testCase = null)
        {
            if (testCase == null) testCase = testCases[0];

            if (loadedTest != null)
                testContainer.Remove(loadedTest);

            testContainer.Add(loadedTest = testCase);
            testCase.Reset();
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

                SizeMode = InheritMode.X;
                Size = new Vector2(1, 80);

                Add(box = new Box()
                {
                    SizeMode = InheritMode.XY,
                    Alpha = 0.2f
                });

                FlowContainer flow;
                Add(flow = new FlowContainer()
                {
                    Direction = FlowDirection.VerticalOnly,
                    SizeMode = InheritMode.X
                });

                flow.Add(new SpriteText()
                {
                    Text = test.Name,
                    SizeMode = InheritMode.X,
                    //TextBold = true
                });

                flow.Add(new SpriteText()
                {
                    Text = test.Description,
                    SizeMode = InheritMode.X,
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
