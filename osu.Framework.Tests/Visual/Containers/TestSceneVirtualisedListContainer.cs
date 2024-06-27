// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;

namespace osu.Framework.Tests.Visual.Containers
{
    [TestFixture]
    public partial class TestSceneVirtualisedListContainer : FrameworkTestScene
    {
        [Test]
        public void TestNaiveList()
        {
            AddStep("create list", () => Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    ChildrenEnumerable = Enumerable.Range(1, 10000).Select(i => new DrawableItem { Current = { Value = $"Item #{i}" } })
                }
            });
        }

        [Test]
        public void TestVirtualisedList()
        {
            ExampleVirtualisedList list = null!;
            AddStep("create list", () =>
            {
                Child = list = new ExampleVirtualisedList
                {
                    RelativeSizeAxes = Axes.Both,
                };
                list.RowData.AddRange(Enumerable.Range(1, 10000).Select(i => $"Item #{i}"));
            });
            AddStep("replace items", () =>
            {
                list.RowData.Clear();
                list.RowData.AddRange(Enumerable.Range(10001, 10000).Select(i => $"Item #{i}"));
            });
        }

        [Test]
        public void TestVirtualisedListDisposal()
        {
            ExampleVirtualisedList list = null!;
            AddStep("create list nested in container", () =>
            {
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = list = new ExampleVirtualisedList
                        {
                            RelativeSizeAxes = Axes.Both,
                        }
                    }
                };
                list.RowData.AddRange(Enumerable.Range(1, 10000).Select(i => $"Item #{i}"));
            });
            AddStep("clear", Clear);
            AddUntilStep("wait for async disposal", () => list.IsDisposed);
        }

        [Test]
        public void TestCollectionChangeHandling()
        {
            ExampleVirtualisedList list = null!;

            AddStep("create list", () =>
            {
                Child = list = new ExampleVirtualisedList
                {
                    RelativeSizeAxes = Axes.Both,
                };
                list.RowData.AddRange(Enumerable.Range(1, 10).Select(i => $"Item #{i}"));
            });

            AddStep("insert at start", () => list.RowData.Insert(0, "first"));
            AddStep("insert at end", () => list.RowData.Add("last"));
            AddStep("remove from middle", () => list.RowData.RemoveAt(5));
            AddStep("move forward", () => list.RowData.Move(0, 4));
            AddStep("move back", () => list.RowData.Move(5, 2));
            AddStep("replace", () => list.RowData[3] = "replacing");
            AddStep("clear", () => list.RowData.Clear());
        }

        private partial class DrawableItem : PoolableDrawable, IHasCurrentValue<string>
        {
            public const int HEIGHT = 25;

            private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>();

            private Box background = null!;
            private SpriteText text = null!;

            public Bindable<string> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                Height = HEIGHT;
                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark,
                    },
                    text = new SpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Margin = new MarginPadding { Left = 10, },
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(_ => text.Text = Current.Value, true);
                updateState();
                FinishTransforms(true);
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return true;
            }

            private void updateState() => background.FadeTo(IsHovered ? 1 : 0, 300, Easing.OutQuint);

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
            }
        }

        private partial class ExampleVirtualisedList : VirtualisedListContainer<string, DrawableItem>
        {
            public ExampleVirtualisedList()
                : base(DrawableItem.HEIGHT, 50)
            {
            }

            protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer();
        }
    }
}
