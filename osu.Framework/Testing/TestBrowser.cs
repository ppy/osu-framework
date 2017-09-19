﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
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

        private DynamicClassCompiler<TestCase> backgroundCompiler;

        private bool interactive;

        private readonly BasicDropdown<Assembly> assemblyDropdown;

        public TestBrowser()
        {
            assemblyDropdown = new BasicDropdown<Assembly>
            {
                Width = 400,
            };

            assemblyDropdown.Current.ValueChanged += updateList;

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

            var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*Test*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
            toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

            //we want to build the lists here because we're interested in the assembly we were *created* on.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies().Where(n => n.FullName.StartsWith("osu")))
            {
                var tests = asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase)) && !t.IsAbstract).ToList();

                if (!tests.Any())
                    continue;

                assemblyDropdown.AddDropdownItem(asm.GetName().Name, asm);
                foreach (Type type in tests)
                    TestTypes.Add(type);
            }

            TestTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        private void updateList(Assembly asm)
        {
            leftFlowContainer.Clear();
            //Add buttons for each TestCase.
            leftFlowContainer.AddRange(TestTypes.Where(t => t.Assembly == asm).Select(t => new TestCaseButton(t) { Action = () => LoadTest(t) }));
        }

        private Button runAllButton;

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
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 200 },
                    Children = new[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 50,
                            Depth = -1,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Black,
                                },
                                new FillFlowContainer
                                {
                                    Spacing = new Vector2(5),
                                    Direction = FillDirection.Horizontal,
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(10),
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Padding = new MarginPadding(5),
                                            Text = "Current Assembly:"
                                        },
                                        assemblyDropdown,
                                        runAllButton = new Button
                                        {
                                            Text = "Run all steps",
                                            BackgroundColour = Color4.MediumPurple,
                                            Action = delegate
                                            {
                                                runAllButton.Enabled.Value = false;
                                                runAllButton.BackgroundColour = Color4.DimGray;
                                                CurrentTest.RunAllSteps(() => runAllComplete(), e => runAllComplete(true));
                                            },
                                            Width = 150,
                                            RelativeSizeAxes = Axes.Y,
                                        },
                                    }
                                }
                            }
                        },
                        testContentContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 50 },
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
                    }
                }
            };

            backgroundCompiler = new DynamicClassCompiler<TestCase>
            {
                CompilationStarted = compileStarted,
                CompilationFinished = compileFinished,
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

        private void runAllComplete(bool error = false)
        {
            runAllButton.Enabled.Value = true;
            runAllButton.BackgroundColour = error ? Color4.Red : Color4.MediumPurple;
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
                compilingNotice.FadeOut(800, Easing.InQuint);
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
            runAllComplete();

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
                assemblyDropdown.RemoveDropdownItem(assemblyDropdown.Items.LastOrDefault(i => i.Value.CodeBase.Contains("DotNetCompiler")).Value);

                // if we are a dynamically compiled type (via DynamicClassCompiler) we should update the dropdown accordingly.
                if (testType.Assembly.CodeBase.Contains("DotNetCompiler"))
                    assemblyDropdown.AddDropdownItem($"dynamic ({testType.Name})", testType.Assembly);

                assemblyDropdown.Current.Value = testType.Assembly;

                testContentContainer.Add(CurrentTest = (TestCase)Activator.CreateInstance(testType));
                if (!interactive) CurrentTest.OnLoadComplete = d => ((TestCase)d).RunAllSteps(onCompletion);

                var methods = testType.GetMethods();

                var setUpMethod = methods.FirstOrDefault(m => m.GetCustomAttributes(typeof(SetUpAttribute), false).Length > 0);

                foreach (var m in methods.Where(m => m.Name != "TestConstructor" && m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0))
                {
                    var step = CurrentTest.AddStep(m.Name, () => { setUpMethod?.Invoke(CurrentTest, null); });
                    step.BackgroundColour = Color4.Teal;
                    m.Invoke(CurrentTest, null);
                }

                backgroundCompiler.Checkpoint(CurrentTest);

                CurrentTest.RunFirstStep();
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
