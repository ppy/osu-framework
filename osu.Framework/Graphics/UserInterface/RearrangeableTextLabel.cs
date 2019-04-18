// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class RearrangeableTextLabel : FillFlowContainer, IRearrangeableDrawable<RearrangeableTextLabel>, IHasText
    {
        public event Action<RearrangeableTextLabel> RequestRemoval;

        public bool IsDraggable => IsSelected;

        protected bool IsSelected;
        protected readonly Button DragButton;

        private readonly SpriteText label;

        public RearrangeableTextLabel(string text)
        {
            Height = 25;
            RelativeSizeAxes = Axes.X;
            Direction = FillDirection.Horizontal;
            Children = new Drawable[]
            {
                DragButton = new Button(FontAwesome.Solid.Bars, Color4.DarkSlateGray.Opacity(0f))
                {
                    Width = 0.05f,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft
                },
                new Container
                {
                    Width = 0.9f,
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
                        label = new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,

                            AllowMultiline = false,
                            Padding = new MarginPadding(5),
                            Text = text,
                        },
                    }
                }
            };
        }

        public string Text
        {
            get => label.Text;
            set => label.Text = value;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (DragButton.IsHovered)
                IsSelected = true;

            return base.OnMouseDown(e);
        }

        protected override bool OnMouseUp(MouseUpEvent e)
        {
            IsSelected = false;
            return base.OnMouseUp(e);
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
