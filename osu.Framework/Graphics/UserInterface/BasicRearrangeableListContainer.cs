// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicRearrangeableListContainer<TModel> : RearrangeableListContainer<TModel>
    {
        protected override FillFlowContainer<RearrangeableListItem<TModel>> CreateListFillFlowContainer() => base.CreateListFillFlowContainer().With(d =>
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

        protected sealed override RearrangeableListItem<TModel> CreateDrawable(TModel item) => CreateBasicItem(item).With(d =>
        {
            d.RequestRemoval += _ => Items.Remove(item);
        });

        protected virtual BasicRearrangeableListItem<TModel> CreateBasicItem(TModel item) => new BasicRearrangeableListItem<TModel>(item);
    }

    public class BasicRearrangeableListItem<TModel> : RearrangeableListItem<TModel>
    {
        internal Action<RearrangeableListItem<TModel>> RequestRemoval;

        private readonly bool removable;
        private Drawable dragHandle;

        public BasicRearrangeableListItem(TModel item, bool removable = false)
            : base(item)
        {
            this.removable = removable;
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
                        new Button(FontAwesome.Solid.Times)
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = removable ? 25 : 0, // https://github.com/ppy/osu-framework/issues/3214
                            Colour = Color4.DarkRed,
                            Alpha = removable ? 1 : 0,
                            Action = () => RequestRemoval?.Invoke(this),
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
