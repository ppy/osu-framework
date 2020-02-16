// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteEdgeAlignment : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            const float trim = 1;

            var left = textures.Get("button-left");
            var middle = textures.Get("button-middle");
            var right = textures.Get("button-right");

            Child = new FillFlowContainer
            {
                Scale = new Vector2(5),
                Direction = FillDirection.Horizontal,
                Children = new[]
                {
                    new Container
                    {
                        Masking = true,
                        Child = new Sprite
                        {
                            Texture = left,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                        },
                        AutoSizeAxes = Axes.Y,
                        Width = left.DisplayWidth - trim,
                    },
                    new Container
                    {
                        Masking = true,
                        Child = new Sprite
                        {
                            Texture = middle,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        AutoSizeAxes = Axes.Y,
                        Width = middle.DisplayWidth - trim,
                    },
                    new Container
                    {
                        Masking = true,
                        Child = new Sprite
                        {
                            Texture = right,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft
                        },
                        AutoSizeAxes = Axes.Y,
                        Width = right.DisplayWidth - trim,
                    }
                }
            };
        }
    }
}
