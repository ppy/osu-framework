// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Screens.Testing
{
    public class TestBrowser : Screen
    {
        class TestBrowserConfig : ConfigManager<TestBrowserOption>
        {
            public override string Filename => @"visualtests.cfg";

            public TestBrowserConfig(Storage storage) : base(storage)
            {
            }
        }

        enum TestBrowserOption
        {
            LastTest
        }

        private Container leftContainer;
        private Container<Drawable> leftFlowContainer;
        private Container leftScrollContainer;
        private TestCase loadedTest;
        private Container testContainer;

        List<TestCase> testCases = new List<TestCase>();

        public int TestCount => testCases.Count;

        private List<TestCase> tests = new List<TestCase>();

        ConfigManager<TestBrowserOption> config;

        public TestBrowser()
        {
            //we want to build the lists here because we're interested in the assembly we were *created* on.
            Assembly asm = Assembly.GetCallingAssembly();
            foreach (Type type in asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase))))
                tests.Add((TestCase)Activator.CreateInstance(type));

            tests.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
            config = new TestBrowserConfig(storage);

            Add(leftContainer = new Container
            {
                RelativeSizeAxes = Axes.Y,
                Size = new Vector2(200, 1)
            });

            leftContainer.Add(new Box
            {
                Colour = Color4.DimGray,
                RelativeSizeAxes = Axes.Both
            });

            leftContainer.Add(leftScrollContainer = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                ScrollDraggerOverlapsContent = false
            });

            leftScrollContainer.Add(leftFlowContainer = new FillFlowContainer
            {
                Padding = new MarginPadding(3),
                Direction = FillDirection.Down,
                Spacing = new Vector2(0, 5),
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
            });

            //this is where the actual tests are loaded.
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Left = 200 }
            });

            foreach (var testCase in tests)
                addTest(testCase);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            loadTest(tests.Find(t => t.Name == config.Get<string>(TestBrowserOption.LastTest)));
        }

        protected override bool OnExiting(Screen next)
        {
            if (next == null)
                Game?.Exit();

            return base.OnExiting(next);
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

            config.Set(TestBrowserOption.LastTest, testCase?.Name);

            if (loadedTest != null)
            {
                testContainer.Remove(loadedTest);
                loadedTest.Clear();
                loadedTest = null;
            }

            if (testCase != null)
            {
                testContainer.Add(loadedTest = testCase);
                testCase.Reset();
            }
        }

        class TestCaseButton : ClickableContainer
        {
            private readonly Box box;

            public TestCaseButton(TestCase test)
            {
                Masking = true;
                CornerRadius = 5;
                RelativeSizeAxes = Axes.X;
                Size = new Vector2(1, 60);

                Add(new Drawable[]
                {
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(140, 140, 140, 255),
                        Alpha = 0.7f
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 4,
                            Right = 4,
                            Bottom = 2,
                        },
                        Children = new[]
                        {
                            new SpriteText
                            {
                                Text = test.Name,
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                            },
                            new SpriteText
                            {
                                Text = test.Description,
                                TextSize = 15,
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft
                            }
                        }
                    }
                });
            }

            protected override bool OnHover(InputState state)
            {
                box.FadeTo(1, 150);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                box.FadeTo(0.7f, 150);
                base.OnHoverLost(state);
            }

            protected override bool OnClick(InputState state)
            {
                box.FlashColour(Color4.White, 300);
                return base.OnClick(state);
            }
        }
    }
}
