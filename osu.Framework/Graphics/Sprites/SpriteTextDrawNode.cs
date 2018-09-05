// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shaders;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    internal class SpriteTextDrawNode : DrawNode
    {
        internal SpriteTextDrawNodeSharedData Shared;

        public bool Shadow;
        public Color4 ShadowColour;
        public Vector2 ShadowOffset;

        internal readonly List<ScreenSpaceCharacterPart> Parts = new List<ScreenSpaceCharacterPart>();

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            Shader shader = needsRoundedShader ? Shared.RoundedTextureShader : Shared.TextureShader;

            shader.Bind();

            for (int i = 0; i < Parts.Count; i++)
            {
                if (Shadow)
                {
                    var shadowColour = DrawInfo.Colour;
                    shadowColour.ApplyChild(ShadowColour);

                    var shadowQuad = Parts[i].DrawQuad;
                    shadowQuad.TopLeft += ShadowOffset;
                    shadowQuad.TopRight += ShadowOffset;
                    shadowQuad.BottomLeft += ShadowOffset;
                    shadowQuad.BottomRight += ShadowOffset;

                    Parts[i].Texture.DrawQuad(shadowQuad, shadowColour, vertexAction: vertexAction);
                }

                Parts[i].Texture.DrawQuad(Parts[i].DrawQuad, DrawInfo.Colour, vertexAction: vertexAction);
            }

            shader.Unbind();
        }
    }

    internal class SpriteTextDrawNodeSharedData
    {
        public Shader TextureShader;
        public Shader RoundedTextureShader;
    }
}
