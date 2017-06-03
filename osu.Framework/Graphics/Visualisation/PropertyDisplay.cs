// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Visualisation
{
    internal class PropertyDisplay : OverlayContainer
    {
        private readonly FillFlowContainer flow;

        private const float width = 600;
        private const float height = 400;

        private readonly Box titleBar;

        protected override Container<Drawable> Content => flow;

        public PropertyDisplay()
        {
            Masking = true;
            CornerRadius = 5;
            Position = new Vector2(500, 100);
            Size = new Vector2(width, height);
            AddInternal(new Drawable[]
            {
                new Box
                {
                    Colour = new Color4(30, 30, 30, 240),
                    RelativeSizeAxes = Axes.Both,
                    Depth = 0
                },
                titleBar = new Box //title decoration
                {
                    Colour = Color4.DarkBlue,
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 20),
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 20 },
                    Children = new[]
                    {
                        new ScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ScrollbarOverlapsContent = false,
                            Children = new[]
                            {
                                flow = new FillFlowContainer()
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical
                                }
                            },
                        }
                    }
                },
            });
        }

        protected override bool OnDragStart(InputState state) => titleBar.Contains(state.Mouse.NativeState.Position);

        protected override bool OnDrag(InputState state)
        {
            Position += state.Mouse.Delta;
            return base.OnDrag(state);
        }

        protected override void PopIn()
        {
            FadeTo(0.7f, 100);
        }
        protected override void PopOut()
        {
            FadeOut(100);
        }
    }

    internal class PropertyItem : ClickableContainer
    {
        private readonly SpriteText nameText;
        private readonly SpriteText valueText;

        private readonly Box box;
        private readonly Func<string> getValue;     // Use delegate for performance

        public PropertyItem(MemberInfo info, IDrawable d)
        {
            try
            {
                switch (info.MemberType)
                {
                    case MemberTypes.Property:
                        getValue = ((PropertyInfo)info).GetValue(d).ToString;
                        break;

                    case MemberTypes.Field:
                        getValue = ((FieldInfo)info).GetValue(d).ToString;
                        break;
                }
            }
            catch
            {
                getValue = () => @"<Cannot evaluate>";
            }

            RelativeSizeAxes = Axes.X;
            Height = 20f;
            AddInternal(new Drawable[]
            {
                box = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0f
                },
                nameText = new SpriteText()
                {
                    Text = info.Name,
                    TextSize = Height
                },
                valueText = new SpriteText()
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    TextSize = Height
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            // Update value
            valueText.Text = getValue();
        }

        protected override bool OnHover(InputState state)
        {
            box.FadeTo(0.2f, 100);

            return true;
        }
        protected override void OnHoverLost(InputState state)
        {
            box.FadeOut(100);
        }
    }
}
