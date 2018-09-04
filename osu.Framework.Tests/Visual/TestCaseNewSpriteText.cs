// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseNewSpriteText : TestCase
    {
        public TestCaseNewSpriteText()
        {
            var pairs = new List<Drawable[]>
            {
                new Drawable[] { new TestOldSpriteText { Text = "Old" }, new TestOldSpriteText { Text = "New" } },
                new Drawable[] { new TestOldSpriteText { Text = "Basic: Hello world!" }, new TestNewSpriteText { Text = "Basic: Hello world!" } },
                new Drawable[] { new TestOldSpriteText { Text = "Text size = 15", TextSize = 15 }, new TestNewSpriteText { Text = "Text size = 15", TextSize = 15 } },
                new Drawable[] { new TestOldSpriteText { Text = "Colour = green", Colour = Color4.Green }, new TestNewSpriteText { Text = "Colour = green", Colour = Color4.Green } },
                new Drawable[] { new TestOldSpriteText { Text = "Rotation = 45", Rotation = 45 }, new TestNewSpriteText { Text = "Rotation = 45", Rotation = 45 } },
                new Drawable[] { new TestOldSpriteText { Text = "Scale = 2", Scale = new Vector2(2) }, new TestNewSpriteText { Text = "Scale = 2", Scale = new Vector2(2) } },
                new Drawable[]
                {
                    new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        AutoSizeAxes = Axes.Both,
                        Child = new TestOldSpriteText { Text = "||MASKED||" }
                    },
                    new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        AutoSizeAxes = Axes.Both,
                        Child = new TestNewSpriteText { Text = "||MASKED||" }
                    }
                },
                new Drawable[] { new TestOldSpriteText { Text = "Explicit width", AutoSizeAxes = Axes.Y, Width = 50 }, new TestNewSpriteText { Text = "Explicit width", Width = 50 } },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestOldSpriteText { Text = "Relative size", AutoSizeAxes = Axes.Y, RelativeSizeAxes = Axes.X }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestNewSpriteText { Text = "Relative size", RelativeSizeAxes = Axes.X }
                    },
                }
            };

            var rowDimensions = new List<Dimension>();
            for (int i = 0; i < pairs.Count; i++)
                rowDimensions.Add(new Dimension(GridSizeMode.AutoSize));

            Child = new AutoSizeGridContainer
            {
                AutoSizeAxes = Axes.Y,
                Content = pairs.ToArray(),
                RowDimensions = rowDimensions.ToArray(),
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 300),
                    new Dimension(GridSizeMode.Absolute, 300),
                }
            };
        }

        private class TestOldSpriteText : SpriteText
        {
            public TestOldSpriteText()
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
            }
        }

        private class TestNewSpriteText : NewSpriteText
        {
            public TestNewSpriteText()
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
            }
        }

        private class AutoSizeGridContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                get => base.AutoSizeAxes;
                set => base.AutoSizeAxes = value;
            }
        }

        private class NewSpriteText : Drawable
        {
            private const float default_text_size = 20;

            public string Text;

            public float TextSize = default_text_size;

            public string Font;

            [Resolved]
            private FontStore store { get; set; }

            private float spaceWidth;

            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                spaceWidth = GetTextureForCharacter('.')?.DisplayWidth * 2 ?? default_text_size;
                sharedData.TextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
                sharedData.RoundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            }

            protected override void Update()
            {
                base.Update();

                // Temporary
                Invalidate();
                charactersCache.Invalidate();
            }

            #region Sizing

            private float? explicitWidth;

            /// <summary>
            /// Gets or sets the width of this <see cref="NewSpriteText"/>. The <see cref="NewSpriteText"/> will maintain this width when set.
            /// </summary>
            public override float Width
            {
                get => base.Width;
                set
                {
                    if (explicitWidth == value)
                        return;

                    base.Width = value;
                    explicitWidth = value;

                    charactersCache.Invalidate();
                }
            }

            private float? explicitHeight;

            /// <summary>
            /// Gets or sets the height of this <see cref="NewSpriteText"/>. The <see cref="NewSpriteText"/> will maintain this height when set.
            /// </summary>
            public override float Height
            {
                get => base.Height;
                set
                {
                    if (explicitHeight == value)
                        return;

                    base.Height = value;
                    explicitHeight = value;

                    charactersCache.Invalidate();
                }
            }

            /// <summary>
            /// Gets or sets the size of this <see cref="NewSpriteText"/>. The <see cref="NewSpriteText"/> will maintain this size when set.
            /// </summary>
            public override Vector2 Size
            {
                get => new Vector2(Width, Height);
                set
                {
                    Width = value.X;
                    Height = value.Y;
                }
            }

            #endregion

            #region Characters

            private Cached charactersCache = new Cached();

            private readonly List<CharacterPart> charactersBacking = new List<CharacterPart>();

            private List<CharacterPart> characters
            {
                get
                {
                    computeCharacters();
                    return charactersBacking;
                }
            }

            private void computeCharacters()
            {
                if (charactersCache.IsValid)
                    return;

                charactersBacking.Clear();

                if (string.IsNullOrEmpty(Text))
                    return;

                float maxWidth = float.PositiveInfinity;
                if ((RelativeSizeAxes & Axes.X) > 0 || explicitWidth != null)
                    maxWidth = DrawWidth;

                Vector2 currentPos = Vector2.Zero;

                foreach (var character in Text)
                {
                    if (char.IsWhiteSpace(character))
                    {
                        float width = spaceWidth * TextSize;

                        if (character == 0x3000)
                        {
                            // Double-width space
                            width *= 2;
                        }

                        if (currentPos.X + width >= maxWidth)
                        {
                            currentPos.X = 0;
                            currentPos.Y += TextSize;
                        }

                        currentPos.X += width;

                        continue;
                    }

                    var tex = GetTextureForCharacter(character);

                    var textureSize = new Vector2(tex.DisplayWidth, tex.DisplayHeight) * TextSize;

                    if (currentPos.X + textureSize.X >= maxWidth)
                    {
                        currentPos.X = 0;
                        currentPos.Y += TextSize;
                    }

                    var drawQuad = ToScreenSpace(new RectangleF(currentPos, textureSize));

                    charactersBacking.Add(new CharacterPart
                    {
                        Texture = tex,
                        DrawQuad = drawQuad
                    });

                    currentPos.X += textureSize.X;
                }

                currentPos.Y += TextSize;

                if (explicitWidth == null && (RelativeSizeAxes & Axes.X) == 0)
                    base.Width = currentPos.X;
                if (explicitHeight == null && (RelativeSizeAxes & Axes.Y) == 0)
                    base.Height = currentPos.Y;

                charactersCache.Validate();
            }

            #endregion

            #region DrawNode

            private readonly NewSpriteTextDrawNodeSharedData sharedData = new NewSpriteTextDrawNodeSharedData();

            protected override DrawNode CreateDrawNode() => new NewSpriteTextDrawNode();

            protected override void ApplyDrawNode(DrawNode node)
            {
                base.ApplyDrawNode(node);

                var n = (NewSpriteTextDrawNode)node;

                n.Shared = sharedData;
                n.Parts.Clear();
                n.Parts.AddRange(characters);
            }

            #endregion

            /// <summary>
            /// Gets the texture for the given character.
            /// </summary>
            /// <param name="c">The character to get the texture for.</param>
            /// <returns>The texture for the given character.</returns>
            protected Texture GetTextureForCharacter(char c)
            {
                if (store == null)
                    return null;

                return store.Get(getTextureName(c)) ?? store.Get(getTextureName(c, false));
            }

            private string getTextureName(char c, bool useFont = true) => !useFont || string.IsNullOrEmpty(Font) ? c.ToString() : $@"{Font}/{c}";
        }

        private class NewSpriteTextDrawNodeSharedData
        {
            public Shader TextureShader;
            public Shader RoundedTextureShader;
        }

        private class NewSpriteTextDrawNode : DrawNode
        {
            public NewSpriteTextDrawNodeSharedData Shared;

            public readonly List<CharacterPart> Parts = new List<CharacterPart>();

            private bool needsRoundedShader => GLWrapper.IsMaskingActive;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                Shader shader = needsRoundedShader ? Shared.RoundedTextureShader : Shared.TextureShader;

                shader.Bind();

                for (int i = 0; i < Parts.Count; i++)
                {
                    Parts[i].Texture.DrawQuad(
                        Parts[i].DrawQuad,
                        DrawInfo.Colour,
                        vertexAction: vertexAction);
                }

                shader.Unbind();
            }
        }

        private struct CharacterPart
        {
            public Quad DrawQuad;
            public Texture Texture;
        }
    }
}
