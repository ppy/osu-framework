using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseTabs : TestCase
    {

        public override string Name => @"Tabs";

        public override string Description => @"Tabs with some content";

        private TabContainer tabC;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                tabC = new TabContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                }
            };
            Container tabBody;
            tabC.Add(tabBody = new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new SpriteText()
                    {
                        Anchor = Anchor.TopLeft,
                        Text = "Hey!",
                    }
                }
            });
            tabC.Header.Add(new TabHead(tabBody, "Tab 1"));

            tabC.Add(tabBody = new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new SpriteText()
                    {
                        Anchor = Anchor.TopLeft,
                        Text = "Hoo!",
                    }
                }
            });
            tabC.Header.Add(new TabHead(tabBody, "Tab 2"));

            //TabContainer innerTabC;
            //tabC.Add(innerTabC = new TabContainer()
            //{
            //    RelativeSizeAxes = Axes.Both,
            //    TabIndexTextColor = Color4.Red,
                
            //}, "TabContainer");

            //innerTabC.AddTab(new Container()
            //{
            //    RelativeSizeAxes = Axes.Both,
            //    Children = new Drawable[]
            //    {
            //        new SpriteText()
            //        {
            //            Anchor = Anchor.TopLeft,
            //            Text = "Hey!",
            //        }
            //    }
            //}, "Tab 1");

            //innerTabC.AddTab(new Container()
            //{
            //    RelativeSizeAxes = Axes.Both,
            //    Children = new Drawable[]
            //    {
            //        new SpriteText()
            //        {
            //            Anchor = Anchor.TopLeft,
            //            Text = "Hoo!",
            //        }
            //    }
            //}, "Tab 2");
        }
    }
}
