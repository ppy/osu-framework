// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Testing
{
    public class TestBrowser : KeyBindingContainer<TestBrowserAction>, IKeyBindingHandler<TestBrowserAction>
    {
        public TestCase CurrentTest { get; private set; }

        private TextBox searchTextBox;
        private SearchContainer<TestCaseButton> leftFlowContainer;
        private Container testContentContainer;
        private Container compilingNotice;

        public readonly List<Type> TestTypes = new List<Type>();

        private ConfigManager<TestBrowserSetting> config;

        private DynamicClassCompiler<TestCase> backgroundCompiler;

        private bool interactive;

        private readonly List<Assembly> assemblies;

        /// <summary>
        /// Creates a new TestBrowser that displays the TestCases of every assembly that start with either "osu" or the specified namespace (if it isn't null)
        /// </summary>
        /// <param name="assemblyNamespace">Assembly prefix which is used to match assemblies whose tests should be displayed</param>
        public TestBrowser(string assemblyNamespace = null)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.FullName.StartsWith("osu") || assemblyNamespace != null && n.FullName.StartsWith(assemblyNamespace)).ToList();

            //we want to build the lists here because we're interested in the assembly we were *created* on.
            foreach (Assembly asm in assemblies.ToList())
            {
                var tests = asm.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(TestCase)) && !t.IsAbstract && t.IsPublic).ToList();

                if (!tests.Any())
                {
                    assemblies.Remove(asm);
                    continue;
                }

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

        private BindableDouble rateBindable;

        private Toolbar toolbar;
        private Container leftContainer;
        private Container mainContainer;

        private const float test_list_width = 200;

        private Action exit;

        private Bindable<bool> showLogOverlay;

        [BackgroundDependencyLoader]
        private void load(Storage storage, GameHost host, FrameworkConfigManager frameworkConfig)
        {
            interactive = host.Window != null;
            config = new TestBrowserConfig(storage);

            exit = host.Exit;

            showLogOverlay = frameworkConfig.GetBindable<bool>(FrameworkSetting.ShowLogOverlay);

            rateBindable = new BindableDouble(1)
            {
                MinValue = 0,
                MaxValue = 2,
            };

            var rateAdjustClock = new StopwatchClock(true);
            var framedClock = new FramedClock(rateAdjustClock);

            Children = new Drawable[]
            {
                leftContainer = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(test_list_width, 1),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.DimGray,
                            RelativeSizeAxes = Axes.Both
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Vertical,
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                searchTextBox = new TextBox
                                {
                                    OnCommit = delegate
                                    {
                                        var firstVisible = leftFlowContainer.FirstOrDefault(b => b.IsPresent);
                                        if (firstVisible != null)
                                            LoadTest(firstVisible.TestType);
                                    },
                                    Height = 20,
                                    RelativeSizeAxes = Axes.X,
                                    PlaceholderText = "type to search"
                                },
                                new ScrollContainer
                                {
                                    Padding = new MarginPadding { Top = 3, Bottom = 20 },
                                    RelativeSizeAxes = Axes.Both,
                                    ScrollbarOverlapsContent = false,
                                    Child = leftFlowContainer = new SearchContainer<TestCaseButton>
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
                    }
                },
                mainContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = test_list_width },
                    Children = new Drawable[]
                    {
                        toolbar = new Toolbar
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 50,
                            Depth = -1,
                        },
                        testContentContainer = new Container
                        {
                            Clock = framedClock,
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

            searchTextBox.Current.ValueChanged += newValue => leftFlowContainer.SearchTerm = newValue;

            backgroundCompiler = new DynamicClassCompiler<TestCase>
            {
                CompilationStarted = compileStarted,
                CompilationFinished = compileFinished,
                CompilationFailed = compileFailed
            };
            try
            {
                backgroundCompiler.Start();
            }
            catch
            {
                //it's okay for this to fail for now.
            }

            foreach (Assembly asm in assemblies)
                toolbar.AssemblyDropdown.AddDropdownItem(asm.GetName().Name, asm);

            toolbar.AssemblyDropdown.Current.ValueChanged += updateList;
            toolbar.RunAllSteps.Current.ValueChanged += v => runTests(null);
            toolbar.RateAdjustSlider.Current.BindTo(rateBindable);

            rateBindable.ValueChanged += v => rateAdjustClock.Rate = v;
            rateBindable.TriggerChange();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            backgroundCompiler?.Dispose();
        }

        private void compileStarted() => Schedule(() =>
        {
            compilingNotice.Show();
            compilingNotice.FadeColour(Color4.White);
        });

        private void compileFailed(Exception ex) => Schedule(() => {
            showLogOverlay.Value = true;
            Logger.Error(ex, "Error with dynamic compilation!");

            compilingNotice.FadeIn(100, Easing.OutQuint).Then().FadeOut(800, Easing.InQuint);
            compilingNotice.FadeColour(Color4.Red, 100);
        });

        private void compileFinished(Type newType) => Schedule(() =>
        {
            compilingNotice.FadeOut(800, Easing.InQuint);
            compilingNotice.FadeColour(Color4.YellowGreen, 100);

            int i = TestTypes.FindIndex(t => t.Name == newType.Name && t.Assembly.GetName().Name == newType.Assembly.GetName().Name);

            if (i < 0)
                TestTypes.Add(newType);
            else
                TestTypes[i] = newType;

            try
            {
                LoadTest(newType, isDynamicLoad: true);
            }
            catch (Exception e)
            {
                compileFailed(e);
            }
        });

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (CurrentTest == null)
                LoadTest(TestTypes.Find(t => t.Name == config.Get<string>(TestBrowserSetting.LastTest)));
        }

        private void toggleTestList()
        {
            if (leftContainer.Width > 0)
            {
                leftContainer.Width = 0;
                mainContainer.Padding = new MarginPadding();
            }
            else
            {
                leftContainer.Width = test_list_width;
                mainContainer.Padding = new MarginPadding { Left = test_list_width };
            }
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (!args.Repeat)
            {
                switch (args.Key)
                {
                    case Key.Escape:
                        exit();
                        return true;
                }
            }

            return base.OnKeyDown(state, args);
        }

        public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new [] {InputKey.Control, InputKey.F},TestBrowserAction.Search),
            new KeyBinding(new [] {InputKey.Control, InputKey.R},TestBrowserAction.Reload), // for macOS

            new KeyBinding(new [] {InputKey.Super, InputKey.F},TestBrowserAction.Search), // for macOS
            new KeyBinding(new [] {InputKey.Super, InputKey.R},TestBrowserAction.Reload), // for macOS

            new KeyBinding(new [] {InputKey.Control, InputKey.H},TestBrowserAction.ToggleTestList),
        };

        public bool OnPressed(TestBrowserAction action)
        {
            switch (action)
            {
                case TestBrowserAction.Search:
                    if (leftContainer.Width == 0) toggleTestList();
                    GetContainingInputManager().ChangeFocus(searchTextBox);
                    return true;
                case TestBrowserAction.Reload:
                    LoadTest(CurrentTest.GetType());
                    return true;
                case TestBrowserAction.ToggleTestList:
                    toggleTestList();
                    return true;
            }

            return false;
        }

        public bool OnReleased(TestBrowserAction action) => false;

        public void LoadTest(int testIndex) => LoadTest(TestTypes[testIndex]);

        public void LoadTest(Type testType = null, Action onCompletion = null, bool isDynamicLoad = false)
        {
            if (testType == null && TestTypes.Count > 0)
                testType = TestTypes[0];

            config.Set(TestBrowserSetting.LastTest, testType?.Name ?? string.Empty);

            var lastTest = CurrentTest;

            if (testType == null)
                return;

            var newTest = (TestCase)Activator.CreateInstance(testType);

            var dropdown = toolbar.AssemblyDropdown;

            const string dynamic = "dynamic";

            dropdown.RemoveDropdownItem(dropdown.Items.LastOrDefault(i => i.Value.FullName.Contains(dynamic)).Value);

            // if we are a dynamically compiled type (via DynamicClassCompiler) we should update the dropdown accordingly.
            if (isDynamicLoad)
                dropdown.AddDropdownItem($"{dynamic} ({testType.Name})", testType.Assembly);
            else
                TestTypes.RemoveAll(t => t.Assembly.FullName.Contains(dynamic));

            dropdown.Current.Value = testType.Assembly;

            CurrentTest = newTest;

            updateButtons();

            testContentContainer.Add(new ErrorCatchingDelayedLoadWrapper(CurrentTest, isDynamicLoad)
            {
                OnCaughtError = compileFailed
            });

            newTest.OnLoadComplete = d => Schedule(() =>
            {
                if (lastTest?.Parent != null)
                {
                    testContentContainer.Remove(lastTest.Parent);
                    lastTest.Clear();
                    lastTest.Dispose();
                }

                if (CurrentTest != newTest)
                {
                    // There could have been multiple loads fired after us. In such a case we want to silently remove ourselves.
                    testContentContainer.Remove(newTest.Parent);
                    return;
                }

                updateButtons();

                var methods = testType.GetMethods();

                var setUpMethods = methods.Where(m => m.GetCustomAttributes(typeof(SetUpAttribute), false).Length > 0);

                foreach (var m in methods.Where(m => m.Name != "TestConstructor" && m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0))
                {
                    var step = CurrentTest.AddStep($"Setup: {m.Name}", () => setUpMethods.ForEach(s => s.Invoke(CurrentTest, null)));
                    step.LightColour = Color4.Teal;
                    m.Invoke(CurrentTest, null);
                }

                backgroundCompiler.Checkpoint(CurrentTest);
                runTests(onCompletion);
                updateButtons();
            });
        }

        private void runTests(Action onCompletion)
        {
            if (!interactive || toolbar.RunAllSteps.Current)
                CurrentTest.RunAllSteps(onCompletion, e => Logger.Log($@"Error on step: {e}"));
            else
                CurrentTest.RunFirstStep();
        }

        private void updateButtons()
        {
            foreach (var b in leftFlowContainer.Children)
                b.Current = b.TestType.Name == CurrentTest?.GetType().Name;
        }

        private class ErrorCatchingDelayedLoadWrapper : DelayedLoadWrapper
        {
            private readonly bool catchErrors;

            public Action<Exception> OnCaughtError;

            public ErrorCatchingDelayedLoadWrapper(Drawable content, bool catchErrors)
                : base(content, 0)
            {
                this.catchErrors = catchErrors;
            }

            public override bool UpdateSubTree()
            {
                try
                {
                    return base.UpdateSubTree();
                }
                catch (Exception e)
                {
                    if (!catchErrors)
                        throw;

                    OnCaughtError?.Invoke(e);
                    RemoveInternal(Content);
                }

                return false;
            }
        }

        private class Toolbar : CompositeDrawable
        {
            public BasicSliderBar<double> RateAdjustSlider;

            public BasicDropdown<Assembly> AssemblyDropdown;

            public BasicCheckbox RunAllSteps;

            [BackgroundDependencyLoader]
            private void load()
            {
                SpriteText playbackSpeedDisplay;
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                    },
                    new Container
                    {
                        Padding = new MarginPadding(10),
                        RelativeSizeAxes = Axes.Both,
                        Child = new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.Distributed),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        Spacing = new Vector2(5),
                                        Direction = FillDirection.Horizontal,
                                        RelativeSizeAxes = Axes.Y,
                                        AutoSizeAxes = Axes.X,
                                        Children = new Drawable[]
                                        {
                                            new SpriteText
                                            {
                                                Padding = new MarginPadding(5),
                                                Text = "Current Assembly:"
                                            },
                                            AssemblyDropdown = new BasicDropdown<Assembly>
                                            {
                                                Width = 300,
                                            },
                                            RunAllSteps = new BasicCheckbox
                                            {
                                                LabelText = "Run all steps",
                                                LabelPadding = new MarginPadding { Left = 5, Right = 10 },
                                                AutoSizeAxes = Axes.Y,
                                                Width = 140,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                            },
                                        }
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        ColumnDimensions = new[]
                                        {
                                            new Dimension(GridSizeMode.AutoSize),
                                            new Dimension(GridSizeMode.Distributed),
                                            new Dimension(GridSizeMode.AutoSize),
                                        },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                new SpriteText
                                                {
                                                    Padding = new MarginPadding(5),
                                                    Text = "Rate:"
                                                },
                                                RateAdjustSlider = new BasicSliderBar<double>
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Color4.MediumPurple,
                                                    SelectionColor = Color4.White,
                                                },
                                                playbackSpeedDisplay = new SpriteText
                                                {
                                                    Padding = new MarginPadding(5),
                                                },
                                            }
                                        }
                                    }
                                }
                            },
                        },
                    },
                };

                RateAdjustSlider.Current.ValueChanged += v => playbackSpeedDisplay.Text = v.ToString("0%");
            }
        }
    }

    public enum TestBrowserAction
    {
        ToggleTestList,
        Reload,
        Search
    }
}
