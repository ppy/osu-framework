// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTexturedTriangle : FrameworkTestScene
    {
        public TestSceneTexturedTriangle()
        {
            Add(new TexturedTriangle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300, 150)
            });
        }

        private class TexturedTriangle : Triangle
        {
            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Texture = textures.Get(@"sample-texture");
            }
        }
    }
}
