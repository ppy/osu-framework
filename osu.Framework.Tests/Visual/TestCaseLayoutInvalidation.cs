// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using JetBrains.Annotations;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osu.Framework.Tests.Extensions;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLayoutInvalidation : GridTestCase
    {
        // disable certain cases to discover more cases
        public static bool ForbidAutoSizeUndefinedCase = true; // keep (AutoSizeAxes & (Child.RelativeSizeAxes | Child.RelativePositionAxes | Child.BypassAutoSizeAxes)) to 0
        public static bool NoPadding = true;
        public static bool NoRotation = true;
        public static bool NoShear = true;
        public static bool NoBypassAutosizeAxes = true;
        public static bool NoFillMode = true;

        public class Case
        {
            public readonly Scene Scene;
            public readonly SceneModification[] Modifications;
            public readonly SceneInstance Instance;

            public IEnumerable<Drawable> Drawables => Instance.Nodes;
            public float Scale { get; }

            public Case(Scene scene, SceneModification[] modifications, float scale = 1)
            {
                Scale = scale;
                Scene = scene;
                Modifications = modifications;
                Instance = new SceneInstance(scene);

                foreach (var entry in modifications.Take(modifications.Length - 1))
                    Instance.Execute(entry);
            }

            public void DoModification()
            {
                Instance.Execute(Modifications.Last());
            }
        }

        private Action currentCaseAction;

        public void SetCase(Func<Case> factory)
        {
            var instance1 = factory();
            var instance2 = factory();

            for (var i = 0; i < 2; i++)
            {
                var instance = i == 0 ? instance1 : instance2;
                Cell(0, i).Child = new Container
                {
                    Size = new Vector2(250),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopLeft,
                    Name = i == 0 ? "WithoutInvalidation" : "WithInvalidation",
                    Child = new Container
                    {
                        Scale = new Vector2(instance.Scale),
                        AlwaysPresent = true,
                        Child = instance.Drawables.First()
                    }
                };

                var index = 0;
                foreach (var c in instance.Drawables.Cast<Container>())
                {
                    c.Add(new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1),
                        Colour = (index == 0 ? Color4.Red : index == 1 ? Color4.Blue : index == 2 ? Color4.Green : Color4.Yellow).Opacity(.5f),
                        Depth = -1,
                    });
                    index += 1;
                }
            }

            currentCaseAction = () =>
            {
                instance1.DoModification();
                instance2.DoModification();

                foreach (var d in instance2.Drawables)
                    d.Invalidate();
            };
        }

        private void addCaseStep(string name, Scene scene, SceneModification[] modifications, float scale)
        {
            AddStep($"{name} init", () =>
            {
                var result = runTest(scene, modifications);
                SetCase(() => new Case(scene, modifications, scale));

                if (!result)
                    throw new Exception("runTest returned false");
            });

            AddStep($"{name} update", () => { currentCaseAction?.Invoke(); });

            AddAssert($"{name} check", () =>
            {
                var container1 = Cell(0, 0).Child;
                var container2 = Cell(0, 1).Child;
                var state1 = GetDrawState(container1);
                var state2 = GetDrawState(container2);
                return almostEquals(state1, state2);
            });
        }

        private void addCaseSteps()
        {
            addCaseStep("AutoSize1",
                new Scene(new SceneNode(new[] { new SceneNode(new SceneNode[] { }), new SceneNode(new SceneNode[] { }) })),
                new[]
                {
                    new SceneModification("Root", nameof(AutoSizeAxes), Axes.Y),
                    new SceneModification("Child2", nameof(Origin), Anchor.BottomCentre),
                    new SceneModification("Child1", nameof(RelativePositionAxes), Axes.Y)
                },
                100);

            addCaseStep("AutoSize2",
                new Scene(new SceneNode(new[] { new SceneNode(new SceneNode[] { }), new SceneNode(new SceneNode[] { }) })),
                new[]
                {
                    new SceneModification("Root", nameof(AutoSizeAxes), Axes.Y),
                    new SceneModification("Child1", nameof(RelativeSizeAxes), Axes.Y),
                    new SceneModification("Child1", nameof(Height), 2),
                    new SceneModification("Child1", nameof(Height), 2)
                },
                50);

            addCaseStep("AutoSize3",
                new Scene(new SceneNode(new[] { new SceneNode(new SceneNode[] { }), new SceneNode(new SceneNode[] { }) })),
                new[]
                {
                    new SceneModification("Root", nameof(AutoSizeAxes), Axes.X),
                    new SceneModification("Child1", nameof(Anchor), Anchor.TopRight),
                    new SceneModification("Child2", nameof(Anchor), Anchor.TopRight),
                    new SceneModification("Child2", nameof(X), 1),
                    new SceneModification("Child2", nameof(RelativePositionAxes), Axes.X)
                },
                100);


            addCaseStep("AutoSize4",
                new Scene(new SceneNode(new[] { new SceneNode(new SceneNode[] { }), new SceneNode(new SceneNode[] { }) })),
                new[]
                {
                    new SceneModification("Root", nameof(AutoSizeAxes), Axes.X),
                    new SceneModification("Child1", nameof(Anchor), Anchor.TopRight),
                    new SceneModification("Child2", nameof(Anchor), Anchor.TopRight),
                    new SceneModification("Child1", nameof(RelativeSizeAxes), Axes.X)
                },
                100);

            addCaseStep("AutoSize5",
                new Scene(new SceneNode(new[] { new SceneNode(new[] { new SceneNode(new SceneNode[] { }) }) })),
                new[]
                {
                    new SceneModification("Root", nameof(AutoSizeAxes), Axes.X),
                    new SceneModification("Child", nameof(AutoSizeAxes), Axes.X),
                    new SceneModification("Child", nameof(Anchor), Anchor.Centre),
                    new SceneModification("GrandChild", nameof(Anchor), Anchor.TopRight),
                    new SceneModification("Child", nameof(Scale), new Vector2(12, 1))
                },
                100);
        }

        public TestCaseLayoutInvalidation()
            : base(1, 2)
        {
            addCaseSteps();

            var qc = Config.Quick;
            var config = new Config(3000, 0, qc.Replay, qc.Name, 1, 300, qc.QuietOnSuccess, qc.Every, qc.EveryShrink, qc.Arbitrary, new MyRunner
            {
                OnCounterCaseFound = onCounterCaseFound
            });

            foreach (var i in new[] { 2, 3, 10 })
            {
                var size = i;
                AddStep($"quickCheck({i})", () => Check.One(config, prop(size)));
            }
        }

        private void onCounterCaseFound(Scene scene, SceneModification[] modifications)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"{nameof(addCaseStep)}(\"Case\",\n\t{scene.GetCode()},\n\tnew[] {{{string.Join(", ", modifications.Select(x => x.GetCode()))}}},\n\t100);");
            Console.WriteLine();
        }

        public class MyRunner : IRunner
        {
            private readonly IRunner runner;
            public Action<Scene, SceneModification[]> OnCounterCaseFound;

            public MyRunner()
            {
                runner = Config.QuickThrowOnFailure.Runner;
            }

            public void OnStartFixture(Type t)
            {
                runner.OnStartFixture(t);
            }

            public void OnArguments(int nTest, FSharpList<object> args, FSharpFunc<int, FSharpFunc<FSharpList<object>, string>> every)
            {
                runner.OnArguments(nTest, args, every);
            }

            public void OnShrink(FSharpList<object> args, FSharpFunc<FSharpList<object>, string> everyShrink)
            {
                runner.OnShrink(args, everyShrink);
            }

            public void OnFinished(string name, TestResult testResult)
            {
                if (testResult is TestResult.False r)
                {
                    var shrunkArgs = r.Item3;
                    if (shrunkArgs.Length == 1 && shrunkArgs[0] is SceneAndModifications s)
                        OnCounterCaseFound?.Invoke(s.Scene, s.Modifications);
                }

                runner.OnFinished(name, testResult);
            }
        }

        private Property prop(int size)
        {
            return Prop.ForAll(new ArbitrarySceneAndModifications(size), s => runTest(s.Scene, s.Modifications));
        }

        private bool runTest(Scene scene, SceneModification[] modifications)
        {
            var cell = Cell(0, 0);
            var instance = new SceneInstance(scene);
            var container = new Container
            {
                Child = instance.Root,
                Size = new Vector2(250),
                Anchor = Anchor.Centre,
                Origin = Anchor.TopLeft,
            };

            try
            {
                cell.Child = container;
                cell.UpdateSubTree();

                foreach (var entry in modifications)
                {
                    if (!instance.Execute(entry)) continue;

                    if (!instance.CheckStateValidity())
                        return false;
                }
            }
            finally
            {
                cell.Remove(container);
                container.Remove(container.Child);
            }

            return true;
        }

        public struct SceneAndModifications
        {
            public readonly Scene Scene;
            public readonly SceneModification[] Modifications;

            public SceneAndModifications(Scene scene, SceneModification[] modifications)
            {
                Scene = scene;
                Modifications = modifications;
            }

            public override string ToString()
            {
                return $"({Scene}, {string.Join(", ", Modifications.Select(x => x.ToString()))})";
            }
        }

        public class ArbitrarySceneAndModifications : Arbitrary<SceneAndModifications>
        {
            private readonly ArbitraryScene arbitraryScene;

            public ArbitrarySceneAndModifications(int size)
            {
                arbitraryScene = new ArbitraryScene(size, size);
            }

            public override Gen<SceneAndModifications> Generator => arbitraryScene.Generator.SelectMany(scene =>
            {
                var arbitraryModificationList = new ArbitraryModificationList(new ArbitraryModification(scene));
                return arbitraryModificationList.Generator.Select(modifications => new SceneAndModifications(scene, modifications));
            });

            public override IEnumerable<SceneAndModifications> Shrinker(SceneAndModifications s)
            {
                foreach (var newScene in arbitraryScene.Shrinker(s.Scene))
                {
                    if (s.Modifications.Any(m => newScene.Nodes.All(node => node.Name != m.NodeName)))
                        continue;
                    yield return new SceneAndModifications(newScene, s.Modifications);
                }

                var arbitraryModificationList = new ArbitraryModificationList(new ArbitraryModification(s.Scene));

                foreach (var newModification in arbitraryModificationList.Shrinker(s.Modifications))
                    yield return new SceneAndModifications(s.Scene, newModification);
            }
        }

        public class ArbitraryModificationList : Arbitrary<SceneModification[]>
        {
            private readonly Arbitrary<SceneModification> elemArbitrary;

            public ArbitraryModificationList(Arbitrary<SceneModification> elemArbitrary)
            {
                this.elemArbitrary = elemArbitrary;
            }

            public override Gen<SceneModification[]> Generator => Gen.ListOf(elemArbitrary.Generator).Select(x => x.ToArray());

            public override IEnumerable<SceneModification[]> Shrinker(SceneModification[] list)
            {
                return list.Select(elem => list.Where(x => x != elem).ToArray()).Concat(
                    list.SelectMany(elem => elemArbitrary.Shrinker(elem).Select(newElem => list.Select(x => x == elem ? newElem : x).ToArray())));
            }
        }

        public class TestContainer : Container
        {
            public TestContainer()
            {
                Size = new Vector2(1);
                AlwaysPresent = true;
            }

            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds)
            {
                return false;
            }

            public override string ToString()
            {
                return $"{Name} ({DrawPosition.X:#,0},{DrawPosition.Y:#,0}) {DrawSize.X:#,0}x{DrawSize.Y:#,0}";
            }
        }

        public class SceneModification
        {
            public readonly string NodeName;
            public readonly string PropertyName;
            public readonly object Value;

            public SceneModification(string nodeName, string propertyName, object value)
            {
                NodeName = nodeName;
                PropertyName = propertyName;
                Value = value;
            }

            public override string ToString()
            {
                return $"{NodeName}.{PropertyName} = {formatValue(Value)}";
            }

            private string formatValue(object value)
            {
                switch (value)
                {
                    case float x:
                        return x == (int)x ? ((int)x).ToString() : $"{x}f";
                    case Vector2 vec:
                        return $"new Vector2({formatValue(vec.X)}, {formatValue(vec.Y)})";
                    case MarginPadding x:
                        return $"new MarginPadding {{Top={formatValue(x.Top)},Left={formatValue(x.Left)},Bottom={formatValue(x.Bottom)},Right={formatValue(x.Right)}}}";
                    case Anchor _:
                    case Axes _:
                    case FillMode _:
                        return $"{value.GetType().Name}.{value}";
                    default:
                        return value.ToString();
                }
            }

            public string GetCode()
            {
                return $"new {nameof(SceneModification)}(\"{NodeName}\", nameof({PropertyName}), {formatValue(Value)})";
            }
        }

        public class SceneNode
        {
            public readonly SceneNode[] Children;
            public readonly int TreeSize;
            public string Name = "Node";

            public SceneNode(IEnumerable<SceneNode> children)
            {
                Children = children.ToArray();
                TreeSize = 1 + Children.Select(c => c.TreeSize).Sum();
            }

            public override string ToString()
            {
                return $"{Name} {{{string.Join(", ", Children.Select(x => x.ToString()))}}}";
            }

            public string GetCode()
            {
                return $"new {nameof(SceneNode)}(new{(Children.Length != 0 ? "" : " " + nameof(SceneNode))}[]{{{string.Join(", ", Children.Select(x => x.GetCode()))}}})";
            }
        }

        public class Scene
        {
            private readonly List<SceneNode> nodes;
            public IReadOnlyList<SceneNode> Nodes => nodes;
            public SceneNode Root => Nodes.First();

            public Scene([NotNull] SceneNode root)
            {
                if (root == null) throw new ArgumentNullException(nameof(root));
                nodes = new List<SceneNode>();

                var depthMap = new Dictionary<object, int>();

                void enumNodes(SceneNode node, int depth)
                {
                    depthMap[node] = depth;
                    nodes.Add(node);
                    foreach (var child in node.Children)
                        enumNodes(child, depth + 1);
                }

                enumNodes(root, 0);

                foreach (var group in Nodes.GroupBy(c => depthMap[c]))
                {
                    var depth = group.Key;
                    var array = group.ToArray();
                    var prefix = depth == 0 ? "Root" : string.Join("", Enumerable.Repeat("Grand", depth - 1)) + "Child";
                    var index = 1;
                    foreach (var node in array)
                    {
                        node.Name = array.Length == 1 ? prefix : prefix + index;
                        ++index;
                    }
                }
            }

            public override string ToString()
            {
                return $"Scene({Root})";
            }

            public string GetCode()
            {
                return $"new {nameof(Scene)}({Root.GetCode()})";
            }
        }

        public class SceneTreeComparator : IEqualityComparer<SceneNode>
        {
            public bool Equals(SceneNode x, SceneNode y)
            {
                return x.TreeSize == y.TreeSize && x.Children.Length == y.Children.Length &&
                       x.Children.Zip(y.Children, Equals).All(b => b);
            }

            public int GetHashCode(SceneNode node)
            {
                return node.Children.Select(GetHashCode).Prepend(node.TreeSize).Aggregate((x, y) => unchecked(x * 1234567 + y));
            }
        }

        public class ArbitraryScene : Arbitrary<Scene>
        {
            public readonly int SizeLo, SizeUp;

            public ArbitraryScene(int sizeLo, int sizeUp)
            {
                SizeLo = sizeLo;
                SizeUp = sizeUp;
            }

            public override Gen<Scene> Generator => Gen.Choose(SizeLo, SizeUp).SelectMany(gen).Select(root => new Scene(root));

            private static Gen<SceneNode> gen(int treeSize)
            {
                return genChildren(treeSize - 1).Select(children => new SceneNode(children));
            }

            private static Gen<FSharpList<SceneNode>> genChildren(int treeSize)
            {
                return treeSize == 0
                    ? Gen.Constant(FSharpList<SceneNode>.Empty)
                    : Gen.Choose(1, treeSize).SelectMany(childSize => gen(childSize).SelectMany(head =>
                        genChildren(treeSize - childSize).Select(tail => FSharpList<SceneNode>.Cons(head, tail))));
            }

            public override IEnumerable<Scene> Shrinker(Scene scene)
            {
                var leaves = scene.Nodes.Where(x => x.Children.Length == 0 && x != scene.Root);
                return leaves.Select(leaf => remove(scene.Root, leaf)).Distinct(new SceneTreeComparator()).Select(root => new Scene(root));
            }

            private SceneNode remove(SceneNode node, SceneNode target)
            {
                return new SceneNode(node.Children.Where(x => x != target).Select(x => remove(x, target)).ToArray());
            }
        }

        public class ArbitraryModification : Arbitrary<SceneModification>
        {
            public readonly Scene Scene;

            public ArbitraryModification(Scene scene)
            {
                Scene = scene;
            }

            public override Gen<SceneModification> Generator => gen_for(Scene);

            public override IEnumerable<SceneModification> Shrinker(SceneModification modification)
            {
                return shrink(modification.Value, positiveProperties.Contains(modification.PropertyName))
                    .Select(newValue => new SceneModification(modification.NodeName, modification.PropertyName, newValue));
            }

            private readonly HashSet<string> positiveProperties = new HashSet<string>(new[] { nameof(Width), nameof(Height), nameof(Scale) });

            private IEnumerable<float> shrink_float(float x)
            {
                return float_values.Where(y => (y < 0 ? 1 : 0) < (x < 0 ? 1 : 0) || (y < 0 ? 1 : 0) == (x < 0 ? 1 : 0) && Math.Abs(y) < Math.Abs(x));
            }

            private IEnumerable<T[]> shrink_tuple<T>(T[] tuple, Func<T, IEnumerable<T>> shrinkElem)
            {
                for (var i = 0; i < tuple.Length; i++)
                    foreach (var newElem in shrinkElem(tuple[i]))
                        yield return tuple.Take(i).Concat(tuple.Skip(i).Prepend(newElem)).ToArray();
            }

            private IEnumerable<object> shrink(object value, bool positive = false)
            {
                switch (value)
                {
                    case int x:
                        return Arb.Shrink(x).Cast<object>();
                    case float x:
                        return shrink_float(x).Where(f => f > 0 || !positive).Cast<object>();
                    case Vector2 v:
                        return shrink_tuple(new[] { v.X, v.Y }, x => shrink_float(x).Where(f => f > 0 || !positive)).Select(t => (object)new Vector2(t[0], t[1]));
                    case MarginPadding m:
                        return shrink_tuple(new[] { m.Top, m.Left, m.Bottom, m.Right }, shrink_float).Select(t => (object)new MarginPadding { Top = t[0], Left = t[1], Bottom = t[2], Right = t[3] });
                    case Axes x:
                        return new[] { x & ~Axes.X, x & ~Axes.Y }.Distinct().Where(y => y != x).Cast<object>();
                    default:
                        return Enumerable.Empty<object>();
                }
            }

            private struct Entry
            {
                public readonly string PropertyName;
                public readonly object Value;

                public Entry(string propertyName, object value)
                {
                    PropertyName = propertyName;
                    Value = value;
                }
            }

            private static Gen<SceneModification> gen_for(Scene scene)
            {
                return Gen.Choose(0, scene.Nodes.Count - 1).SelectMany(nodeIndex =>
                {
                    var node = scene.Nodes[nodeIndex];
                    return for_container.Select(pair => new SceneModification(node.Name, pair.PropertyName, pair.Value));
                });
            }

            private static readonly float[] positive_float_values = new float[] { 1 / 3f, 0.5f, 2 / 3f, 1f, 4 / 3f, 1.5f, 5 / 3f, 2f, 2.5f, 3f };
            private static readonly float[] float_values = positive_float_values.Select(x => -x).Reverse().Concat(positive_float_values.Prepend(0f)).ToArray();

            private static readonly Gen<float> position = Gen.OneOf(float_values.Select(Gen.Constant));
            private static readonly Gen<float> size = Gen.OneOf(positive_float_values.Select(Gen.Constant));
            private static readonly Gen<float> rotation = Gen.OneOf(float_values.Select(x => Gen.Constant(x * 120)));

            private static readonly Gen<Anchor> anchor = Gen.OneOf(new[]
            {
                Anchor.TopLeft,
                Anchor.TopCentre,
                Anchor.TopRight,
                Anchor.CentreLeft,
                Anchor.Centre,
                Anchor.CentreRight,
                Anchor.BottomLeft,
                Anchor.BottomCentre,
                Anchor.BottomRight
            }.Select(Gen.Constant));

            private static readonly Gen<Axes> axes = Gen.OneOf(new[] { Axes.None, Axes.X, Axes.Y, Axes.Both }.Select(Gen.Constant));
            private static readonly Gen<FillMode> fillmode = Gen.OneOf(new[] { FillMode.Fill, FillMode.Fit, FillMode.Stretch }.Select(Gen.Constant));

            private static Gen<Vector2> vec(Gen<float> gen)
            {
                return Gen.Two(gen).Select(t => new Vector2(t.Item1, t.Item2));
            }

            private static Gen<MarginPadding> marginpadding(Gen<float> gen)
            {
                return Gen.Four(gen).Select(t => new MarginPadding
                {
                    Top = t.Item1,
                    Left = t.Item2,
                    Bottom = t.Item3,
                    Right = t.Item4
                });
            }

            private static Gen<Entry> entry<T>(string propertyName, Gen<T> gen)
            {
                return gen.Select(x => new Entry(propertyName, x));
            }

            private static readonly Gen<Entry> dummy = Gen.Constant(new Entry("Dummy", 0));

            private static readonly Gen<Entry> for_container = Gen.OneOf(
                entry(nameof(X), position),
                entry(nameof(Y), position),
                entry(nameof(Width), size),
                entry(nameof(Height), size),
                entry(nameof(Margin), marginpadding(position)),
                NoPadding ? dummy : entry(nameof(Padding), marginpadding(position)),
                entry(nameof(Origin), anchor),
                entry(nameof(Anchor), anchor),
                entry(nameof(RelativeSizeAxes), axes),
                entry(nameof(AutoSizeAxes), axes),
                entry(nameof(RelativePositionAxes), axes),
                NoBypassAutosizeAxes ? dummy : entry(nameof(BypassAutoSizeAxes), axes),
                NoFillMode ? dummy : entry(nameof(FillMode), fillmode),
                entry(nameof(Scale), vec(size)),
                NoRotation ? dummy : entry(nameof(Rotation), rotation),
                NoShear ? dummy : entry(nameof(Shear), vec(position))
            );
        }

        public class SceneInstance
        {
            private readonly List<TestContainer> nodes;
            public IReadOnlyList<TestContainer> Nodes => nodes;
            public TestContainer Root => Nodes.First();

            private readonly Dictionary<string, TestContainer> nodeMap;

            public TestContainer GetNode(string name)
            {
                return nodeMap[name];
            }

            public SceneInstance(Scene scene)
            {
                nodes = new List<TestContainer>();
                nodeMap = new Dictionary<string, TestContainer>();

                TestContainer createInstanceTree(SceneNode node)
                {
                    var container = new TestContainer { Name = node.Name };

                    nodes.Add(container);
                    nodeMap.Add(node.Name, container);

                    foreach (var child in node.Children)
                        container.Add(createInstanceTree(child));
                    return container;
                }

                createInstanceTree(scene.Root);
            }

            private bool checkAutoSizeUndefinedCase(Container container, SceneModification modification)
            {
                if (!ForbidAutoSizeUndefinedCase) return true;

                Axes overwrite(Drawable c, string prop, Axes value)
                {
                    return c.Name == modification.NodeName && prop == modification.PropertyName ? (Axes)modification.Value : value;
                }

                var relativeAxes = container.Children.Select(x => overwrite(x, nameof(RelativeSizeAxes), x.RelativeSizeAxes) |
                                                                  overwrite(x, nameof(RelativePositionAxes), x.RelativePositionAxes) |
                                                                  overwrite(x, nameof(BypassAutoSizeAxes), x.Get<Axes>("bypassAutoSizeAxes"))).Prepend(~(Axes)0).Aggregate((x, y) => x & y);

                return (overwrite(container, nameof(AutoSizeAxes), container.AutoSizeAxes) & relativeAxes) == 0;
            }

            public bool CanExecute(SceneModification modification)
            {
                var container = GetNode(modification.NodeName);

                switch (modification.PropertyName)
                {
                    case nameof(RelativeSizeAxes):
                        if ((container.AutoSizeAxes & (Axes)modification.Value) != 0) return false;
                        goto case nameof(RelativePositionAxes);

                    case nameof(RelativePositionAxes):
                    case nameof(BypassAutoSizeAxes):
                        return container.Parent == null || checkAutoSizeUndefinedCase((Container)container.Parent, modification);

                    case nameof(AutoSizeAxes):
                        return (container.RelativeSizeAxes & (Axes)modification.Value) == 0 && checkAutoSizeUndefinedCase(container, modification);

                    case nameof(Width):
                        return (container.AutoSizeAxes & Axes.X) == 0;
                    case nameof(Height):
                        return (container.AutoSizeAxes & Axes.Y) == 0;

                    default:
                        return true;
                }
            }

            public bool Execute(SceneModification modification)
            {
                if (modification.PropertyName == "Dummy") return false;
                if (!CanExecute(modification)) return false;

                var container = GetNode(modification.NodeName);
                container.Set(modification.PropertyName, modification.Value);

                return true;
            }

            private void invalidate()
            {
                foreach (var node in Nodes)
                    node.Invalidate();
            }

            private void update()
            {
                Root.UpdateSubTree();
                Root.UpdateSubTree();
                Root.UpdateSubTree();
                Root.UpdateSubTree();
                Root.UpdateSubTree();
                //Root.ValidateSubTree();
            }

            public float[] LastState1, LastState2;

            public bool CheckStateValidity()
            {
                if (!Root.IsLoaded) throw new InvalidOperationException("The scene isn't loaded");

                update();

                var state1 = GetDrawState(Root);

                invalidate();
                update();

                var state2 = GetDrawState(Root);

                LastState1 = state1;
                LastState2 = state2;

                return almostEquals(state1, state2);
            }
        }

        public static float[] GetDrawState(Drawable root)
        {
            return root.GetDecendants().SelectMany(c =>
            {
                var size = c.DrawSize;
                var position = c.DrawPosition;
                return new[] { size.X, size.Y, position.X, position.Y };
            }).ToArray();
        }

        private static bool almostEquals(float[] x, float[] y)
        {
            // more tolerance for an array containing a big value
            var epsilon = 1e-2f * x.Concat(y).Prepend(1f).Select(Math.Abs).Max();
            return x.Length == y.Length && Enumerable.Range(0, x.Length).All(i => Precision.AlmostEquals(x[i], y[i], epsilon));
        }
    }
}
