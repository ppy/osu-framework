// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Visualisation
{
    internal class PropertyDisplay : Container
    {
        private readonly FillFlowContainer flow;

        private Bindable<object> inspectedTarget;

        [Resolved]
        private ITreeContainer tree { get; set; }

        protected override Container<Drawable> Content => flow;

        private PropertyDisplayMode displayMode = PropertyDisplayMode.ListAll;
        public PropertyDisplayMode DisplayMode
        {
            get => displayMode;
            set
            {
                if (displayMode == value)
                    return;

                displayMode = value;
                updateProperties(inspectedTarget.Value);
            }
        }

        public PropertyDisplay()
        {
            RelativeSizeAxes = Axes.Both;

            FillFlowContainer buttonContainer;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.GreenDarker,
                    RelativeSizeAxes = Axes.Both,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Distributed),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Relative, 1),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            buttonContainer = new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Spacing = new Vector2(7),
                                Padding = new MarginPadding(10),
                            },
                        },
                        new Drawable[]
                        {
                            new BasicScrollContainer<Drawable>
                            {
                                Padding = new MarginPadding(10) { Top = 0 },
                                RelativeSizeAxes = Axes.Both,
                                ScrollbarOverlapsContent = false,
                                Child = flow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical
                                }
                            },
                        },
                    }
                }
            });

            void addButton(string text, Action action)
            {
                buttonContainer.Add(new BasicButton
                {
                    Size = new Vector2(100, 25),
                    Text = text,
                    Action = action,
                });
            }

            addButton("show all", () => DisplayMode = PropertyDisplayMode.ListAll);
            addButton("group by type", () => DisplayMode = PropertyDisplayMode.GroupByClass);
            addButton("reset", () => updateProperties(inspectedTarget.Value));
        }

        [BackgroundDependencyLoader]
        private void load(Bindable<object> inspected)
        {
            inspectedTarget = inspected.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inspectedTarget.BindValueChanged(inspected => updateProperties(inspected.NewValue), true);
        }

        private void updateProperties(object source)
        {
            Clear();
            flow.FindClosestParent<BasicScrollContainer<Drawable>>().ScrollToStart();

            if (source == null)
                return;

            var allMembers = new HashSet<MemberInfo>(new MemberInfoComparer());
            var typeMembers = new List<(Type, HashSet<MemberInfo>)>();

            foreach (var type in source.GetType().EnumerateBaseTypes().Concat(source.GetType().GetInterfaces()))
            {
                var members = new HashSet<MemberInfo>(new MemberInfoComparer());
                typeMembers.Add((type, members));
                type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m is FieldInfo || m is PropertyInfo pi && pi.GetMethod != null && !pi.GetIndexParameters().Any())
                    .ForEach(m =>
                    {
                        allMembers.Add(m);
                        members.Add(m);
                    });
            }

            // Order by upper then lower-case, and exclude auto-generated backing fields of properties
            IEnumerable<PropertyItem> generateItems(IEnumerable<MemberInfo> members) =>
                members.OrderBy(m => m.Name[0]).ThenBy(m => m.Name)
                       .Where(m => m.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                       .Where(m => m.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State != DebuggerBrowsableState.Never)
                       .Select(m => new BasicPropertyItem(m, source, tree));

            switch (displayMode)
            {
                case PropertyDisplayMode.ListAll:
                    AddRange(generateItems(allMembers));
                    break;
                case PropertyDisplayMode.GroupByClass:
                    foreach (var (type, member) in typeMembers)
                    {
                        Add(new PropertyGroupHeader(type.ReadableName()));
                        Add(new VirtualPropertyItem<Type>("^type", type, tree));
                        AddRange(generateItems(member));
                    }
                    break;
            }
        }

        private class PropertyGroupHeader : Container
        {
            private readonly SpriteText groupText;

            public PropertyGroupHeader(string groupName)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Padding = new MarginPadding { Vertical = 5 };

                AddInternal(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding
                    {
                        Right = 6
                    },
                    Child = new FillFlowContainer<Drawable>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10f),
                        Children = new Drawable[]
                        {
                            groupText = new SpriteText
                            {
                                Text = $"{groupName}:",
                                Colour = Colour4.LightSkyBlue,
                                Font = FrameworkFont.Regular
                            },
                        },
                    }
                });
            }
        }

        private abstract class PropertyItem : Container
        {
            private SpriteText valueText;
            private ClickableContainer valueSlot;
            private Box changeMarker;
            protected Func<object> GetValue;
            /// <summary>Indicates whether it's a stored value (static) or a computable property (dynamic)</summary>
            protected bool IsDynamic;
            protected bool IsShown;
            private readonly ITreeContainer tree;

            protected abstract Type PropertyType { get; }
            protected abstract string PropertyName { get; }

            protected PropertyItem(ITreeContainer tree)
            {
                this.tree = tree;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
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
                        Child = new FillFlowContainer<Drawable>
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10f),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = PropertyName,
                                    Colour = FrameworkColour.Yellow,
                                    Font = FrameworkFont.Regular
                                },
                                new SpriteText
                                {
                                    Text = $@"[{PropertyType.ReadableName()}]:",
                                    Colour = FrameworkColour.YellowGreen,
                                    Font = FrameworkFont.Regular
                                },
                                valueSlot = new ClickableContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Action = activate,
                                    Child = valueText = new SpriteText
                                    {
                                        Colour = IsShown ? Color4.White : Color4.Gray,
                                        Font = FrameworkFont.Regular,
                                        Text = "<click to show value>"
                                    }
                                },
                            },
                        }
                    },
                    changeMarker = new Box
                    {
                        Size = new Vector2(4, 18),
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Colour = Color4.Red,
                        Alpha = 0f
                    }
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // Update the value once
                updateValue();
            }

            private void activate()
            {
                if (IsShown)
                {
                    tryInspect();
                }
                else
                {
                    IsShown = true;
                    valueText.Colour = Color4.White;
                }
            }

            private void tryInspect()
            {
                object value = GetValue();

                // Strings and enums are not represented
                if (value is null || value is string || value is Enum)
                {
                    return;
                }
                // Check for primitive type (not handling pointers)
                if (new[]
                {
                    typeof(sbyte), typeof(byte),
                    typeof(ushort), typeof(short),
                    typeof(int), typeof(uint),
                    typeof(long), typeof(ulong),
                    typeof(float), typeof(double),
                    typeof(char), typeof(bool),
                }.Contains(value.GetType()))
                {
                    return;
                }

                tree.RequestTarget(value);
            }

            protected override void Update()
            {
                base.Update();
                updateValue();
            }

            private object lastValue;

            private void updateValue()
            {
                if (!IsShown)
                    return;

                try
                {
                    object value;
                    value = GetValue();
                    lastValue = value;
                    if (value is null)
                    {
                        valueText.Text = "<null>";
                        valueText.Colour = Color4.Gray;
                    }
                    else
                    {
                        string str = value.ToString();
                        if (string.IsNullOrWhiteSpace(str))
                        {
                            valueText.Text = "<blank>";
                            valueText.Colour = Color4.Gray;
                        }
                        else
                        {
                            valueText.Text = str;
                            valueText.Colour = Color4.White;
                        }
                    }

                    // An alternative of object.Equals, which is banned.
                    if (!EqualityComparer<object>.Default.Equals(value, lastValue))
                    {
                        changeMarker.ClearTransforms();
                        changeMarker.Alpha = 0.8f;
                        changeMarker.FadeOut(200);
                    }
                }
                catch (Exception e)
                {
                    valueText.Text = $@"<{((e as TargetInvocationException)?.InnerException ?? e).GetType().ReadableName()} occured during evaluation>";
                    valueText.Colour = Color4.Red;
                    IsShown = false;
                }
            }
        }

        private class BasicPropertyItem : PropertyItem
        {
            protected override Type PropertyType { get; }
            protected override string PropertyName { get; }

            public BasicPropertyItem(MemberInfo info, object d, ITreeContainer tree)
                : base(tree)
            {
                switch (info)
                {
                    case PropertyInfo propertyInfo:
                        PropertyType = propertyInfo.PropertyType;
                        GetValue = () => propertyInfo.GetValue(d);
                        IsDynamic = true;
                        IsShown = false;
                        break;

                    case FieldInfo fieldInfo:
                        PropertyType = fieldInfo.FieldType;
                        GetValue = () => fieldInfo.GetValue(d);
                        IsDynamic = false;
                        IsShown = true;
                        break;

                    default:
                        throw new ArgumentException(@"Not a value member.", nameof(info));
                }

                PropertyName = info.Name;
            }
        }

        private class VirtualPropertyItem<T> : PropertyItem
        {
            protected override Type PropertyType => typeof(T);
            protected override string PropertyName { get; }

            private T value;

            public VirtualPropertyItem(string name, T value, ITreeContainer tree)
                : base(tree)
            {
                PropertyName = name;
                this.value = value;

                IsDynamic = false;
                IsShown = true;
                GetValue = () => this.value;
            }
        }

        private class MemberInfoComparer : IEqualityComparer<MemberInfo>
        {
            public bool Equals(MemberInfo x, MemberInfo y) => x?.Name == y?.Name;

            public int GetHashCode(MemberInfo obj) => obj.Name.GetHashCode();
        }
    }

    internal enum PropertyDisplayMode
    {
        ListAll, // Lists all properties, alphabetically
        GroupByClass, // Groups properties by declaring class
    }
}
