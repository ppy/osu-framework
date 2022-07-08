// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Graphics.Visualisation
{
    internal class PropertyDisplay : Container
    {
        private readonly FillFlowContainer flow;

        private Bindable<Drawable> inspectedDrawable;

        protected override Container<Drawable> Content => flow;

        public PropertyDisplay()
        {
            RelativeSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.GreenDarker,
                    RelativeSizeAxes = Axes.Both,
                },
                new BasicScrollContainer<Drawable>
                {
                    Padding = new MarginPadding(10),
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarOverlapsContent = false,
                    Child = flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical
                    }
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load(Bindable<Drawable> inspected)
        {
            inspectedDrawable = inspected.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inspectedDrawable.BindValueChanged(inspected => updateProperties(inspected.NewValue), true);
        }

        private void updateProperties(IDrawable source)
        {
            Clear();

            if (source == null)
                return;

            var allMembers = new HashSet<MemberInfo>(new MemberInfoComparer());

            foreach (var type in source.GetType().EnumerateBaseTypes())
            {
                type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m is FieldInfo || (m is PropertyInfo pi && pi.GetMethod != null && !pi.GetIndexParameters().Any()))
                    .ForEach(m => allMembers.Add(m));
            }

            // Order by upper then lower-case, and exclude auto-generated backing fields of properties
            AddRange(allMembers.OrderBy(m => m.Name[0]).ThenBy(m => m.Name)
                               .Where(m => m.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                               .Where(m => m.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State != DebuggerBrowsableState.Never)
                               .Select(m => new PropertyItem(m, source)));
        }

        private class PropertyItem : Container
        {
            private readonly SpriteText valueText;
            private readonly Box changeMarker;
            private readonly Func<object> getValue;

            public PropertyItem(MemberInfo info, IDrawable d)
            {
                Type type;

                switch (info)
                {
                    case PropertyInfo propertyInfo:
                        type = propertyInfo.PropertyType;
                        getValue = () => propertyInfo.GetValue(d);
                        break;

                    case FieldInfo fieldInfo:
                        type = fieldInfo.FieldType;
                        getValue = () => fieldInfo.GetValue(d);
                        break;

                    default:
                        throw new ArgumentException(@"Not a value member.", nameof(info));
                }

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                AddRangeInternal(new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding
                        {
                            Right = 6
                        },
                        Child = new FillFlowContainer<SpriteText>
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10f),
                            Children = new[]
                            {
                                new SpriteText
                                {
                                    Text = info.Name,
                                    Colour = FrameworkColour.Yellow,
                                    Font = FrameworkFont.Regular
                                },
                                new SpriteText
                                {
                                    Text = $@"[{type.Name}]:",
                                    Colour = FrameworkColour.YellowGreen,
                                    Font = FrameworkFont.Regular
                                },
                                valueText = new SpriteText
                                {
                                    Colour = Color4.White,
                                    Font = FrameworkFont.Regular
                                },
                            }
                        }
                    },
                    changeMarker = new Box
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
                catch (Exception e)
                {
                    value = $@"<{((e as TargetInvocationException)?.InnerException ?? e).GetType().ReadableName()} occured during evaluation>";
                }

                // An alternative of object.Equals, which is banned.
                if (!EqualityComparer<object>.Default.Equals(value, lastValue))
                {
                    changeMarker.ClearTransforms();
                    changeMarker.Alpha = 0.8f;
                    changeMarker.FadeOut(200);
                }

                lastValue = value;
                valueText.Text = value.ToString();
            }
        }

        private class MemberInfoComparer : IEqualityComparer<MemberInfo>
        {
            public bool Equals(MemberInfo x, MemberInfo y) => x?.Name == y?.Name;

            public int GetHashCode(MemberInfo obj) => obj.Name.GetHashCode();
        }
    }
}
