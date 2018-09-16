// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    public partial class SpriteText
    {
        internal class SpriteTextDrawNode : DrawNode
        {
            internal SpriteTextDrawNodeSharedData Shared;

            public bool Shadow;
            public ColourInfo ShadowColour;
            public Vector2 ShadowOffset;

            internal readonly List<ScreenSpaceCharacterPart> Parts = new List<ScreenSpaceCharacterPart>();

            private bool needsRoundedShader => GLWrapper.IsMaskingActive;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                Shader shader = needsRoundedShader ? Shared.RoundedTextureShader : Shared.TextureShader;

                shader.Bind();

                var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;
                float shadowAlpha = (float)Math.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var shadowColour = DrawColourInfo.Colour;
                shadowColour.ApplyChild(ShadowColour.MultiplyAlpha(shadowAlpha));

                for (int i = 0; i < Parts.Count; i++)
                {
                    if (Shadow)
                    {
                        var shadowQuad = Parts[i].DrawQuad;
                        shadowQuad.TopLeft += ShadowOffset;
                        shadowQuad.TopRight += ShadowOffset;
                        shadowQuad.BottomLeft += ShadowOffset;
                        shadowQuad.BottomRight += ShadowOffset;

                        Parts[i].Texture.DrawQuad(shadowQuad, shadowColour, vertexAction: vertexAction);
                    }

                    Parts[i].Texture.DrawQuad(Parts[i].DrawQuad, DrawColourInfo.Colour, vertexAction: vertexAction);
                }

                shader.Unbind();
            }
        }

        internal class SpriteTextDrawNodeSharedData
        {
            public Shader TextureShader;
            public Shader RoundedTextureShader;
        }

        /// <summary>
        /// A character of a <see cref="SpriteText"/> provided with local space coordinates.
        /// </summary>
        internal struct CharacterPart
        {
            /// <summary>
            /// The local-space rectangle for the character to be drawn in.
            /// </summary>
            public RectangleF DrawRectangle;

            /// <summary>
            /// The texture to draw the character with.
            /// </summary>
            public Texture Texture;
        }

        /// <summary>
        /// A character of a <see cref="SpriteText"/> provided with screen space draw coordinates.
        /// </summary>
        internal struct ScreenSpaceCharacterPart
        {
            /// <summary>
            /// The screen-space quad for the character to be drawn in.
            /// </summary>
            public Quad DrawQuad;

            /// <summary>
            /// The texture to draw the character with.
            /// </summary>
            public Texture Texture;
        }
    }
}
