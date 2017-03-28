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
using osu.Framework.Graphics.Transforms;
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
        private Container<Drawable> leftFlowContainer;
        private Container leftScrollContainer;
        private TestCase loadedTest;
        private Container testContainer;
        private Container compilingNotice;

        private readonly List<TestCase> testCases = new List<TestCase>();

        public int TestCount => testCases.Count;

        private readonly List<TestCase> tests = new List<TestCase>();

        private ConfigManager<TestBrowserOption> config;

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

            foreach (var testCase in tests)
                addTest(testCase);

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

        private FileSystemWatcher fsw;

        /// <summary>
        /// Find the base path of the closest solution folder
        /// </summary>
        private static string findSolutionPath()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (!Directory.GetFiles(di.FullName, "*.sln").Any())
                di = di.Parent;
            return di.FullName;
        }

        private static CSharpCodeProvider createCodeProvider()
        {
            var csc = new CSharpCodeProvider();

            var newPath = Path.Combine(findSolutionPath(), "packages", "Microsoft.Net.Compilers.1.3.2", "tools", "csc.exe");

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
                    int i = testCases.FindIndex(t => t.GetType().Name == newVersion.GetType().Name);
                    testCases[i] = newVersion;
                    LoadTest(i);
                });
            };
        }

        private class TestCaseButton : ClickableContainer
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
