// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicRearrangeableListContainer : RearrangeableListContainer<BasicRearrangeableItem>
    {
        public bool UseDragHandle = true;

        public bool ShowRemoveButton = true;

        protected override DrawableRearrangeableListItem CreateDrawable(BasicRearrangeableItem item) => new BasicDrawableRearrangeableListItem(item)
        {
            UseDragHandle = UseDragHandle,
            ShowRemoveButton = ShowRemoveButton,
        };

        public class BasicDrawableRearrangeableListItem : DrawableRearrangeableListItem
        {
            public bool UseDragHandle = true;

            public bool ShowRemoveButton = true;

            protected override bool IsDraggableAt(Vector2 screenSpacePos) => (!ShowRemoveButton || !removeButton.IsHovered) && (!UseDragHandle || dragHandle.ReceivePositionalInputAt(screenSpacePos));

            private Drawable dragHandle;
            private Drawable removeButton;

            public BasicDrawableRearrangeableListItem(BasicRearrangeableItem item)
                : base(item)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                var contentWidth = 1f;

                if (UseDragHandle)
                    contentWidth -= 0.05f;

                if (ShowRemoveButton)
                    contentWidth -= 0.05f;

                var items = new Drawable[]
                {
                    new Container
                    {
                        Width = contentWidth,
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        CornerRadius = 2,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.DarkSlateGray,
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,

                                AllowMultiline = false,
                                Padding = new MarginPadding(5),
                                Text = Model.Text,
                            }
                        },
                    },
                };

                if (UseDragHandle)
                {
                    dragHandle = new Button(FontAwesome.Solid.Bars)
                    {
                        Width = 0.05f,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    };

                    items = items.Prepend(dragHandle).ToArray();
                }

                if (ShowRemoveButton)
                {
                    var drawable = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        CornerRadius = 2,
                        Masking = true,
                        Width = 0.05f,
                        Child = removeButton = new Button(FontAwesome.Solid.Times)
                        {
                            Colour = Color4.DarkRed,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    };

                    items = items.Append(drawable).ToArray();
                }

                Height = 25;
                RelativeSizeAxes = Axes.X;
                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = items,
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (removeButton.IsHovered)
                {
                    OnRequestRemoval();

                    return true;
                }

                return false;
            }

            protected class Button : Container
            {
                public override bool HandlePositionalInput => true;

                public Button(IconUsage icon)
                {
                    RelativeSizeAxes = Axes.Both;
                    InternalChildren = new Drawable[]
                    {
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
