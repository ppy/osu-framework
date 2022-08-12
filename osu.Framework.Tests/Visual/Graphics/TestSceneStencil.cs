// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Tests.Visual.Graphics
{
    public class TestSceneStencil : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Container circlesContainer;

            Child = new StencilDrawable
            {
                RelativeSizeAxes = Axes.Both,
                Background = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textures.Get("sample-texture")
                },
                Stencil = circlesContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            const float circle_radius = 0.05f;
            const float spacing = 0.01f;

            for (float xPos = 0; xPos < 1; xPos += circle_radius + spacing)
            {
                for (float yPos = 0; yPos < 1; yPos += circle_radius + spacing)
                {
                    circlesContainer.Add(new CircularProgress
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(circle_radius),
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(xPos, yPos),
                        Current = { Value = 1 }
                    });
                }
            }
        }

        private class StencilDrawable : CompositeDrawable
        {
            public Drawable Background
            {
                set => backgroundContainer.Child = value;
            }

            public Drawable Stencil
            {
                set => stencilContainer.Child = value;
            }

            private readonly Container backgroundContainer;
            private readonly Container stencilContainer;

            public StencilDrawable()
            {
                InternalChildren = new Drawable[]
                {
                    backgroundContainer = new Container { RelativeSizeAxes = Axes.Both },
                    stencilContainer = new Container { RelativeSizeAxes = Axes.Both }
                };
            }

            protected override DrawNode CreateDrawNode() => new StencilDrawNode(this);
        }

        private class StencilDrawNode : DrawNode, ICompositeDrawNode
        {
            public StencilDrawNode(IDrawable source)
                : base(source)
            {
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                // No depth testing.
                renderer.PushDepthInfo(new DepthInfo(false));

                drawStencil(renderer, Children[1]);
                drawBackground(renderer, Children[0]);

                renderer.PopDepthInfo();
            }

            private void drawStencil(IRenderer renderer, DrawNode drawNode)
            {
                // Populate the stencil buffer with 255 in places defined by the draw node.
                renderer.PushStencilInfo(new StencilInfo(
                    stencilTest: true,
                    testFunction: DepthStencilFunction.Always,
                    255,
                    passed: StencilOperation.Replace));

                // Don't write colour.
                renderer.SetBlend(new BlendingParameters
                {
                    Source = BlendingType.Zero,
                    Destination = BlendingType.One,
                    SourceAlpha = BlendingType.Zero,
                    DestinationAlpha = BlendingType.One,
                });

                drawNode.Draw(renderer);

                renderer.PopStencilInfo();
                renderer.SetBlend(DrawColourInfo.Blending);
            }

            private void drawBackground(IRenderer renderer, DrawNode drawNode)
            {
                // Only pass the stencil test where the stencil buffer value is 255.
                renderer.PushStencilInfo(new StencilInfo(
                    stencilTest: true,
                    testFunction: DepthStencilFunction.Equal,
                    mask: 255,
                    testValue: 255,
                    stencilFailed: StencilOperation.Keep,
                    passed: StencilOperation.Keep
                ));

                drawNode.Draw(renderer);

                renderer.PopStencilInfo();
            }

            public List<DrawNode> Children { get; set; } = new List<DrawNode>();

            public bool AddChildDrawNodes => true;
        }
    }
}
