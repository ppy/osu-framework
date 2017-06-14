// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            Alpha = 0.7f;
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

        public void UpdateFrom(Drawable source)
        {
            Clear(true);

            if (source == null)
            {
                State = Visibility.Hidden;
                return;
            }
            Type type = source.GetType();

            Add(((IEnumerable<MemberInfo>)type.GetProperties(BindingFlags.Instance | BindingFlags.Public))      // Get all properties
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public))                            // And all fields
                .OrderBy(member => member.Name)
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).OrderBy(field => field.Name))  // Include non-public fields at the end
                .Select(member => new PropertyItem(member, source)));
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

        protected override bool OnHover(InputState state)
        {
            FadeIn(300);

            return true;
        }

        protected override void OnHoverLost(InputState state)
        {
            using (BeginDelayedSequence(500, true))
            {
                FadeTo(0.7f, 300);
            }
        }

        private class PropertyItem : Container
        {
            private readonly SpriteText valueText;
            private readonly Box changeMarker;
            private readonly Func<object> getValue;

            public PropertyItem(MemberInfo info, IDrawable d)
            {
                Type type;
                switch (info.MemberType)
                {
                    case MemberTypes.Property:
                        PropertyInfo propertyInfo = (PropertyInfo)info;
                        type = propertyInfo.PropertyType;
                        getValue = () => propertyInfo.GetValue(d);
                        break;

                    case MemberTypes.Field:
                        FieldInfo fieldInfo = (FieldInfo)info;
                        type = fieldInfo.FieldType;
                        getValue = () => fieldInfo.GetValue(d);
                        break;

                    default:
                        throw new NotImplementedException(@"Not a value member.");
                }

                RelativeSizeAxes = Axes.X;
                Height = 20f;
                AddInternal(new Drawable[]
                {
                new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding()
                    {
                        Right = 6
                    },
                    Children = new Drawable[]
                    {
                        new FillFlowContainer<SpriteText>()
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10f),
                            Children = new[]
                            {
                                new SpriteText()
                                {
                                    Text = info.Name,
                                    TextSize = Height,
                                    Colour = Color4.LightBlue,
                                },
                                new SpriteText()
                                {
                                    Text = $@"[{type.Name}]:",
                                    TextSize = Height,
                                    Colour = Color4.MediumPurple,
                                },
                                valueText = new SpriteText()
                                {
                                    TextSize = Height,
                                    Colour = Color4.White,
                                },
                            }
                        }
                    }
                },
                changeMarker = new Box()
                {
                    Size = new Vector2(4, 18),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Colour = Color4.Red
                }
                });

                // Update the value once
                updateValue();
            }

            protected override void Update()
            {
                base.Update();

                updateValue();
            }

            private object lastValue;

            private void updateValue()
            {
                object value;
                try
                {
                    value = getValue() ?? "<null>";
                }
                catch
                {
                    value = @"<Cannot evaluate>";
                }

                if (!value.Equals(lastValue))
                {
                    changeMarker.ClearTransforms();
                    changeMarker.Alpha = 0.8f;
                    changeMarker.FadeOut(200);
                }

                lastValue = value;
                valueText.Text = value.ToString();
            }
        }
    }
}
