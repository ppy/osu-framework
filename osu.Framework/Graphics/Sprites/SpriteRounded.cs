// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteRounded : Sprite
    {
        public float Radius = 0.0f;

        protected override DrawNode CreateDrawNode() => new SpriteRoundedDrawNode();

        private static Shader shader;

        public override void Load(BaseGame game)
        {
            base.Load(game);

            //todo: make this better.
            if (shader == null)
                shader = game.Shaders.Load(VertexShader.Texture2D, FragmentShader.TextureRounded);
        }

        protected override void ApplyDrawNode(DrawNode node)
        {
            SpriteRoundedDrawNode n = node as SpriteRoundedDrawNode;

            base.ApplyDrawNode(node);

            n.Shader = shader;
            n.Radius = Radius;
        }

        public override Drawable Clone()
        {
            SpriteRounded clone = (SpriteRounded)base.Clone();
            clone.Radius = Radius;

            return clone;
        }

        public override string ToString()
        {
            return base.ToString() + $" radius: {Radius}";
        }
    }
}
