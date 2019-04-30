// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicRearrangeableListContainer : RearrangeableListContainer<BasicRearrangeableItem>
    {
        protected override DrawableRearrangeableListItem CreateDrawable(BasicRearrangeableItem item) => new BasicDrawableRearrangeableListItem(item);

        public class BasicDrawableRearrangeableListItem : DrawableRearrangeableListItem
        {
            protected readonly Drawable DragHandle;

            protected override bool IsDraggableAt(Vector2 screenSpacePos) => DragHandle.ReceivePositionalInputAt(screenSpacePos);

            public BasicDrawableRearrangeableListItem(BasicRearrangeableItem item)
                : base(item)
            {
                Height = 25;
                RelativeSizeAxes = Axes.X;
                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        DragHandle = new Button(FontAwesome.Solid.Bars)
                        {
                            Width = 0.05f,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new Container
                        {
                            Width = 0.95f,
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
                                    Text = item.Text,
                                },
                            }
                        },
                    }
                };
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
