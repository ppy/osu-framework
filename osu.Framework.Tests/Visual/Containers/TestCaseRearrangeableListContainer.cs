// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestItem : RearrangeableListItem
    {
        public Color4 Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1);
        public bool VariableHeight;
        public bool ShowHandles = true;
    }

    public class TestCaseRearrangeableListContainer : ManualInputManagerTestCase
    {
        private TestRearrangeableListContainer list, listWithSpacing, listWithVariableSizes, listWithoutHandles, listWithoutHandlesWithSpacing, listWithoutHandlesWithVariableSizes;
        private readonly List<TestRearrangeableListContainer> lists = new List<TestRearrangeableListContainer>();

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TestRearrangeableListContainer),
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f) },
                ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 1f / 3f) },
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
                                listWithVariableSizes = new TestRearrangeableListContainer(),
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
                                listWithoutHandles = new TestRearrangeableListContainer(),
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
                                    Spacing = new Vector2(20),
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
                                    Colour = new Color4(1f, 1f, 1f, 0.025f),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                listWithoutHandlesWithVariableSizes = new TestRearrangeableListContainer(),
                            }
                        },
                    },
                }
            };

            lists.AddRange(new[]
            {
                list,
                listWithSpacing,
                listWithVariableSizes,
                listWithoutHandles,
                listWithoutHandlesWithSpacing,
                listWithoutHandlesWithVariableSizes,
            });

            SetUp();
        }

        [SetUp]
        public override void SetUp()
        {
            foreach (var l in lists)
                l.Clear();

            for (int i = 0; i < 5; i++)
            {
                var colour = randomColour();
                foreach (var l in lists)
                {
                    var variableHeight = l == listWithVariableSizes || l == listWithoutHandlesWithVariableSizes;
                    var hideHandles = l == listWithoutHandles || l == listWithoutHandlesWithSpacing || l == listWithoutHandlesWithVariableSizes;

                    l.AddItem(new TestItem
                    {
                        VariableHeight = variableHeight,
                        ShowHandles = !hideHandles,
                        Colour = colour,
                    });
                }
            }
        }

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
                    l.AddItem(new TestItem
                    {
                        ShowHandles = l == list || l == listWithSpacing || l == listWithVariableSizes,
                    });
                });
                AddAssert("Ensure correct child count", () => l.Count == 5);
            }
        }

        private Color4 randomColour() => new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1);

        private class TestRearrangeableListContainer : RearrangeableListContainer<TestItem>
        {
            public int Count => ListContainer.Count;

            public IReadOnlyList<Drawable> Children => ListContainer.Children;

            public float GetLayoutPosition(TestDrawable d) => ListContainer.GetLayoutPosition(d);

            // GetChild returns the children in the order of addition, not in their rearranged order.
            public TestDrawable GetChild(int index) => (TestDrawable)Children[index];

            public Vector2 GetChildSize(int index) => GetChild(index).DrawSize;

            protected override DrawableRearrangeableListItem CreateDrawable(TestItem item) => new TestDrawable(item);

            public class TestDrawable : DrawableRearrangeableListItem
            {
                protected Button RemoveButton;

                protected Drawable DragHandle;

                protected override bool IsDraggableAt(Vector2 screenSpacePos) => !Model.ShowHandles || DragHandle.ReceivePositionalInputAt(screenSpacePos);

                protected override bool OnClick(ClickEvent e)
                {
                    if (RemoveButton.IsHovered)
                    {
                        OnRequestRemoval();

                        return true;
                    }

                    return false;
                }

                protected override bool OnMouseUp(MouseUpEvent e)
                {
                    IsBeingDragged = false;
                    return base.OnMouseUp(e);
                }

                public TestDrawable(TestItem item)
                    : base(item)
                {
                    Height = item.VariableHeight ? RNG.NextSingle(10, 50) : 25;
                    RelativeSizeAxes = Axes.X;
                    CornerRadius = 5;
                    Masking = true;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    var items = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = Model.ShowHandles ? 0.9f : 0.95f,
                            Colour = Model.Colour,
                        },
                        RemoveButton = new Button(FontAwesome.Solid.MinusSquare, Color4.DarkRed.Opacity(0.5f))
                        {
                            Width = 0.05f,
                        },
                    };

                    if (Model.ShowHandles)
                    {
                        DragHandle = new Button(FontAwesome.Solid.Bars, Color4.Transparent)
                        {
                            Width = 0.05f,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        };

                        items = items.Prepend(DragHandle).ToArray();
                    }

                    InternalChild = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Children = items,
                    };
                }

                protected class Button : Container
                {
                    public override bool HandlePositionalInput => true;

                    public Button(IconUsage icon, Color4 colour)
                    {
                        RelativeSizeAxes = Axes.Both;
                        InternalChildren = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colour
                            },
                            new SpriteIcon
                            {
                                RelativeSizeAxes = Axes.Both,
                                Icon = icon,
                                Scale = new Vector2(0.5f),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                            }
                        };
                    }
                }
            }
        }
    }
}
