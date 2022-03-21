// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    [Cached]
    internal class TabbedTreeWindow : EmptyToolWindow
    {
        private readonly TabControl<TreeTab> tabControl;

        public readonly DrawableTreeContainer DrawableTreeContainer;

        public readonly Container<TreeContainer> TreeContainers;

        public TabbedTreeWindow(DrawableTreeContainer drawableTreeContainer)
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
            BodyContent.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize, minSize: WIDTH)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        tabControl = new TreeTabControl
                        {
                            RelativeSizeAxes = Axes.X,
                        },
                        TreeContainers = new Container<TreeContainer>
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                        },
                    }
                }.Invert()
            });

            tabControl.Current.BindValueChanged(showTab, true);

            DrawableTreeContainer = drawableTreeContainer;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            TreeContainers.Add(DrawableTreeContainer);
            AddTab(current = new SearchingTab(DrawableTreeContainer));
        }

        private void showTab(ValueChangedEvent<TreeTab> tabChanged)
        {
            Current = tabChanged.NewValue;
        }

        protected void AddTab(TreeTab tab)
        {
            tabControl.AddItem(tab);
            tabControl.Current.Value = tab;
        }

        private TreeTab current = null!;

        public TreeTab Current
        {
            get => current;
            set
            {
                if (current == value)
                    return;

                current.Tree.Hide();

                current = value;
                tabControl.Current.Value = value;

                current.Tree.Show();
                SetCurrentToolbar(current.Tree.Toolbar);
                current.SetTarget();
            }
        }

        public ObjectTreeContainer SpawnObjectVisualiser(object initTarget)
        {
            ObjectTreeContainer visualiser = new ObjectTreeContainer();
            TreeContainers.Add(visualiser);
            AddTab(new ObjectTreeTab(visualiser, initTarget));
            return visualiser;
        }

        public void SpawnDrawableVisualiser(Drawable initTarget)
        {
            AddTab(new DrawableTreeTab(DrawableTreeContainer, initTarget));
        }

        public void SelectTarget()
        {
            if (current == null)
                return;
            Current = tabControl.Items[0];
        }

        public new void Clear()
        {
            SelectTarget();
            tabControl.Items = new[] { current };
        }

        private class TreeTabControl : TabControl<TreeTab>
        {
            public TreeTabControl()
            {
                AutoSizeAxes = Axes.Y;
                TabContainer.AllowMultiline = true;
                TabContainer.RelativeSizeAxes = Axes.X;
                TabContainer.AutoSizeAxes = Axes.Y;
            }

            protected override Dropdown<TreeTab> CreateDropdown()
                => new BasicTabControl<TreeTab>.BasicTabControlDropdown();

            protected override TabItem<TreeTab> CreateTabItem(TreeTab value)
                => new TreeTabItem(value)
                {
                    RequestClose = closeTab
                };

            private void closeTab(TreeTabItem tab)
            {
                RemoveTabItem(tab);
            }
        }

        private class TreeTabItem : TabItem<TreeTab>
        {
            private Box background;
            private SpriteText text;

            public Action<TreeTabItem> RequestClose = null!;

            public TreeTabItem(TreeTab value)
                : base(value)
            {
                AutoSizeAxes = Axes.X;
                Height = 20f;
                AddRangeInternal(new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.YellowGreenDark,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            text = new SpriteText
                            {
                                Colour = Colour4.White,
                                Font = FrameworkFont.Regular.With(size: 18),
                            },
                            new TabCloseButton
                            {
                                Action = () => RequestClose(this),
                                Alpha = value is SearchingTab ? 0f : 1f,
                            },
                        }
                    }
                });
            }

            protected override void Update()
            {
                base.Update();

                text.Text = Value.ToString();
            }

            protected override void OnActivated()
                => background.Colour = FrameworkColour.YellowGreen;

            protected override void OnDeactivated()
                => background.Colour = FrameworkColour.YellowGreenDark;

            private class TabCloseButton : ClickableContainer
            {
                private SpriteIcon icon;

                public TabCloseButton()
                {
                    Margin = new MarginPadding { Left = 10f, Right = 2f };
                    Size = new osuTK.Vector2(20f);
                    InternalChild = icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Scale = new osuTK.Vector2(0.65f),
                        Icon = FontAwesome.Solid.Times,
                        Colour = Colour4.OrangeRed,
                    };
                }
            }
        }
    }
}
