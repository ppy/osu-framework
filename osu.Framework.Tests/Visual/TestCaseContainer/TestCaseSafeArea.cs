// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using GameWindow = osu.Framework.Platform.GameWindow;

namespace osu.Framework.Tests.Visual.TestCaseContainer
{
    public class TestCaseSafeArea : TestCase
    {
        private readonly BindableMarginPadding safeAreaPadding = new BindableMarginPadding();
        private readonly Container container;
        private readonly Box box;
        private readonly SpriteText textbox;

        private GameWindow window;

        public TestCaseSafeArea()
        {
            Child = new FillFlowContainer
            {
                Padding = new MarginPadding(10),
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5f),
                Children = new Drawable[]
                {
                    textbox = new SpriteText { Text = "SafeAreaPadding:" },
                    new Container
                    {
                        Children = new Drawable[]
                        {
                            box = new Box
                            {
                                Colour = Color4.Red,
                                Size = new Vector2(100, 100)
                            },
                            container = new Container
                            {
                                Size = new Vector2(100, 100),
                                Child = new Box
                                {
                                    Colour = Color4.Blue,
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window;
            safeAreaPadding.ValueChanged += updatePadding;
            safeAreaPadding.BindTo(window.SafeAreaPadding);
            updatePadding(window.SafeAreaPadding.Value);
        }

        private void updatePadding(MarginPadding padding)
        {
            container.Padding = padding;
            textbox.Text = $"SafeAreaPadding: {padding}";
        }

        protected override void Update()
        {
            base.Update();
            var size = new Vector2(window.Width, window.Height);
            var scale = Vector2.Divide(Content.DrawSize, size);
            container.Size = box.Size = size;
            container.Scale = box.Scale = new Vector2(Math.Min(scale.X, scale.Y) * 0.9f);
        }
    }
}
