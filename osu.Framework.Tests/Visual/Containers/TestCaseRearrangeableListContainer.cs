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
    public class TestCaseRearrangeableListContainer : ManualInputManagerTestCase
    {
        private TestRearrangeableListContainer<TestDrawable> list;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TestRearrangeableListContainer<TestDrawable>),
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(list = new TestRearrangeableListContainer<TestDrawable>());
        }

        [SetUp]
        public void SetUp()
        {
            list.Clear();
            for (int i = 0; i < 50; i++)
                list.AddItem(generateItem());
        }

        [Test]
        public void SortingTests()
        {
            AddStep("Hover item", () => { InputManager.MoveMouseTo(getFirstChild().ToScreenSpace(new Vector2(10, getChildDrawableSize().Y * 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag downward", () => { InputManager.MoveMouseTo(getFirstChild().ToScreenSpace(new Vector2(10, getChildDrawableSize().Y * 2.5f))); });
            AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
            AddAssert("Ensure item is now third", () => list.GetLayoutPosition(getFirstChild()) == 2);

            AddStep("Hover item", () => { InputManager.MoveMouseTo(getFirstChild().ToScreenSpace(new Vector2(10, getChildDrawableSize().Y * 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag upward", () => { InputManager.MoveMouseTo(getFirstChild().ToScreenSpace(new Vector2(10, -getChildDrawableSize().Y * 1.5f))); });
            AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
            AddAssert("Ensure item is now first again", () => list.GetLayoutPosition(getFirstChild()) == 0);
        }

        [Test]
        public void AddRemoveTests()
        {
            AddAssert("Ensure correct child count", () => getChildCount() == 50);
            AddStep("Hover Remove Button", () => { InputManager.MoveMouseTo(getFirstChild().ToScreenSpace(getChildDrawableSize() + new Vector2(-20, -getChildDrawableSize().Y * 0.5f))); });
            AddStep("RemoveItem", () => InputManager.Click(MouseButton.Left));
            AddAssert("Ensure correct child count", () => getChildCount() == 49);
            AddStep("AddItem", () => { list.AddItem(generateItem()); });
            AddAssert("Ensure correct child count", () => getChildCount() == 50);
        }

        private int getChildCount() => list.Count;

        private TestDrawable getFirstChild() => (TestDrawable)list.Children.First();

        private Vector2 getChildDrawableSize() => getFirstChild().DrawSize + list.Spacing;

        private TestDrawable generateItem()
        {
            return new TestDrawable();
        }

        private class TestDrawable : FillFlowContainer, IRearrangeableDrawable<TestDrawable>
        {
            public event Action<TestDrawable> RequestRemoval;

            public bool IsDraggable => isSelected;

            private bool isSelected;
            private readonly Button dragButton;
            private readonly Button removeButton;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (dragButton.IsHovered)
                    isSelected = true;

                return base.OnMouseDown(e);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (removeButton.IsHovered)
                {
                    RequestRemoval?.Invoke(this);

                    return true;
                }

                return false;
            }

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                isSelected = false;
                return base.OnMouseUp(e);
            }

            public TestDrawable()
            {
                Height = 30;
                RelativeSizeAxes = Axes.X;
                Direction = FillDirection.Horizontal;
                CornerRadius = 5;
                Masking = true;
                Children = new Drawable[]
                {
                    dragButton = new Button(FontAwesome.Solid.Bars, Color4.DarkSlateGray.Opacity(0.5f))
                    {
                        Width = 0.05f,
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0.9f,
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                    },
                    removeButton = new Button(FontAwesome.Solid.MinusSquare, Color4.DarkRed.Opacity(0.5f))
                    {
                        Width = 0.05f,
                    },
                };
            }

            private class Button : Container
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
        }
    }
}
