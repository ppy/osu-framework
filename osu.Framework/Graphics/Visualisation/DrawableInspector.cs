// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Visualisation
{
    internal class DrawableInspector : VisibilityContainer
    {
        [Cached]
        public Bindable<Drawable> InspectedDrawable { get; private set; } = new Bindable<Drawable>();

        private const float width = 600;

        private readonly GridContainer content;
        private readonly TabControl<Tab> inspectorTabControl;
        private readonly Container tabContentContainer;
        private readonly PropertyDisplay propertyDisplay;
        private readonly TransformDisplay transformDisplay;

        public DrawableInspector()
        {
            Width = width;
            RelativeSizeAxes = Axes.Y;

            Child = content = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                },
                ColumnDimensions = new[] { new Dimension() },
                Content = new[]
                {
                    new Drawable[]
                    {
                        inspectorTabControl = new BasicTabControl<Tab>
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 20,
                            Margin = new MarginPadding
                            {
                                Horizontal = 10,
                                Vertical = 5
                            },
                            Items = Enum.GetValues(typeof(Tab)).Cast<Tab>().ToList()
                        },
                        tabContentContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                propertyDisplay = new PropertyDisplay(),
                                transformDisplay = new TransformDisplay()
                            }
                        }
                    }
                }.Invert()
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inspectorTabControl.Current.BindValueChanged(showTab, true);
        }

        private void showTab(ValueChangedEvent<Tab> tabChanged)
        {
            tabContentContainer.Children.ForEach(tab => tab.Hide());

            switch (tabChanged.NewValue)
            {
                case Tab.Properties:
                    propertyDisplay.Show();
                    break;

                case Tab.Transforms:
                    transformDisplay.Show();
                    break;
            }
        }

        protected override void PopIn()
        {
            this.ResizeWidthTo(width, 500, Easing.OutQuint);

            inspectorTabControl.Current.Value = Tab.Properties;
            content.Show();
        }

        protected override void PopOut()
        {
            content.Hide();

            this.ResizeWidthTo(0, 500, Easing.OutQuint);
        }

        private enum Tab
        {
            Properties,
            Transforms
        }
    }
}
