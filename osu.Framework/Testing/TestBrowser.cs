// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
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
using osu.Framework.Input.Events;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Testing
{
    [Cached]
    public class TestBrowser : KeyBindingContainer<TestBrowserAction>, IKeyBindingHandler<TestBrowserAction>, IHandleGlobalKeyboardInput
    {
        public TestScene CurrentTest { get; private set; }

        private BasicTextBox searchTextBox;
        private SearchContainer<TestGroupButton> leftFlowContainer;
        private Container testContentContainer;
        private Container compilingNotice;

        public readonly List<Type> TestTypes = new List<Type>();

        private ConfigManager<TestBrowserSetting> config;

        private DynamicClassCompiler<TestScene> backgroundCompiler;

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
                var tests = asm.GetLoadableTypes().Where(isValidVisualTest).ToList();

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

        private bool isValidVisualTest(Type t) => t.IsSubclassOf(typeof(TestScene)) && !t.IsAbstract && t.IsPublic && !t.GetCustomAttributes<HeadlessTestAttribute>().Any();

        private void updateList(ValueChangedEvent<Assembly> args)
        {
            leftFlowContainer.Clear();

            //Add buttons for each TestCase.
            string namespacePrefix = TestTypes.Select(t => t.Namespace).GetCommonPrefix();

            leftFlowContainer.AddRange(TestTypes.Where(t => t.Assembly == args.NewValue)
                                                .GroupBy(
                                                    t =>
                                                    {
                                                        string group = t.Namespace?.Substring(namespacePrefix.Length).TrimStart('.');
                                                        return string.IsNullOrWhiteSpace(group) ? "Ungrouped" : group;
                                                    },
                                                    t => t,
                                                    (group, types) => new TestGroup { Name = group, TestTypes = types.ToArray() }
                                                ).OrderBy(g => g.Name)
                                                .Select(t => new TestGroupButton(type => LoadTest(type), t)));
        }

        internal readonly BindableDouble PlaybackRate = new BindableDouble(1) { MinValue = 0, MaxValue = 2, Default = 1 };
        internal readonly Bindable<Assembly> Assembly = new Bindable<Assembly>();
        internal readonly Bindable<bool> RunAllSteps = new Bindable<bool>();
        internal readonly Bindable<RecordState> RecordState = new Bindable<RecordState>();
        internal readonly BindableInt CurrentFrame = new BindableInt { MinValue = 0, MaxValue = 0 };

        private TestBrowserToolbar toolbar;
        private Container leftContainer;
        private Container mainContainer;

        private const float test_list_width = 200;

        private Action exit;

        private Bindable<bool> showLogOverlay;

        private readonly BindableDouble audioRateAdjust = new BindableDouble(1);

        [BackgroundDependencyLoader]
        private void load(Storage storage, GameHost host, FrameworkConfigManager frameworkConfig, FontStore fonts, Game game, AudioManager audio)
        {
            interactive = host.Window != null;
            config = new TestBrowserConfig(storage);

            exit = host.Exit;

            audio.AddAdjustment(AdjustableProperty.Frequency, audioRateAdjust);

            var resources = game.Resources;

            //Roboto
            fonts.AddStore(new GlyphStore(resources, @"Fonts/Roboto/Roboto-Regular"));
            fonts.AddStore(new GlyphStore(resources, @"Fonts/Roboto/Roboto-Bold"));

            //RobotoCondensed
            fonts.AddStore(new GlyphStore(resources, @"Fonts/RobotoCondensed/RobotoCondensed-Regular"));
            fonts.AddStore(new GlyphStore(resources, @"Fonts/RobotoCondensed/RobotoCondensed-Bold"));

            showLogOverlay = frameworkConfig.GetBindable<bool>(FrameworkSetting.ShowLogOverlay);

            var rateAdjustClock = new StopwatchClock(true);
            var framedClock = new FramedClock(rateAdjustClock);

            Children = new Drawable[]
            {
                mainContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = test_list_width },
                    Children = new Drawable[]
                    {
                        new SafeAreaContainer
                        {
                            SafeAreaOverrideEdges = Edges.Right | Edges.Bottom,
                            RelativeSizeAxes = Axes.Both,
                            Child = testContentContainer = new Container
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
                                            Font = new FontUsage(size: 30),
                                            Text = @"Compiling new version..."
                                        }
                                    },
                                }
                            }
                        },
                        toolbar = new TestBrowserToolbar
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 50,
                        },
                    }
                },
                leftContainer = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(test_list_width, 1),
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new SafeAreaContainer
                        {
                            SafeAreaOverrideEdges = Edges.Left | Edges.Top | Edges.Bottom,
                            RelativeSizeAxes = Axes.Both,
                            Child = new Box
                            {
                                Colour = FrameworkColour.GreenDark,
                                RelativeSizeAxes = Axes.Both
                            }
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Vertical,
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                searchTextBox = new TestBrowserTextBox
                                {
                                    OnCommit = delegate
                                    {
                                        var firstTest = leftFlowContainer.Where(b => b.IsPresent).SelectMany(b => b.FilterableChildren).OfType<TestSceneSubButton>().FirstOrDefault(b => b.MatchingFilter)?.TestType;
                                        if (firstTest != null)
                                            LoadTest(firstTest);
                                    },
                                    Height = 25,
                                    RelativeSizeAxes = Axes.X,
                                    PlaceholderText = "type to search",
                                    Depth = -1,
                                },
                                new BasicScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = false,
                                    Child = leftFlowContainer = new SearchContainer<TestGroupButton>
                                    {
                                        Padding = new MarginPadding { Top = 3, Bottom = 20 },
                                        Direction = FillDirection.Vertical,
                                        AutoSizeAxes = Axes.Y,
                                        RelativeSizeAxes = Axes.X,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            searchTextBox.Current.ValueChanged += e => leftFlowContainer.SearchTerm = e.NewValue;

            if (RuntimeInfo.SupportsJIT)
            {
                backgroundCompiler = new DynamicClassCompiler<TestScene>();
                backgroundCompiler.CompilationStarted += compileStarted;
                backgroundCompiler.CompilationFinished += compileFinished;
                backgroundCompiler.CompilationFailed += compileFailed;

                try
                {
                    backgroundCompiler.Start();
                }
                catch
                {
                    //it's okay for this to fail for now.
                }
            }

            foreach (Assembly asm in assemblies)
                toolbar.AddAssembly(asm.GetName().Name, asm);

            Assembly.BindValueChanged(updateList);
            RunAllSteps.BindValueChanged(v => runTests(null));
            PlaybackRate.BindValueChanged(e =>
            {
                rateAdjustClock.Rate = e.NewValue;
                audioRateAdjust.Value = e.NewValue;
            }, true);
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

        private void compileFailed(Exception ex) => Schedule(() =>
        {
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

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!e.Repeat)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        exit();
                        return true;
                }
            }

            return base.OnKeyDown(e);
        }

        public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F }, TestBrowserAction.Search),
            new KeyBinding(new[] { InputKey.Control, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Super, InputKey.F }, TestBrowserAction.Search), // for macOS
            new KeyBinding(new[] { InputKey.Super, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Control, InputKey.H }, TestBrowserAction.ToggleTestList),
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

        public void LoadTest(Type testType = null, Action onCompletion = null, bool isDynamicLoad = false)
        {
            if (testType == null && TestTypes.Count > 0)
                testType = TestTypes[0];

            config.Set(TestBrowserSetting.LastTest, testType?.Name ?? string.Empty);

            if (testType == null)
                return;

            var newTest = (TestScene)Activator.CreateInstance(testType);

            const string dynamic_prefix = "dynamic";

            // if we are a dynamically compiled type (via DynamicClassCompiler) we should update the dropdown accordingly.
            if (isDynamicLoad)
                toolbar.AddAssembly($"{dynamic_prefix} ({testType.Name})", testType.Assembly);
            else
                TestTypes.RemoveAll(t => t.Assembly.FullName.Contains(dynamic_prefix));

            Assembly.Value = testType.Assembly;

            var lastTest = CurrentTest;

            CurrentTest = newTest;
            CurrentTest.OnLoadComplete += _ => Schedule(() => finishLoad(newTest, lastTest, onCompletion));

            updateButtons();
            resetRecording();

            testContentContainer.Add(new ErrorCatchingDelayedLoadWrapper(CurrentTest, isDynamicLoad)
            {
                OnCaughtError = compileFailed
            });
        }

        private void resetRecording()
        {
            CurrentFrame.Value = 0;
            if (RecordState.Value == Testing.RecordState.Stopped)
                RecordState.Value = Testing.RecordState.Normal;
        }

        private void finishLoad(Drawable newTest, Drawable lastTest, Action onCompletion)
        {
            if (lastTest?.Parent != null)
            {
                testContentContainer.Remove(lastTest.Parent);
                lastTest.Dispose();
            }

            if (CurrentTest != newTest)
            {
                // There could have been multiple loads fired after us. In such a case we want to silently remove ourselves.
                testContentContainer.Remove(newTest.Parent);
                return;
            }

            updateButtons();

            var methods = newTest.GetType().GetMethods();

            bool hadTestAttributeTest = false;

            foreach (var m in methods.Where(m => m.Name != nameof(TestScene.TestConstructor)))
            {
                if (m.GetCustomAttribute(typeof(TestAttribute), false) != null)
                {
                    hadTestAttributeTest = true;
                    handleTestMethod(m);

                    if (m.GetCustomAttribute(typeof(RepeatAttribute), false) != null)
                    {
                        var count = (int)m.GetCustomAttributesData().Single(a => a.AttributeType == typeof(RepeatAttribute)).ConstructorArguments.Single().Value;

                        for (int i = 2; i <= count; i++)
                            handleTestMethod(m, $"{m.Name} ({i})");
                    }
                }

                foreach (var tc in m.GetCustomAttributes(typeof(TestCaseAttribute), false).OfType<TestCaseAttribute>())
                {
                    hadTestAttributeTest = true;
                    CurrentTest.AddLabel($"{m.Name}({string.Join(", ", tc.Arguments)})");

                    addSetUpSteps();

                    m.Invoke(CurrentTest, tc.Arguments);
                }
            }

            // even if no [Test] or [TestCase] methods were found, [SetUp] steps should be added.
            if (!hadTestAttributeTest)
                addSetUpSteps();

            backgroundCompiler?.Checkpoint(CurrentTest);
            runTests(onCompletion);
            updateButtons();

            void addSetUpSteps()
            {
                var setUpMethods = methods.Where(m => m.Name != nameof(TestScene.SetUpTestForNUnit) && m.GetCustomAttributes(typeof(SetUpAttribute), false).Length > 0).ToArray();

                if (setUpMethods.Any())
                {
                    CurrentTest.AddStep(new SetUpStep
                    {
                        Action = () => setUpMethods.ForEach(s => s.Invoke(CurrentTest, null))
                    });
                }

                CurrentTest.RunSetUpSteps();
            }

            void handleTestMethod(MethodInfo methodInfo, string name = null)
            {
                CurrentTest.AddLabel(name ?? methodInfo.Name);

                addSetUpSteps();

                methodInfo.Invoke(CurrentTest, null);
            }
        }

        private class SetUpStep : SingleStepButton
        {
            public SetUpStep()
            {
                Text = "[SetUp]";
                LightColour = Color4.Teal;
            }
        }

        private void runTests(Action onCompletion)
        {
            int actualStepCount = 0;
            CurrentTest.RunAllSteps(onCompletion, e => Logger.Log($@"Error on step: {e}"), s =>
            {
                if (!interactive || RunAllSteps.Value)
                    return false;

                if (actualStepCount > 0)
                    // stop once one actual step has been run.
                    return true;

                if (!(s is SetUpStep) && !(s is LabelStep))
                    actualStepCount++;

                return false;
            });
        }

        private void updateButtons()
        {
            foreach (var b in leftFlowContainer.Children)
                b.Current = CurrentTest.GetType();
        }

        private class ErrorCatchingDelayedLoadWrapper : DelayedLoadWrapper
        {
            private readonly bool catchErrors;
            private bool hasCaught;

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

                    // without this we will enter an infinite loading loop (DelayedLoadWrapper will see the child removed below and retry).
                    hasCaught = true;

                    OnCaughtError?.Invoke(e);
                    RemoveInternal(Content);
                }

                return false;
            }

            protected override bool ShouldLoadContent => !hasCaught;
        }

        private class TestBrowserTextBox : BasicTextBox
        {
            protected override float LeftRightPadding => TestSceneButton.LEFT_TEXT_PADDING;

            public TestBrowserTextBox()
            {
                TextFlow.Height = 0.75f;
            }
        }
    }

    internal enum RecordState
    {
        /// <summary>
        /// The game is playing back normally.
        /// </summary>
        Normal,

        /// <summary>
        /// Drawn game frames are currently being recorded.
        /// </summary>
        Recording,

        /// <summary>
        /// The default game playback is stopped, recorded frames are being played back.
        /// </summary>
        Stopped
    }

    public enum TestBrowserAction
    {
        ToggleTestList,
        Reload,
        Search
    }
}
