// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public class TestBrowser : Screen
    {
        public TestCase CurrentTest;

        private class TestBrowserConfig : ConfigManager<TestBrowserOption>
        {
            protected override string Filename => @"visualtests.cfg";

            public TestBrowserConfig(Storage storage) : base(storage)
            {
            }
        }

        private enum TestBrowserOption
        {
            LastTest
        }

        private Container leftContainer;
        private FillFlowContainer<TestCaseButton> leftFlowContainer;
        private Container leftScrollContainer;
        private Container testContainer;
        private Container compilingNotice;

        public int TestCount => Tests.Count;

        public readonly List<TestCase> Tests = new List<TestCase>();

        private ConfigManager<TestBrowserOption> config;

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

            leftScrollContainer.Add(leftFlowContainer = new FillFlowContainer<TestCaseButton>
            {
                Padding = new MarginPadding(3),
                Direction = FillDirection.Vertical,
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

            testContainer.Add(compilingNotice = new Container
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
                    new Box {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                    },
                    new SpriteText
                    {
                        TextSize = 30,
                        Text = @"Compiling new version..."
                    }
                },
            });

            foreach (var t in Tests)
                addTest(t);

            try
            {
                startBackgroundCompiling();
            }
            catch
            {
                //it's okay for this to fail for now.
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            LoadTest(Tests.Find(t => t.Name == config.Get<string>(TestBrowserOption.LastTest)));
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
            button.Action += delegate { LoadTest(testCase); };
            leftFlowContainer.Add(button);
        }

        public void LoadTest(int testIndex) => LoadTest(Tests[testIndex]);

        public void LoadTest(TestCase testCase = null)
        {
            if (testCase == null && Tests.Count > 0)
                testCase = Tests[0];

            config.Set(TestBrowserOption.LastTest, testCase?.Name);

            if (CurrentTest != null)
            {
                testContainer.Remove(CurrentTest);
                CurrentTest.Clear();

                var button = buttonFor(CurrentTest);
                if (button != null) button.Current = false;

                CurrentTest = null;
            }

            if (testCase != null)
            {
                testContainer.Add(CurrentTest = testCase);
                testCase.Reset();

                var button = buttonFor(CurrentTest);
                if (button != null) button.Current = true;
            }
        }

        private TestCaseButton buttonFor(TestCase currentTest) => leftFlowContainer.Children.FirstOrDefault(b => b.TestCase.Name == currentTest.Name);

        private FileSystemWatcher fsw;

        /// <summary>
        /// Find the base path of the closest solution folder
        /// </summary>
        private static string findSolutionPath()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (!Directory.GetFiles(di.FullName, "*.sln").Any() && di.Parent != null)
                di = di.Parent;

            return di.FullName;
        }

        private static CSharpCodeProvider createCodeProvider()
        {
            var csc = new CSharpCodeProvider();

            var newPath = Path.Combine(findSolutionPath(), "packages", "Microsoft.Net.Compilers.2.0.1", "tools", "csc.exe");

            //Black magic to fix incorrect packaged path (http://stackoverflow.com/a/40311406)
            var settings = csc.GetType().GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(csc);
            var path = settings?.GetType().GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);
            path?.SetValue(settings, newPath);

            return csc;
        }

        private void startBackgroundCompiling()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            var basePath = Path.Combine(di.Parent?.Parent?.FullName, @"Tests");

            fsw = new FileSystemWatcher(basePath, @"*.cs")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };

            fsw.Changed += (sender, e) =>
            {
                CompilerParameters cp = new CompilerParameters
                {
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = false,
                    GenerateExecutable = false,
                };

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location);
                cp.ReferencedAssemblies.AddRange(assemblies.ToArray());

                string source;
                while (true)
                {
                    try
                    {
                        source = File.ReadAllText(e.FullPath);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }

                Logger.Log($@"Recompiling {e.Name}...", LoggingTarget.Runtime, LogLevel.Important);

                Schedule(() =>
                {
                    compilingNotice.Show();
                    compilingNotice.FadeColour(Color4.White);
                });

                TestCase newVersion = null;

                using (var provider = createCodeProvider())
                {
                    CompilerResults compile = provider.CompileAssemblyFromSource(cp, source);

                    if (compile.Errors.HasErrors)
                    {
                        string text = "Compile error: ";
                        foreach (CompilerError ce in compile.Errors)
                            text += "\r\n" + ce;

                        Logger.Log(text, LoggingTarget.Runtime, LogLevel.Error);
                    }
                    else
                    {
                        Module module = compile.CompiledAssembly.GetModules()[0];
                        if (module != null)
                            newVersion = (TestCase)Activator.CreateInstance(module.GetTypes()[0]);
                    }
                }

                Schedule(() =>
                {
                    compilingNotice.FadeOut(800, EasingTypes.InQuint);
                    compilingNotice.FadeColour(newVersion == null ? Color4.Red : Color4.YellowGreen, 100);
                });

                if (newVersion == null) return;

                Logger.Log(@"Complete!", LoggingTarget.Runtime, LogLevel.Important);

                Schedule(() =>
                {
                    int i = Tests.FindIndex(t => t.GetType().Name == newVersion.GetType().Name);
                    Tests[i] = newVersion;
                    LoadTest(i);
                });
            };
        }

        private class TestCaseButton : ClickableContainer
        {
            private readonly Box box;
            private readonly Container text;

            public readonly TestCase TestCase;

            public bool Current
            {
                set
                {
                    const float transition_duration = 100;

                    if (value)
                    {
                        box.FadeColour(new Color4(220, 220, 220, 255), transition_duration);
                        text.FadeColour(Color4.Black, transition_duration);
                    }
                    else
                    {
                        box.FadeColour(new Color4(140, 140, 140, 255), transition_duration);
                        text.FadeColour(Color4.White, transition_duration);
                    }
                }
            }

            public TestCaseButton(TestCase test)
            {
                Masking = true;

                TestCase = test;

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
                    text = new Container
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
        }
    }
}
