// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneSafeArea : FrameworkTestScene
    {
        private readonly IBindable<MarginPadding> safeAreaPadding = new BindableMarginPadding();
        private readonly Container container;
        private readonly Box box;
        private readonly SpriteText textbox;

        private IWindow window;

        public TestSceneSafeArea()
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

            if (window == null) return;

            safeAreaPadding.ValueChanged += e => updatePadding(e.NewValue);
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

            if (window == null) return;

            var size = new Vector2(window.Width, window.Height);
            var scale = Vector2.Divide(Content.DrawSize, size);
            container.Size = box.Size = size;
            container.Scale = box.Scale = new Vector2(Math.Min(scale.X, scale.Y) * 0.9f);
        }
    }
}
