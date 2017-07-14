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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Testing.Drawables;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public class TestBrowser : Screen
    {
        public TestCase CurrentTest { get; private set; }

        private FillFlowContainer<TestCaseButton> leftFlowContainer;
        private Container testContentContainer;
        private Container compilingNotice;

        public readonly List<Type> TestTypes = new List<Type>();

        private ConfigManager<TestBrowserSetting> config;

        private DynamicClassCompiler backgroundCompiler;

        private bool interactive;

        public TestBrowser()
        {
            //we want to build the lists here because we're interested in the assembly we were *created* on.
            Assembly asm = Assembly.GetCallingAssembly();
            foreach (Type type in asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase)) && !t.IsAbstract))
                TestTypes.Add(type);

            TestTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage, GameHost host)
        {
            interactive = host.Window != null;
            config = new TestBrowserConfig(storage);

            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(200, 1),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.DimGray,
                            RelativeSizeAxes = Axes.Both
                        },
                        new ScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ScrollbarOverlapsContent = false,
                            Child = leftFlowContainer = new FillFlowContainer<TestCaseButton>
                            {
                                Padding = new MarginPadding(3),
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 5),
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                            }
                        }
                    }
                },
                testContentContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 200 },
                    Child = compilingNotice = new Container
                    {
                        Alpha = 0,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        Depth = float.MinValue,
                        CornerRadius = 5,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                            },
                            new SpriteText
                            {
                                TextSize = 30,
                                Text = @"Compiling new version..."
                            }
                        },
                    }
                }
            };

            //Add buttons for each TestCase.
            leftFlowContainer.AddRange(TestTypes.Select(t => new TestCaseButton(t) { Action = () => LoadTest(t) }));

            backgroundCompiler = new DynamicClassCompiler
            {
                CompilationStarted = compileStarted,
                CompilationFinished = compileFinished,
                WatchDirectory = @"Tests",
            };

            try
            {
                backgroundCompiler.Start();
            }
            catch
            {
                //it's okay for this to fail for now.
            }
        }

        private void compileStarted()
        {
            compilingNotice.Show();
            compilingNotice.FadeColour(Color4.White);
        }

        private void compileFinished(Type newType)
        {
            Schedule(() =>
            {
                compilingNotice.FadeOut(800, EasingTypes.InQuint);
                compilingNotice.FadeColour(newType == null ? Color4.Red : Color4.YellowGreen, 100);

                if (newType == null) return;

                int i = TestTypes.FindIndex(t => t.Name == newType.Name);
                TestTypes[i] = newType;
                LoadTest(i);
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (CurrentTest == null)
                LoadTest(TestTypes.Find(t => t.Name == config.Get<string>(TestBrowserSetting.LastTest)));
        }

        protected override bool OnExiting(Screen next)
        {
            if (next == null)
                Game?.Exit();

            return base.OnExiting(next);
        }

        public void LoadTest(int testIndex) => LoadTest(TestTypes[testIndex]);

        public void LoadTest(Type testType = null, Action onCompletion = null)
        {
            if (testType == null && TestTypes.Count > 0)
                testType = TestTypes[0];

            config.Set(TestBrowserSetting.LastTest, testType?.Name);

            if (CurrentTest != null)
            {
                testContentContainer.Remove(CurrentTest);
                CurrentTest.Clear();
                CurrentTest = null;
            }

            if (testType != null)
            {
                testContentContainer.Add(CurrentTest = (TestCase)Activator.CreateInstance(testType));
                if (!interactive) CurrentTest.OnLoadComplete = d => ((TestCase)d).RunAllSteps(onCompletion);
            }

            updateButtons();
        }

        private void updateButtons()
        {
            foreach (var b in leftFlowContainer.Children)
                b.Current = b.TestType.Name == CurrentTest.GetType().Name;
        }
    }
}
