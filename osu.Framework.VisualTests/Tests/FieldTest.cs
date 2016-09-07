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

        public FieldTest() : base(0.2f)
        {
            Add(new BackButton(delegate { Game.ChangeMode(OsuModes.Menu); }));

            Add(leftContainer = new Container() { SizeMode = InheritMode.XY Size = new Vector2(0.15f, 1) });
            leftContainer.Add(new Box(Color4.DimGray) { SizeMode = InheritMode.XY});
            leftContainer.Add(leftScrollContainer = new ScrollContainer()
            {
                Depth = 2
            });

            leftScrollContainer.Add(leftFlowContainer = new FlowContainer(FlowDirection.VerticalOnly) { Padding = new Vector2(0, 5) });

            //this is where the actual tests are loaded.
            Add(testContainer = new Container()
            {
                SizeMode = InheritMode.XY
                Size = new Vector2(0.85f, 1),
                PositionMode = PositionMode.Inherit,
                Position = new Vector2(0.15f, 0),
            });

#if DEBUG
            testContainer.Size = new Vector2(0.6f, 1);
            Add(new DrawVisualiser(testContainer)
            {
                PositionMode = PositionMode.Inherit,
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
            TestCaseItem item = new TestCaseItem(testCase);
            item.ObsoleteOnClick += delegate
            {
                loadTest(testCase);
                return true;
            };
            leftFlowContainer.Add(item);
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

        class TestCaseItem : Container
        {
            private Box box;

            public TestCaseItem(TestCase test)
            {
                Add(box = new Box(Color4.Gray)
                {
                    SizeMode = InheritMode.XY | InheritMode.FixedY,
                    Size = new Vector2(1, 30),
                    Depth = 0
                });
                Add(new SpriteText(test.Name, 9, Vector2.Zero, 1, Color4.White)
                {
                    TextBold = true
                });

                Add(new SpriteText(test.Description, 9, Vector2.Zero, 1, Color4.White, new Vector2(150, 0))
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                });

                box.ObsoleteOnHover += Box_OnHover;
                box.ObsoleteOnHoverLost += Box_OnHoverLost;
                box.ObsoleteOnMouseDown += Box_OnClick;
            }

            private bool Box_OnClick(object sender, EventArgs e)
            {
                box.FlashColour(Color4.White, 50);
                return false;
            }

            private bool Box_OnHoverLost(object sender, EventArgs e)
            {
                box.FadeColour(Color4.Gray, 150);
                return true;
            }

            private bool Box_OnHover(object sender, EventArgs e)
            {
                box.FadeColour(Color4.LightGray, 150);
                return true;
            }
        }
    }
}
