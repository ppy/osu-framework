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

        public readonly List<TestCase> Tests = new List<TestCase>();

        private ConfigManager<TestBrowserSetting> config;

        private DynamicClassCompiler<TestCase> backgroundCompiler;

        public TestBrowser()
        {
            //we want to build the lists here because we're interested in the assembly we were *created* on.
            Assembly asm = Assembly.GetCallingAssembly();
            foreach (Type type in asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase))))
                Tests.Add((TestCase)Activator.CreateInstance(type));

            Tests.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
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
                            ScrollDraggerOverlapsContent = false,
                            Children = new[]
                            {
                                leftFlowContainer = new FillFlowContainer<TestCaseButton>
                                {
                                    Padding = new MarginPadding(3),
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 5),
                                    AutoSizeAxes = Axes.Y,
                                    RelativeSizeAxes = Axes.X,
                                }
                            }
                        }
                    }
                },
                testContentContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 200 },
                    Children = new Drawable[]
                    {
                        compilingNotice = new Container
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
                }
            };

            //Add buttons for each TestCase.
            leftFlowContainer.Add(Tests.Select(t => new TestCaseButton(t) { Action = () => LoadTest(t) }));

            backgroundCompiler = new DynamicClassCompiler<TestCase>()
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

        private void compileFinished(TestCase newVersion)
        {
            Schedule(() =>
            {
                compilingNotice.FadeOut(800, EasingTypes.InQuint);
                compilingNotice.FadeColour(newVersion == null ? Color4.Red : Color4.YellowGreen, 100);

                if (newVersion == null) return;

                int i = Tests.FindIndex(t => t.GetType().Name == newVersion.GetType().Name);
                Tests[i] = newVersion;
                LoadTest(i);
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (CurrentTest == null)
                LoadTest(Tests.Find(t => t.Name == config.Get<string>(TestBrowserSetting.LastTest)));
        }

        protected override bool OnExiting(Screen next)
        {
            if (next == null)
                Game?.Exit();

            return base.OnExiting(next);
        }

        public void LoadTest(int testIndex) => LoadTest(Tests[testIndex]);

        public void LoadTest(TestCase testCase = null, Action onCompletion = null)
        {
            if (testCase == null && Tests.Count > 0)
                testCase = Tests[0];

            config.Set(TestBrowserSetting.LastTest, testCase?.Name);

            if (CurrentTest != null)
            {
                testContentContainer.Remove(CurrentTest);
                CurrentTest.Clear();

                var button = getButtonFor(CurrentTest);
                if (button != null) button.Current = false;

                CurrentTest = null;
            }

            if (testCase != null)
            {
                testContentContainer.Add(CurrentTest = testCase);
                testCase.Reset();
                testCase.RunAllSteps(onCompletion);

                var button = getButtonFor(CurrentTest);
                if (button != null) button.Current = true;
            }
        }

        private TestCaseButton getButtonFor(TestCase currentTest) => leftFlowContainer.Children.FirstOrDefault(b => b.TestCase.Name == currentTest.Name);
    }
}
