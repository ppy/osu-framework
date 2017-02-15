using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class TabContainer : Container
    {

        private Container currentTab;
        private FlowContainer tabIndexesFlow;
        private Container tabIndexes;
        private Container tabBodies;
        public float TabIndexHeight { get { return tabIndexes.Height; } set { tabIndexes.Height = value; tabBodies.Margin = new MarginPadding() { Top = value }; } }
        public Color4 TabIndexTextColor { get; set; } = Color4.Yellow;
        public Color4 TabIndexBackgroundColor { get { return tabIndexesBackground.Colour; } set { tabIndexesBackground.Colour = value; } }
        private Box tabIndexesBackground;


        public void AddTab(Container tab, string name)
        {
            ChangeTab(tab);
            tabIndexesFlow.Add(new TabIndex(this, tab, name));
            tabBodies.Add(tab);
        }

        internal void ChangeTab(Container tab)
        {
            if (currentTab != null)
            {
                currentTab.Hide();
            }
            currentTab = tab;
            currentTab.Show();
        }

        public TabContainer()
        {

            Add(tabIndexes = new Container()
            {
                RelativeSizeAxes = Axes.X,
                Height = 20,
                Children = new Drawable[]
                {
                    tabIndexesBackground = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGray,
                    },
                    tabIndexesFlow = new FlowContainer()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FlowDirections.Horizontal,
                        Spacing = new Vector2(5,0),
                    }
                }
            });
            Add(tabBodies = new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Margin = new MarginPadding()
                {
                    Top = TabIndexHeight,
                },
            });

        }

    }

    public class TabIndex : Container
    {

        private Container container;
        private TabContainer tabContainer;

        public TabIndex(TabContainer tab, Container container, string tabName)
        {
            tabContainer = tab;
            this.container = container;
            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {

                new SpriteText()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = tabName,
                    Colour = tab.TabIndexTextColor,
                }
            };

        }


        protected override bool OnClick(InputState state)
        {
            tabContainer.ChangeTab(container);
            return base.OnClick(state);
        }

    }
}
