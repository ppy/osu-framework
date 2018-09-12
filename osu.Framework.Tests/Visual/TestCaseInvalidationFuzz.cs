// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FsCheck;
using JetBrains.Annotations;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osu.Framework.Tests.Extensions;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseInvalidationFuzz : GridTestCase
    {
        // disable certain cases to discover more cases
        public static bool ForbidAutoSizeSomeCase = false; // keep (AutoSizeAxes & (Child1.BypassAutoSizeAxes & ... & Childn.BypassAutoSizeAxes)) to 0
        public static bool ForbidChildPositionModificationForFlowContainer = true;
        public static bool NoBypassAutosizeAxes = false;
        public static bool NoPadding = false;
        public static bool NoFillMode = false;
        public static bool NoRotation = false;
        public static bool NoShear = false;

        public static bool NoFillFlowContainer = false;
        public static bool NoGridContainer = true;

        public static bool RepeatQuickCheck = false;

        private Action currentCaseAction;
        private readonly Container<DrawQuadOverlayBox> overlayBoxContainer;

        public TestCaseInvalidationFuzz()
            : base(1, 2)
        {
            var qc = Config.Quick;
            {
                var config = new Config(300, 0, qc.Replay, qc.Name, 1, 300, qc.QuietOnSuccess, qc.Every, qc.EveryShrink, qc.Arbitrary, new MyRunner
                {
                    OnCounterCaseFound = onCounterCaseFound
                });
                if (RepeatQuickCheck)
                {
                    AddRepeatStep("repeat quickCheck", () =>
                    {
                        Check.One(config, prop(3));
                        Check.One(config, prop(5));
                        Check.One(config, prop(10));
                    }, 10000000);
                }

                foreach (var i in new[] { 2, 3, 5 })
                {
                    var size = i;
                    AddStep($"quickCheck({i})", () => Check.One(config, prop(size)));
                }
            }

            Add(overlayBoxContainer = new Container<DrawQuadOverlayBox>());
        }

        public class Case
        {
            public readonly string Name;
            public readonly Scene Scene;
            public readonly SceneModification[] Modifications;
            public readonly float Scale;

            public Case(string name, Scene scene, SceneModification[] modifications, float scale)
            {
                Name = name;
                Scene = scene;
                Modifications = modifications;
                Scale = scale;
            }

            public SceneInstance CreateSceneInstance()
            {
                var instance = new SceneInstance(Scene);
                instance.Container.Child.Scale = new Vector2(Scale);

                foreach (var entry in Modifications.Take(Modifications.Length - 1))
                    instance.Execute(entry);

                return instance;
            }

            public void DoModification(SceneInstance instance)
            {
                instance.Execute(Modifications.Last());
            }
        }

        private SceneInstance instance1, instance2;

        public void SetCase(Case testCase)
        {
            instance1 = testCase.CreateSceneInstance();
            instance2 = testCase.CreateSceneInstance();

            overlayBoxContainer.Clear();

            for (var i = 0; i < 2; i++)
            {
                var instance = i == 0 ? instance1 : instance2;
                Cell(0, i).Child = instance.Container;
                instance.Container.Name = i == 0 ? "WithoutInvalidation" : "WithInvalidation";

                for (int j = 0; j < instance.Nodes.Count; j++)
                    overlayBoxContainer.Add(new DrawQuadOverlayBox(instance.Nodes[j]) { Colour = TestCaseLayoutInvalidation.RandomColorPalette.Get(j) });
            }

            currentCaseAction = () =>
            {
                testCase.DoModification(instance1);
                testCase.DoModification(instance2);

                foreach (var d in instance2.Nodes)
                    d.PropagateInvalidateAll();
            };
        }

        private void addCaseStep(Case testCase)
        {
            AddStep($"{testCase.Name} init", () =>
            {
                foreach (var m in testCase.Modifications)
                    Console.WriteLine($"{m};");

                var result = runTest(testCase.Scene, testCase.Modifications);
                SetCase(testCase);

                if (!result)
                    throw new Exception("runTest returned false");
            });

            AddStep($"{testCase.Name} update", () => { currentCaseAction?.Invoke(); });

            AddAssert($"{testCase.Name} check", () =>
            {
                var state1 = instance1.GetDrawState();
                var state2 = instance2.GetDrawState();
                return almostEquals(state1, state2);
            });
        }

        private void onCounterCaseFound(Scene scene, SceneModification[] modifications)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"{nameof(addCaseStep)}(new {nameof(Case)}(\"Case\",\n\t{scene.GetCode()},\n\tnew[] {{{string.Join(", ", modifications.Select(x => x.GetCode()))}}},\n\t100));");
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

            try
            {
                cell.Add(instance.Container);
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
                cell.Remove(instance.Container);
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
            }

            public override string ToString() => $"{Name} {DrawPosition} {DrawSize}";
            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
        }

        public class TestGridContainer : GridContainer
        {
            public TestGridContainer()
            {
                Size = new Vector2(1);
            }

            public override string ToString() => $"Grid {Name} {DrawPosition} {DrawSize}";
            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
        }

        public class TestFillFlowContainer : FillFlowContainer
        {
            public TestFillFlowContainer()
            {
                Size = new Vector2(1);
            }

            public override string ToString() => $"FillFlow {Name} {DrawPosition} {DrawSize}";
            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
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
                    case FillDirection _:
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

        public abstract class SceneNode
        {
            public readonly SceneNode[] Children;
            public string Name = "Node";

            protected SceneNode(IEnumerable<SceneNode> children)
            {
                Children = children?.ToArray() ?? Array.Empty<SceneNode>();
            }

            public abstract string GetCode();
            public override string ToString() => GetCode();

            public abstract Drawable CreateInstanceTree(List<Drawable> resultDrawables);

            protected string GetChildrenArrayCreationCode() =>
                Children.Length == 0
                    ? ""
                    : $"new{(Children.Length != 0 && Children.All(x => x.GetType() == Children[0].GetType()) ? "" : " " + nameof(SceneNode))}[]{{{string.Join(",", Children.Select(x => x.GetCode()))}}}";
        }

        public class ContainerNode : SceneNode
        {
            public ContainerNode(IEnumerable<SceneNode> children = null)
                : base(children)
            {
            }

            public override Drawable CreateInstanceTree(List<Drawable> resultDrawables)
            {
                var container = new TestContainer { Name = Name };
                resultDrawables.Add(container);
                foreach (var c in Children)
                    container.Add(c.CreateInstanceTree(resultDrawables));
                return container;
            }

            public override string GetCode() => $"new {nameof(ContainerNode)}({GetChildrenArrayCreationCode()})";
        }

        public class GridContainerNode : SceneNode
        {
            public readonly int Rows, Columns;

            public GridContainerNode(int rows, int columns, IEnumerable<SceneNode> children = null)
                : base(children)
            {
                Trace.Assert(Children.Length <= rows * columns);
                Rows = rows;
                Columns = columns;
            }

            public override Drawable CreateInstanceTree(List<Drawable> resultDrawables)
            {
                var gridContainer = new TestGridContainer { Name = Name };
                resultDrawables.Add(gridContainer);

                var content = new Drawable[Rows][];
                for (int i = 0; i < Rows; i++)
                    content[i] = Children.Skip(i * Columns).Take(Columns).Select(c => c.CreateInstanceTree(resultDrawables)).ToArray();

                return gridContainer;
            }

            public override string GetCode() => $"new {nameof(GridContainerNode)}({Columns},{Columns},{GetChildrenArrayCreationCode()})";
        }

        public class FillFlowContainerNode : SceneNode
        {
            public FillFlowContainerNode(IEnumerable<SceneNode> children = null)
                : base(children)
            {
            }

            public override Drawable CreateInstanceTree(List<Drawable> resultDrawables)
            {
                var container = new TestFillFlowContainer { Name = Name };
                resultDrawables.Add(container);
                foreach (var c in Children)
                    container.Add(c.CreateInstanceTree(resultDrawables));
                return container;
            }

            public override string GetCode() => $"new {nameof(FillFlowContainerNode)}({GetChildrenArrayCreationCode()})";
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
                return x.Children.Length == y.Children.Length &&
                       x.Children.Zip(y.Children, Equals).All(b => b);
            }

            public int GetHashCode(SceneNode node)
            {
                return node.Children.Select(GetHashCode).Prepend(node.Children.Length).Aggregate((x, y) => unchecked(x * 1234567 + y));
            }
        }

        public class ArbitraryDimensionList : Arbitrary<Dimension[]>
        {
            public readonly int Length;

            public ArbitraryDimensionList(int length)
            {
                Length = length;
            }

            public override Gen<Dimension[]> Generator => Gen.Sequence(Enumerable.Repeat(dimension, Length).ToArray());

            private static readonly Gen<Dimension> dimension = Gen.OneOf(new[]
            {
                new Dimension(GridSizeMode.Distributed),
                new Dimension(GridSizeMode.Relative, .5f),
                new Dimension(GridSizeMode.Absolute, .5f),
                new Dimension(GridSizeMode.Absolute, 1f),
                new Dimension(GridSizeMode.Absolute, 2f),
                new Dimension(GridSizeMode.AutoSize),
            }.Select(Gen.Constant));
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

            private static Gen<SceneNode> gen(int treeSize) => genChildren(treeSize - 1).SelectMany(children => Gen.OneOf(
                new[]
                {
                    Gen.Constant((SceneNode)new ContainerNode(children)).Yield(),
                    NoFillFlowContainer ? Enumerable.Empty<Gen<SceneNode>>() : Gen.Constant((SceneNode)new FillFlowContainerNode(children)).Yield(),
                    NoGridContainer
                        ? Enumerable.Empty<Gen<SceneNode>>()
                        : Gen.Choose(1, Math.Max(1, children.Length)).Select(rows => (SceneNode)new GridContainerNode(rows, (children.Length - 1) / rows + 1, children)).Yield()
                }.SelectMany(x => x)
            ));

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
                return new ContainerNode(node.Children.Where(x => x != target).Select(x => remove(x, target)).ToArray());
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
                var index = Array.FindIndex(float_values, y => x == y);
                return index == -1 ? float_values : float_values.Take(index);
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
                    var genPair =
                        node is FillFlowContainerNode ? for_fillflow_container :
                        node is GridContainerNode grid ? for_grid_container(grid.Rows, grid.Columns) :
                        for_container;
                    return genPair.Select(pair => new SceneModification(node.Name, pair.PropertyName, pair.Value));
                });
            }

            private static readonly float[] positive_float_values = { 1f, 2f, 0.5f, 1.5f, 3f, 1 / 3f, 2 / 3f, 4 / 3f, 5 / 3f };
            private static readonly float[] float_values = positive_float_values.Prepend(0f).Concat(positive_float_values.Select(x => -x)).ToArray();

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
            private static readonly Gen<FillDirection> filldirection = Gen.OneOf(new[] { FillDirection.Horizontal, FillDirection.Vertical, FillDirection.Full }.Select(Gen.Constant));

            private static readonly Gen<Dimension> dimension = Gen.OneOf(
                Gen.Constant(new Dimension(GridSizeMode.Distributed)),
                Gen.Constant(new Dimension(GridSizeMode.AutoSize)),
                size.Select(x => new Dimension(GridSizeMode.Absolute, x)),
                size.Select(x => new Dimension(GridSizeMode.Relative, x))
            );

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

            private static readonly Gen<Entry> for_fillflow_container = Gen.OneOf(
                for_container,
                entry(nameof(TestFillFlowContainer.Direction), filldirection),
                entry(nameof(TestFillFlowContainer.Spacing), vec(size)),
                entry(nameof(TestFillFlowContainer.MaximumSize), vec(size))
            );

            private static Gen<Entry> for_grid_container(int rows, int columns) => Gen.OneOf(
                for_container,
                entry(nameof(TestGridContainer.RowDimensions), Gen.ListOf(rows, dimension).Select(x => x.ToArray())),
                entry(nameof(TestGridContainer.ColumnDimensions), Gen.ListOf(columns, dimension).Select(x => x.ToArray()))
            );
        }

        public class SceneInstance
        {
            private readonly List<Drawable> nodes;
            public IReadOnlyList<Drawable> Nodes => nodes;
            public Drawable Root => Nodes.First();

            private readonly Dictionary<string, Drawable> nodeMap;
            public readonly Container Container;

            public Drawable GetNode(string name) => nodeMap[name];

            public SceneInstance(Scene scene)
            {
                nodes = new List<Drawable>();

                var root = scene.Root.CreateInstanceTree(nodes);

                nodeMap = new Dictionary<string, Drawable>(Nodes.Select(node => new KeyValuePair<string, Drawable>(node.Name, node)));

                Container = new Container
                {
                    Size = new Vector2(250),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopLeft,
                    Child = new Container
                    {
                        Scale = new Vector2(100),
                        Child = root
                    }
                };
            }

            private bool checkAutoSizeUndefinedCase(CompositeDrawable container, SceneModification modification)
            {
                if (!ForbidAutoSizeSomeCase) return true;

                Axes overwrite(Drawable c, string prop, Axes value)
                {
                    return c.Name == modification.NodeName && prop == modification.PropertyName ? (Axes)modification.Value : value;
                }

                var relativeAxes = container.InternalChildren.Select(x => overwrite(x, nameof(RelativeSizeAxes), x.RelativeSizeAxes) |
                                                                          overwrite(x, nameof(RelativePositionAxes), x.RelativePositionAxes) |
                                                                          overwrite(x, nameof(BypassAutoSizeAxes), x.Get<Axes>("bypassAutoSizeAxes"))).Prepend(~(Axes)0).Aggregate((x, y) => x & y);

                return (overwrite(container, nameof(AutoSizeAxes), container.AutoSizeAxes) & relativeAxes) == 0;
            }

            public bool CanExecute(SceneModification modification)
            {
                var node = GetNode(modification.NodeName);
                var autoSizeAxes = (node as CompositeDrawable)?.AutoSizeAxes ?? Axes.None;

                switch (modification.PropertyName)
                {
                    case nameof(RelativeSizeAxes):
                        if ((autoSizeAxes & (Axes)modification.Value) != 0)
                            return false;
                        goto case nameof(RelativePositionAxes);

                    case nameof(RelativePositionAxes):
                    {
                        if (node.Parent is TestFillFlowContainer && (Axes)modification.Value != Axes.None) return false;
                        goto case nameof(BypassAutoSizeAxes);
                    }
                    case nameof(BypassAutoSizeAxes):
                        return node.Parent == null || checkAutoSizeUndefinedCase(node.Parent, modification);

                    case nameof(AutoSizeAxes):
                    {
                        if (!(node is CompositeDrawable composite)) return false;
                        return (node.RelativeSizeAxes & (Axes)modification.Value) == 0 && checkAutoSizeUndefinedCase(composite, modification);
                    }

                    case nameof(Anchor):
                    {
                        if (node.Parent is TestFillFlowContainer && node.Parent.InternalChildren.Count > 1)
                            return false;
                        return true;
                    }

                    case nameof(Width):
                        return (autoSizeAxes & Axes.X) == 0;
                    case nameof(Height):
                        return (autoSizeAxes & Axes.Y) == 0;

                    case nameof(TestFillFlowContainer.Direction):
                        if ((Axes)modification.Value != Axes.None && ((CompositeDrawable)node).InternalChildren.All(c => c.RelativePositionAxes == Axes.None))
                            return false;
                        goto case nameof(TestFillFlowContainer.Spacing);
                    case nameof(TestFillFlowContainer.Spacing):
                    case nameof(TestFillFlowContainer.MaximumSize):
                        return node is TestCaseFillFlowContainer;

                    case nameof(X):
                    case nameof(Y):
                        return !ForbidChildPositionModificationForFlowContainer ||
                               !(node.Parent is FillFlowContainer);

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
                    node.PropagateInvalidateAll();
            }

            public void Update()
            {
                Container.UpdateSubTree();
                Container.UpdateSubTree();
            }

            public float[] LastState1, LastState2;

            public bool CheckStateValidity()
            {
                if (!Root.IsLoaded) throw new InvalidOperationException("The scene isn't loaded");

                Update();

                var state1 = GetDrawState();

                invalidate();
                Update();

                var state2 = GetDrawState();

                LastState1 = state1;
                LastState2 = state2;

                return almostEquals(state1, state2);
            }

            public float[] GetDrawState()
            {
                return Nodes.SelectMany(c =>
                {
                    var size = c.DrawSize;
                    var position = c.DrawPosition;
                    return new[] { size.X, size.Y, position.X, position.Y };
                }).ToArray();
            }
        }

        private static bool almostEquals(float[] x, float[] y)
        {
            // more tolerance for an array containing a big value
            var epsilon = 1e-2f * x.Concat(y).Prepend(1f).Select(Math.Abs).Max();
            return x.Length == y.Length && Enumerable.Range(0, x.Length).All(i => Precision.AlmostEquals(x[i], y[i], epsilon));
        }
    }
}
