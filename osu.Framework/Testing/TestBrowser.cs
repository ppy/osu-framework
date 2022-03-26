// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using Logger = osu.Framework.Logging.Logger;

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

        private bool interactive;

        private readonly List<Assembly> assemblies;

        /// <summary>
        /// Creates a new TestBrowser that displays the TestCases of every assembly that start with either "osu" or the specified namespace (if it isn't null)
        /// </summary>
        /// <param name="assemblyNamespace">Assembly prefix which is used to match assemblies whose tests should be displayed</param>
        public TestBrowser(string assemblyNamespace = null)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(n =>
            {
                Debug.Assert(n.FullName != null);
                return n.FullName.StartsWith("osu", StringComparison.Ordinal) || (assemblyNamespace != null && n.FullName.StartsWith(assemblyNamespace, StringComparison.Ordinal));
            }).ToList();

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
                                                        string group = t.Namespace?.AsSpan(namespacePrefix.Length).TrimStart('.').ToString();
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

        private readonly BindableDouble audioRateAdjust = new BindableDouble(1);

        [BackgroundDependencyLoader]
        private void load(Storage storage, GameHost host, AudioManager audio)
        {
            interactive = host.Window != null;
            config = new TestBrowserConfig(storage);

            if (host.CanExit)
                exit = host.Exit;
            else if (host.CanSuspendToBackground)
                exit = () => host.SuspendToBackground();

            audio.AddAdjustment(AdjustableProperty.Frequency, audioRateAdjust);

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
                                            Font = FrameworkFont.Regular.With(size: 30),
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
                                        AllowNonContiguousMatching = true,
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

            searchTextBox.OnCommit += delegate
            {
                var firstTest = leftFlowContainer.Where(b => b.IsPresent).SelectMany(b => b.FilterableChildren).OfType<TestSubButton>()
                                                 .FirstOrDefault(b => b.MatchingFilter)?.TestType;
                if (firstTest != null)
                    LoadTest(firstTest);
            };

            searchTextBox.Current.ValueChanged += e => leftFlowContainer.SearchTerm = e.NewValue;

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += compileFinished;

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

        private void compileFinished(Type[] updatedTypes) => Schedule(() =>
        {
            compilingNotice.FadeOut(800, Easing.InQuint);
            compilingNotice.FadeColour(Color4.YellowGreen, 100);

            if (CurrentTest == null)
                return;

            try
            {
                LoadTest(CurrentTest.GetType(), isDynamicLoad: true);
            }
            catch (Exception e)
            {
                compileFailed(e);
            }
        });

        private void compileFailed(Exception ex) => Schedule(() =>
        {
            Logger.Error(ex, "Error with dynamic compilation!");

            compilingNotice.FadeIn(100, Easing.OutQuint).Then().FadeOut(800, Easing.InQuint);
            compilingNotice.FadeColour(Color4.Red, 100);
        });

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (CurrentTest == null)
            {
                string lastTest = config.Get<string>(TestBrowserSetting.LastTest);

                var foundTest = TestTypes.Find(t => t.FullName == lastTest);

                LoadTest(foundTest);
            }
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
                        exit?.Invoke();
                        return true;
                }
            }

            return base.OnKeyDown(e);
        }

        public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F }, TestBrowserAction.Search),
            new KeyBinding(new[] { InputKey.Control, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Super, InputKey.F }, TestBrowserAction.Search), // for macOS
            new KeyBinding(new[] { InputKey.Super, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Control, InputKey.H }, TestBrowserAction.ToggleTestList),
        };

        public bool OnPressed(KeyBindingPressEvent<TestBrowserAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
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

        public void OnReleased(KeyBindingReleaseEvent<TestBrowserAction> e)
        {
        }

        public void LoadTest(Type testType = null, Action onCompletion = null, bool isDynamicLoad = false)
        {
            if (CurrentTest?.Parent != null)
            {
                testContentContainer.Remove(CurrentTest.Parent);
                CurrentTest.Dispose();
            }

            CurrentTest = null;

            if (testType == null && TestTypes.Count > 0)
                testType = TestTypes[0];

            config.SetValue(TestBrowserSetting.LastTest, testType?.FullName ?? string.Empty);

            if (testType == null)
                return;

            var newTest = (TestScene)Activator.CreateInstance(testType);

            Debug.Assert(newTest != null);

            Assembly.Value = testType.Assembly;

            CurrentTest = newTest;
            CurrentTest.OnLoadComplete += _ => Schedule(() => finishLoad(newTest, onCompletion));

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

        private void finishLoad(TestScene newTest, Action onCompletion)
        {
            if (CurrentTest != newTest)
            {
                // There could have been multiple loads fired after us. In such a case we want to silently remove ourselves.
                testContentContainer.Remove(newTest.Parent);
                return;
            }

            updateButtons();

            bool hadTestAttributeTest = false;

            foreach (var m in newTest.GetType().GetMethods())
            {
                string name = m.Name;

                if (name == nameof(TestScene.TestConstructor) || m.GetCustomAttribute(typeof(IgnoreAttribute), false) != null)
                    continue;

                if (name.StartsWith("Test", StringComparison.Ordinal))
                    name = name.Substring(4);

                int runCount = 1;

                if (m.GetCustomAttribute(typeof(RepeatAttribute), false) != null)
                {
                    object count = m.GetCustomAttributesData().Single(a => a.AttributeType == typeof(RepeatAttribute)).ConstructorArguments.Single().Value;
                    Debug.Assert(count != null);

                    runCount += (int)count;
                }

                for (int i = 0; i < runCount; i++)
                {
                    string repeatSuffix = i > 0 ? $" ({i + 1})" : string.Empty;

                    var methodWrapper = new MethodWrapper(m.GetType(), m);

                    if (methodWrapper.GetCustomAttributes<TestAttribute>(false).SingleOrDefault() != null)
                    {
                        var parameters = m.GetParameters();

                        if (parameters.Length > 0)
                        {
                            var valueMatrix = new List<List<object>>();

                            foreach (var p in methodWrapper.GetParameters())
                            {
                                var valueAttrib = p.GetCustomAttributes<ValuesAttribute>(false).SingleOrDefault();
                                if (valueAttrib == null)
                                    throw new ArgumentException($"Parameter is present on a {nameof(TestAttribute)} method without values specification.", p.ParameterInfo.Name);

                                List<object> choices = new List<object>();

                                foreach (object choice in valueAttrib.GetData(p))
                                    choices.Add(choice);

                                valueMatrix.Add(choices);
                            }

                            foreach (var combination in valueMatrix.CartesianProduct())
                            {
                                hadTestAttributeTest = true;
                                CurrentTest.AddLabel($"{name}({string.Join(", ", combination)}){repeatSuffix}");
                                handleTestMethod(m, combination.ToArray());
                            }
                        }
                        else
                        {
                            hadTestAttributeTest = true;
                            CurrentTest.AddLabel($"{name}{repeatSuffix}");
                            handleTestMethod(m);
                        }
                    }

                    foreach (var tc in m.GetCustomAttributes(typeof(TestCaseAttribute), false).OfType<TestCaseAttribute>())
                    {
                        hadTestAttributeTest = true;
                        CurrentTest.AddLabel($"{name}({string.Join(", ", tc.Arguments)}){repeatSuffix}");

                        handleTestMethod(m, tc.Arguments);
                    }

                    foreach (var tcs in m.GetCustomAttributes(typeof(TestCaseSourceAttribute), false).OfType<TestCaseSourceAttribute>())
                    {
                        IEnumerable sourceValue = getTestCaseSourceValue(m, tcs);

                        if (sourceValue == null)
                        {
                            Debug.Assert(tcs.SourceName != null);
                            throw new InvalidOperationException($"The value of the source member {tcs.SourceName} must be non-null.");
                        }

                        foreach (object argument in sourceValue)
                        {
                            hadTestAttributeTest = true;

                            if (argument is IEnumerable argumentsEnumerable)
                            {
                                object[] arguments = argumentsEnumerable.Cast<object>().ToArray();

                                CurrentTest.AddLabel($"{name}({string.Join(", ", arguments)}){repeatSuffix}");
                                handleTestMethod(m, arguments);
                            }
                            else
                            {
                                CurrentTest.AddLabel($"{name}({argument}){repeatSuffix}");
                                handleTestMethod(m, argument);
                            }
                        }
                    }
                }
            }

            // even if no [Test] or [TestCase] methods were found, [SetUp] steps should be added.
            if (!hadTestAttributeTest)
                addSetUpSteps();

            runTests(onCompletion);
            updateButtons();

            void addSetUpSteps()
            {
                var setUpMethods = ReflectionUtils.GetMethodsWithAttribute(newTest.GetType(), typeof(SetUpAttribute), true)
                                                  .Where(m => m.Name != nameof(TestScene.SetUpTestForNUnit));

                if (setUpMethods.Any())
                {
                    CurrentTest.AddStep(new SingleStepButton(true)
                    {
                        Text = "[SetUp]",
                        LightColour = Color4.Teal,
                        Action = () => setUpMethods.ForEach(s => s.Invoke(CurrentTest, null))
                    });
                }

                CurrentTest.RunSetUpSteps();
            }

            void handleTestMethod(MethodInfo methodInfo, params object[] arguments)
            {
                addSetUpSteps();
                methodInfo.Invoke(CurrentTest, arguments);
                CurrentTest.RunTearDownSteps();
            }
        }

        private static IEnumerable getTestCaseSourceValue(MethodInfo testMethod, TestCaseSourceAttribute tcs)
        {
            var sourceDeclaringType = tcs.SourceType ?? testMethod.DeclaringType;
            Debug.Assert(sourceDeclaringType != null);

            if (tcs.SourceType != null && tcs.SourceName == null)
                return (IEnumerable)Activator.CreateInstance(tcs.SourceType);

            var sourceMembers = sourceDeclaringType.AsNonNull().GetMember(tcs.SourceName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (sourceMembers.Length == 0)
                throw new InvalidOperationException($"No static member with the name of {tcs.SourceName} exists in {sourceDeclaringType} or its base types.");

            if (sourceMembers.Length > 1)
                throw new NotSupportedException($"There are multiple members with the same source name ({tcs.SourceName}) (e.g. method overloads).");

            var sourceMember = sourceMembers.Single();

            switch (sourceMember)
            {
                case FieldInfo sf:
                    return (IEnumerable)sf.GetValue(null);

                case PropertyInfo sp:
                    if (!sp.CanRead)
                        throw new InvalidOperationException($"The source property {sp.Name} in {sp.DeclaringType.ReadableName()} must have a getter.");

                    return (IEnumerable)sp.GetValue(null);

                case MethodInfo sm:
                    int methodParamsLength = sm.GetParameters().Length;
                    if (methodParamsLength != (tcs.MethodParams?.Length ?? 0))
                        throw new InvalidOperationException($"The given source method parameters count doesn't match the method. (attribute has {tcs.MethodParams?.Length ?? 0}, method has {methodParamsLength})");

                    return (IEnumerable)sm.Invoke(null, tcs.MethodParams);

                default:
                    throw new NotSupportedException($"{sourceMember.MemberType} is not a supported member type for {nameof(TestCaseSourceAttribute)} (must be static field, property or method)");
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

                if (!s.IsSetupStep && !(s is LabelStep))
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
            protected override float LeftRightPadding => TestButtonBase.LEFT_TEXT_PADDING;

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
