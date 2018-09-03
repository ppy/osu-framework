// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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
                new Drawable[] { new TestOldSpriteText { Text = "Old" }, new TestOldSpriteText { Text = "New" }},
                new Drawable[] { new TestOldSpriteText { Text = "Basic: Hello world!" }, new TestNewSpriteText { Text = "Basic: Hello world!" } },
                new Drawable[] { new TestOldSpriteText { TextSize = 15, Text = "Text size = 15" }, new TestNewSpriteText { TextSize = 15, Text = "Text size = 15" } },
                new Drawable[] { new TestOldSpriteText { Colour = Color4.Green, Text = "Colour = green" }, new TestNewSpriteText { Colour = Color4.Green, Text = "Colour = green" } },
                new Drawable[] { new TestOldSpriteText { Rotation = 45, Text = "Rotation = 45" }, new TestNewSpriteText { Rotation = 45, Text = "Rotation = 45" } },
                new Drawable[] { new TestOldSpriteText { Scale = new Vector2(2), Text = "Scale = 2" }, new TestNewSpriteText { Scale = new Vector2(2), Text = "Scale = 2" } },
                new Drawable[] { new TestOldSpriteText { Scale = new Vector2(2), Text = "Scale = 2" }, new TestNewSpriteText { Scale = new Vector2(2), Text = "Scale = 2" }, }
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
                    new Dimension(GridSizeMode.AutoSize),
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

        private class NewSpriteText : CompositeDrawable
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
            }

            protected override void Update()
            {
                base.Update();

                // Temporary
                Invalidate();
                charactersCache.Invalidate();
            }

#region Characters
            private readonly List<CharacterPart> charactersBacking = new List<CharacterPart>();

            private Cached charactersCache = new Cached();

            private List<CharacterPart> characters
            {
                get
                {
                    if (!charactersCache.IsValid)
                    {
                        computeCharacters();
                        charactersCache.Validate();
                    }

                    return charactersBacking;
                }
            }

            private void computeCharacters()
            {
                charactersBacking.Clear();

                if (string.IsNullOrEmpty(Text))
                    return;

                var pos = ChildOffset;

                foreach (var character in Text)
                {
                    if (char.IsWhiteSpace(character))
                    {
                        float width = spaceWidth;

                        if (character == 0x3000)
                        {
                            // Double-width space
                            width *= 2;
                        }

                        pos.X += width * TextSize;
                        continue;
                    }

                    var tex = GetTextureForCharacter(character);
                    var drawQuad = ToScreenSpace(new RectangleF(pos, new Vector2(tex.DisplayWidth, tex.DisplayHeight) * TextSize));

                    charactersBacking.Add(new CharacterPart
                    {
                        Texture = tex,
                        DrawQuad = drawQuad
                    });

                    pos.X += tex.DisplayWidth * TextSize;
                }

                Size = new Vector2(pos.X, TextSize);
            }
#endregion

#region DrawNode
            protected override bool CanBeFlattened => false; // We need to have a draw node without explicitly having children

            protected override DrawNode CreateDrawNode() => new NewSpriteTextDrawNode();

            protected override void ApplyDrawNode(DrawNode node)
            {
                base.ApplyDrawNode(node);

                var n = (NewSpriteTextDrawNode)node;

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

        private class NewSpriteTextDrawNode : CompositeDrawNode
        {
            public readonly List<CharacterPart> Parts = new List<CharacterPart>();

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                for (int i = 0; i < Parts.Count; i++)
                {
                    Parts[i].Texture.DrawQuad(
                        Parts[i].DrawQuad,
                        DrawInfo.Colour,
                        vertexAction: vertexAction);
                }
            }
        }

        private struct CharacterPart
        {
            public Quad DrawQuad;
            public Texture Texture;
        }
    }
}
