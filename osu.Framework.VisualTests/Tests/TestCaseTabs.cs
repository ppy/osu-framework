using OpenTK;
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
                    Size = Vector2.One,
                }
            };

            tabC.AddTab(new Container()
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
            }, "Tab 1");

            tabC.AddTab(new Container()
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
            }, "Tab 2");
        }
    }
}
