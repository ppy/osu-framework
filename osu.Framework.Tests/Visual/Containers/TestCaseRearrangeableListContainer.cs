// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestCaseRearrangeableListContainer : ManualInputManagerTestCase
    {
        private TestRearrangeableListContainer list, listWithSpacing, listWithoutHandles, listWithoutHandlesWithSpacing;
        private readonly List<TestRearrangeableListContainer> lists = new List<TestRearrangeableListContainer>();

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicRearrangeableListContainer),
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f) },
                ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f) },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = new Color4(1f, 1f, 1f, 0.05f),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                list = new TestRearrangeableListContainer(),
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = new Color4(1f, 1f, 1f, 0.025f),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                listWithSpacing = new TestRearrangeableListContainer
                                {
                                    Spacing = new Vector2(20),
                                },
                            }
                        },
                    },
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = new Color4(1f, 1f, 1f, 0.025f),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                listWithoutHandles = new TestRearrangeableListContainer
                                {
                                    UseDragHandle = false
                                },
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = new Color4(1f, 1f, 1f, 0.05f),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                listWithoutHandlesWithSpacing = new TestRearrangeableListContainer
                                {
                                    UseDragHandle = false,
                                    Spacing = new Vector2(20),
                                },
                            }
                        },
                    },
                }
            };

            lists.AddRange(new[]
            {
                list,
                listWithSpacing,
                listWithoutHandles,
                listWithoutHandlesWithSpacing,
            });
        }

        [SetUp]
        public override void SetUp() => Schedule(() =>
        {
            foreach (var l in lists)
                l.Clear();

            for (int i = 0; i < 5; i++)
            {
                foreach (var l in lists)
                {
                    l.AddItem(new BasicRearrangeableItem($"Test {i}"));
                }
            }
        });

        [Test]
        public void SortingTests()
        {
            foreach (var l in lists)
            {
                // drag item down
                AddStep("Hover item", () => { InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(new Vector2(10, l.GetChildSize(0).Y * 0.5f))); });
                AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
                AddStep("Drag downward", () =>
                {
                    // calculate the offset based on the first 3 items in the list, plus spacing
                    var dragOffset = l.GetChildSize(0).Y + l.GetChildSize(1).Y + l.GetChildSize(2).Y * 0.75f + l.Spacing.Y * 2;
                    InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(new Vector2(10, dragOffset)));
                });
                AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
                AddAssert("Ensure item is now third", () => l.GetLayoutPosition(l.GetChild(0)) == 2);

                // drag item back up
                AddStep("Hover item", () => { InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(new Vector2(10, l.GetChildSize(0).Y * 0.5f))); });
                AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
                AddStep("Drag upward", () =>
                {
                    // calculate the offset based on the 2 items above it in the list, plus spacing
                    var dragOffset = l.GetChildSize(1).Y + l.GetChildSize(2).Y * 0.75f + l.Spacing.Y * 2;
                    InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(new Vector2(10, -dragOffset)));
                });
                AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
                AddAssert("Ensure item is now first again", () => l.GetLayoutPosition(l.GetChild(0)) == 0);
            }
        }

        [Test]
        public void AddRemoveTests()
        {
            foreach (var l in lists)
            {
                AddAssert("Ensure correct starting count", () => l.Count == 5);
                AddStep("Hover Remove Button", () => { InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(l.GetChildSize(0) + new Vector2(-10, -l.GetChildSize(0).Y * 0.5f))); });
                AddStep("RemoveItem", () => InputManager.Click(MouseButton.Left));
                AddAssert("Ensure correct child count", () => l.Count == 4);
                AddStep("AddItem", () =>
                {
                    l.AddItem(new BasicRearrangeableItem($"Test {l.Count + 1}"));
                });
                AddAssert("Ensure correct child count", () => l.Count == 5);
            }
        }

        private class TestRearrangeableListContainer : BasicRearrangeableListContainer
        {
            public int Count => ListContainer.Count;

            public IReadOnlyList<Drawable> Children => ListContainer.Children;

            public float GetLayoutPosition(BasicDrawableRearrangeableListItem d) => ListContainer.GetLayoutPosition(d);

            // GetChild returns the children in the order of addition, not in their rearranged order.
            public BasicDrawableRearrangeableListItem GetChild(int index) => (BasicDrawableRearrangeableListItem)Children[index];

            public Vector2 GetChildSize(int index) => GetChild(index).DrawSize;
        }
    }
}
