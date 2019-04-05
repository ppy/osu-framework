// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
    public class TestCaseRearrangeableListContainer : ManualInputManagerTestCase
    {
        private TestRearrangeableListContainer<TestDrawable> list, listWithSpacing, listWithVariableSizes, listWithoutHandles, listWithoutHandlesWithSpacing, listWithoutHandlesWithVariableSizes;
        private readonly List<TestRearrangeableListContainer<TestDrawable>> lists = new List<TestRearrangeableListContainer<TestDrawable>>();

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TestRearrangeableListContainer<TestDrawable>),
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
                                list = new TestRearrangeableListContainer<TestDrawable>(),
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
                                listWithSpacing = new TestRearrangeableListContainer<TestDrawable>
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
                                listWithVariableSizes = new TestRearrangeableListContainer<TestDrawable>(),
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
                                listWithoutHandles = new TestRearrangeableListContainer<TestDrawable>(),
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
                                listWithoutHandlesWithSpacing = new TestRearrangeableListContainer<TestDrawable>
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
                                listWithoutHandlesWithVariableSizes = new TestRearrangeableListContainer<TestDrawable>(),
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

            Reset();
        }

        [SetUp]
        public void Reset()
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
                    l.AddItem(generateItem(variableHeight, !hideHandles, colour));
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
                if (l == listWithoutHandles || l == listWithoutHandlesWithSpacing || l == listWithoutHandlesWithVariableSizes)
                    break;

                AddAssert("Ensure correct child count", () => l.Count == 5);
                AddStep("Hover Remove Button", () => { InputManager.MoveMouseTo(l.GetChild(0).ToScreenSpace(l.GetChildSize(0) + new Vector2(-10, -l.GetChildSize(0).Y * 0.5f))); });
                AddStep("RemoveItem", () => InputManager.Click(MouseButton.Left));
                AddAssert("Ensure correct child count", () => l.Count == 4);
                AddStep("AddItem", () => { l.AddItem(generateItem(false, true, randomColour())); });
                AddAssert("Ensure correct child count", () => l.Count == 5);
            }
        }

        private TestDrawable generateItem(bool variableHeight, bool useHandles, Color4 colour)
        {
            if (!variableHeight)
                return useHandles
                    ? new TestDrawable(colour)
                    : new TestDrawableNoHandle(colour);

            return useHandles
                ? new TestDrawable(colour)
                {
                    Height = RNG.NextSingle(10, 50),
                }
                : new TestDrawableNoHandle(colour)
                {
                    Height = RNG.NextSingle(10, 50),
                };
        }

        private Color4 randomColour() => new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1);

        private class TestDrawableNoHandle : TestDrawable
        {
            protected override bool OnMouseDown(MouseDownEvent e)
            {
                IsSelected = true;
                return base.OnMouseDown(e);
            }

            protected override bool OnClick(ClickEvent e) => false;

            public TestDrawableNoHandle(Color4 colour)
                : base(colour)
            {
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 1f,
                    Colour = colour,
                };
            }
        }

        private class TestDrawable : FillFlowContainer, IRearrangeableDrawable<TestDrawable>
        {
            public event Action<TestDrawable> RequestRemoval;

            public bool IsDraggable => IsSelected;

            protected bool IsSelected;
            protected readonly Button DragButton;
            protected readonly Button RemoveButton;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (DragButton.IsHovered)
                    IsSelected = true;

                return base.OnMouseDown(e);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (RemoveButton.IsHovered)
                {
                    RequestRemoval?.Invoke(this);

                    return true;
                }

                return false;
            }

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                IsSelected = false;
                return base.OnMouseUp(e);
            }

            public TestDrawable(Color4 colour)
            {
                Height = 25;
                RelativeSizeAxes = Axes.X;
                Direction = FillDirection.Horizontal;
                CornerRadius = 5;
                Masking = true;
                Children = new Drawable[]
                {
                    DragButton = new Button(FontAwesome.Solid.Bars, Color4.DarkSlateGray.Opacity(0.5f))
                    {
                        Width = 0.05f,
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0.9f,
                        Colour = colour,
                    },
                    RemoveButton = new Button(FontAwesome.Solid.MinusSquare, Color4.DarkRed.Opacity(0.5f))
                    {
                        Width = 0.05f,
                    },
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

        private class TestRearrangeableListContainer<T> : RearrangeableListContainer<T> where T : Drawable, IRearrangeableDrawable<T>
        {
            public IReadOnlyList<Drawable> Children => ListContainer.Children;
            public int Count => ListContainer.Count;
            public float GetLayoutPosition(T d) => ListContainer.GetLayoutPosition(d);
            public TestDrawable GetChild(int index) => (TestDrawable)Children[index];
            public Vector2 GetChildSize(int index) => GetChild(index).DrawSize;
        }
    }
}
