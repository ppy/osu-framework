// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneBufferedContainerPixelSnapping : TestScene
    {
        private Texture texture = null!;
        private Container content = null!;
        private BufferedContainer container = null!;
        private bool pixelSnapping = true;
        private bool useBufferContainer = true;
        private Vector2 frameBufferScale = new Vector2(1);

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer, GameHost host, Game game)
        {
            var textures = new TextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, "Textures")), filteringMode: TextureFilteringMode.Nearest);

            texture = textures.Get("sample-texture");

            Child = content = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100),
            };

            content.Loop(t => t.ResizeTo(new Vector2(300), 1000, Easing.InOutSine)
                               .Then()
                               .ResizeTo(new Vector2(100), 1000, Easing.InOutSine));

            updateContent();
        }

        private void updateContent()
        {
            var drawable = new CircularContainer
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                Children = new Drawable[]
                {
                    new Sprite
                    {
                        Texture = texture,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Scale = new Vector2(1.5f)
                    },
                    new Box
                    {
                        Size = new Vector2(80),
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Text",
                        Colour = Color4.Black,
                        Font = new FontUsage(size: 35),
                    }
                }
            };

            container = new BufferedContainer(pixelSnapping: pixelSnapping)
            {
                RelativeSizeAxes = Axes.Both,
                FrameBufferScale = frameBufferScale,
            };

            if (useBufferContainer)
            {
                container.Child = drawable;
                content.Child = container;
            }
            else
            {
                content.Child = drawable;
            }
        }

        [SetUpSteps]
        public void Setup()
        {
            AddToggleStep("buffered", value =>
            {
                useBufferContainer = value;
                updateContent();
            });
            AddToggleStep("pixel snapping", value =>
            {
                pixelSnapping = value;
                updateContent();
            });
            AddSliderStep("framebuffer scale", 0.25f, 2f, 1f, value =>
            {
                frameBufferScale = new Vector2(value);
                updateContent();
            });
        }
    }
}
