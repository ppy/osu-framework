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
using System.Drawing;

namespace osu.Framework.Graphics.UserInterface
{
    public class TabContainer : Container
    {

        protected override Container<Drawable> Content => tabBodies;
        private Drawable currentTab;
        public TabHeadContainer Header;
        private Container tabBodies;


        public float TabIndexHeight
        {
            get
            {
                return Header.Height;
            }
            set
            {
                Header.Height = value;
                tabBodies.Margin = new MarginPadding()
                {
                    Top = value
                };
            }

        }
        public Color4 TabIndexTextColor { get; set; } = Color4.Yellow;
        public Font TabIndexTextFont { get; set; }

        public override void Add(Drawable drawable)
        {
            base.Add(drawable);
            ChangeTab(drawable);
        }

        internal void ChangeTab(Drawable tab)
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
            InternalChildren = new Drawable[] {
                Header = new TabHeadContainer(this)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 20,
                },
                tabBodies = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding()
                    {
                        Top = TabIndexHeight,
                    },
                }
            };
        }

        //protected override bool OnClick(InputState state)
        //{
        //    foreach(Drawable drawable in Header.Children){
        //        System.Diagnostics.Debug.WriteLine(drawable.Contains(ToSpaceOfOtherDrawable(state.Mouse.Position, drawable)));
        //        if (drawable.Contains(ToSpaceOfOtherDrawable(state.Mouse.Position,drawable)))
        //        {
        //            ChangeTab((drawable as TabHead).Container);
        //        }
        //    }
        //    return base.OnClick(state);
        //}
    }

    public class TabHeadContainer : Container
    {
        public FlowContainer TabHeads;
        protected override Container<Drawable> Content => TabHeads;
        private Box tabHeadBackground;
        public Color4 TabHeadBackgroundColor
        {
            get
            {
                return tabHeadBackground.Colour;
            }
            set
            {
                tabHeadBackground.Colour = value;
            }
        }
        public TabHeadContainer(TabContainer container)
        {
            InternalChildren = new Drawable[]
            {
                tabHeadBackground = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkGray,
                },
                TabHeads = new FlowContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FlowDirections.Horizontal,
                    Spacing = new Vector2(5,0),
                }
            };
        }
    }

    public class TabHead : Container
    {
        public Drawable Container { get; set; }
        private SpriteText text;
        public SRGBColour TextColor
        {
            get
            {
                return text.Colour;
            }

            set
            {
                text.Colour = value;
            }
        }

        public TabHead(Drawable container, string tabName)
        {
            Container = container;
            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                text = new SpriteText()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = tabName,
                }
            };
        }

        protected override bool OnClick(InputState state)
        {
            (Container.Parent.Parent as TabContainer).ChangeTab(Container);
            return base.OnClick(state);
        }
    }
}
