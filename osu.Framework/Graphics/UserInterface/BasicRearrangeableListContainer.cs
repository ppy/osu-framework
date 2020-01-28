// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicRearrangeableListContainer<T> : RearrangeableListContainer<T>
        where T : IEquatable<T>
    {
        /// <summary>
        /// Whether items should display a drag handle.
        /// </summary>
        public readonly Bindable<bool> ShowDragHandle = new Bindable<bool>();

        /// <summary>
        /// Whether items should display a remove button.
        /// </summary>
        public readonly Bindable<bool> ShowRemoveButton = new Bindable<bool>();

        protected override FillFlowContainer<DrawableRearrangeableListItem> CreateListFillFlowContainer() => base.CreateListFillFlowContainer().With(d =>
        {
            d.LayoutDuration = 160;
            d.LayoutEasing = Easing.OutQuint;
            d.Spacing = new Vector2(1);
        });

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer().With(d =>
        {
            d.RelativeSizeAxes = Axes.Both;
            d.ScrollbarOverlapsContent = false;
            d.Padding = new MarginPadding(5);
        });

        protected override DrawableRearrangeableListItem CreateDrawable(T item) => new BasicDrawableRearrangeableListItem(item)
        {
            ShowDragHandle = { BindTarget = ShowDragHandle },
            ShowRemoveButton = { BindTarget = ShowRemoveButton },
            RequestRemoval = d => RemoveItem(d.Model)
        };

        public class BasicDrawableRearrangeableListItem : DrawableRearrangeableListItem
        {
            internal Action<DrawableRearrangeableListItem> RequestRemoval;
            internal readonly Bindable<bool> ShowDragHandle = new Bindable<bool>();
            internal readonly Bindable<bool> ShowRemoveButton = new Bindable<bool>();

            private Drawable dragHandle;
            private Drawable removeButton;

            public BasicDrawableRearrangeableListItem(T item)
                : base(item)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Height = 25;
                RelativeSizeAxes = Axes.X;

                InternalChild = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Content = new[]
                    {
                        new[]
                        {
                            dragHandle = new Button(FontAwesome.Solid.Bars)
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 25,
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                CornerRadius = 2,
                                Masking = true,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.DarkSlateGray,
                                    },
                                    new SpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        AllowMultiline = false,
                                        Padding = new MarginPadding(5),
                                        Text = Model.ToString(),
                                    }
                                },
                            },
                            removeButton = new Button(FontAwesome.Solid.Times)
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 25,
                                Colour = Color4.DarkRed,
                                Action = () => RequestRemoval?.Invoke(this)
                            },
                        },
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Distributed),
                        new Dimension(GridSizeMode.AutoSize),
                    }
                };

                ShowDragHandle.BindValueChanged(shown => dragHandle.Alpha = shown.NewValue ? 1 : 0);
                ShowRemoveButton.BindValueChanged(shown => removeButton.Alpha = shown.NewValue ? 1 : 0);
            }

            protected override bool IsDraggableAt(Vector2 screenSpacePos) => dragHandle.IsHovered;

            protected internal class Button : Container
            {
                public Action Action;

                public override bool HandlePositionalInput => true;

                public Button(IconUsage icon)
                {
                    RelativeSizeAxes = Axes.Both;

                    InternalChild = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Scale = new Vector2(0.5f),
                        Icon = icon,
                    };
                }

                protected override bool OnClick(ClickEvent e)
                {
                    Action?.Invoke();
                    return true;
                }
            }
        }
    }
}
