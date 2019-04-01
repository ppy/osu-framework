// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    public partial class SpriteText
    {
        internal class SpriteTextDrawNode : DrawNode
        {
            private bool shadow;
            private ColourInfo shadowColour;
            private Vector2 shadowOffset;

            private IShader textureShader;
            private IShader roundedTextureShader;

            private readonly List<ScreenSpaceCharacterPart> parts = new List<ScreenSpaceCharacterPart>();

            public override void ApplyFromDrawable(Drawable source)
            {
                base.ApplyFromDrawable(source);

                var text = (SpriteText)source;

                parts.Clear();
                parts.AddRange(text.screenSpaceCharacters);
                shadow = text.Shadow;
                textureShader = text.textureShader;
                roundedTextureShader = text.roundedTextureShader;

                if (shadow)
                {
                    shadowColour = text.ShadowColour;
                    shadowOffset = text.shadowOffset;
                }
            }

            private bool needsRoundedShader => GLWrapper.IsMaskingActive;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                IShader shader = needsRoundedShader ? roundedTextureShader : textureShader;

                shader.Bind();

                var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;
                float shadowAlpha = (float)Math.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var finalShadowColour = DrawColourInfo.Colour;
                finalShadowColour.ApplyChild(shadowColour.MultiplyAlpha(shadowAlpha));

                for (int i = 0; i < parts.Count; i++)
                {
                    if (shadow)
                    {
                        var shadowQuad = parts[i].DrawQuad;
                        shadowQuad.TopLeft += shadowOffset;
                        shadowQuad.TopRight += shadowOffset;
                        shadowQuad.BottomLeft += shadowOffset;
                        shadowQuad.BottomRight += shadowOffset;

                        parts[i].Texture.DrawQuad(shadowQuad, finalShadowColour, vertexAction: vertexAction);
                    }

                    parts[i].Texture.DrawQuad(parts[i].DrawQuad, DrawColourInfo.Colour, vertexAction: vertexAction);
                }

                shader.Unbind();
            }
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
