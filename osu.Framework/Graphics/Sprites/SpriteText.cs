// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A container for simple text rendering purposes. If more complex text rendering is required, use <see cref="TextFlowContainer"/> instead.
    /// </summary>
    public partial class SpriteText : Drawable, IHasCurrentValue<string>, IHasLineBaseHeight, IHasText, IHasFilterTerms, IFillFlowContainer
    {
        private const float default_text_size = 20;
        private static readonly Vector2 shadow_offset = new Vector2(0, 0.06f);
        private static readonly char[] default_fixed_width_exceptions = { '.', ':', ',' };

        [Resolved]
        private FontStore store { get; set; }

        private float spaceWidth;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            spaceWidth = GetTextureForCharacter('.')?.DisplayWidth * 2 ?? 1;
            sharedData.TextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            sharedData.RoundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // Pre-cache the characters in the texture store
            if (!string.IsNullOrEmpty(Text))
            {
                foreach (var character in Text)
                    GetTextureForCharacter(character);
            }
        }

        private string text;

        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;
                bool wasPresent = IsPresent;
                text = value;

                if (string.IsNullOrEmpty(text))
                {
                    // We'll become not present and won't update the characters to set the size to 0, so do it manually
                    if (requiresAutoSizedWidth)
                        base.Width = Padding.TotalHorizontal;
                    if (requiresAutoSizedHeight)
                        base.Height = Padding.TotalVertical;
                }

                PropagateInvalidation(InvalidateCharacters() | (IsPresent != wasPresent ? InvalidateIsPresent() : 0));
            }
        }

        private float textSize = default_text_size;

        /// <summary>
        /// The size of the text in local space. This means that if TextSize is set to 16, a single line will have a height of 16.
        /// </summary>
        public float TextSize
        {
            get => textSize;
            set
            {
                if (textSize == value)
                    return;
                textSize = value;

                PropagateInvalidation(InvalidateCharacters() | InvalidateShadowOffset());
            }
        }

        private string font;

        /// <summary>
        /// The name of the font to use when looking up textures for the individual characters.
        /// </summary>
        public string Font
        {
            get => font;
            set
            {
                if (font == value)
                    return;
                font = value;

                PropagateInvalidation(InvalidateCharacters() | InvalidateConstantWidth() | InvalidateShadowOffset());
            }
        }

        private bool allowMultiline = true;

        /// <summary>
        /// True if the text should be wrapped if it gets too wide. Note that \n does NOT cause a line break. If you need explicit line breaks, use <see cref="TextFlowContainer"/> instead.
        /// </summary>
        public bool AllowMultiline
        {
            get => allowMultiline;
            set
            {
                if (allowMultiline == value)
                    return;
                allowMultiline = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        private bool shadow;

        /// <summary>
        /// True if a shadow should be displayed around the text.
        /// </summary>
        public bool Shadow
        {
            get => shadow;
            set
            {
                if (shadow == value)
                    return;
                shadow = value;
                ForceRedraw();
            }
        }

        private Color4 shadowColour = new Color4(0, 0, 0, 0.2f);

        /// <summary>
        /// The colour of the shadow displayed around the text. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Color4 ShadowColour
        {
            get => shadowColour;
            set
            {
                if (shadowColour == value)
                    return;
                shadowColour = value;
                ForceRedraw();
            }
        }

        private bool useFullGlyphHeight = true;

        /// <summary>
        /// True if the <see cref="SpriteText"/>'s vertical size should be equal to <see cref="TextSize"/> (the full height) or precisely the size of used characters.
        /// Set to false to allow better centering of individual characters/numerals/etc.
        /// </summary>
        public bool UseFullGlyphHeight
        {
            get => useFullGlyphHeight;
            set
            {
                if (useFullGlyphHeight == value)
                    return;
                useFullGlyphHeight = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        private bool fixedWidth;

        /// <summary>
        /// True if all characters should be spaced apart the same distance.
        /// </summary>
        public bool FixedWidth
        {
            get => fixedWidth;
            set
            {
                if (fixedWidth == value)
                    return;
                fixedWidth = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        /// <summary>
        /// An array of characters which should not get a fixed width in a <see cref="FixedWidth"/> instance.
        /// </summary>
        protected virtual char[] FixedWidthExceptionCharacters => default_fixed_width_exceptions;

        private bool requiresAutoSizedWidth => explicitWidth == null && (RelativeSizeAxes & Axes.X) == 0;

        private bool requiresAutoSizedHeight => explicitHeight == null && (RelativeSizeAxes & Axes.Y) == 0;

        private Vector2 autoSizeCache;

        private Vector2 autoSize
        {
            get
            {
                var _ = characters;
                return autoSizeCache;
            }
        }

        private float? explicitWidth;

        /// <summary>
        /// Gets or sets the width of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this width when set.
        /// </summary>
        public override float Width
        {
            get => requiresAutoSizedWidth ? autoSize.X : base.Width;

            set
            {
                if (explicitWidth == value)
                    return;

                base.Width = value;
                explicitWidth = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        private float? explicitHeight;

        /// <summary>
        /// Gets or sets the height of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this height when set.
        /// </summary>
        public override float Height
        {
            get => requiresAutoSizedHeight ? autoSize.Y : base.Height;

            set
            {
                if (explicitHeight == value)
                    return;

                base.Height = value;
                explicitHeight = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        /// <summary>
        /// Gets or sets the size of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this size when set.
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

        private Vector2 spacing;

        /// <summary>
        /// Gets or sets the spacing between characters of this <see cref="SpriteText"/>.
        /// </summary>
        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                if (spacing == value)
                    return;
                spacing = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        private MarginPadding padding;

        /// <summary>
        /// Shrinks the space which may be occupied by characters of this <see cref="SpriteText"/> by the specified amount on each side.
        /// </summary>
        public MarginPadding Padding
        {
            get => padding;
            set
            {
                if (padding.Equals(value))
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Padding)} must be finite, but is {value}.");

                padding = value;

                PropagateInvalidation(InvalidateCharacters());
            }
        }

        public override bool IsPresent => base.IsPresent && (AlwaysPresent || !string.IsNullOrEmpty(text));

        #region Characters

        private Cached charactersCache = new Cached { Name = $"{nameof(SpriteText)}.{nameof(characters)}" };
        private readonly List<CharacterPart> charactersBacking = new List<CharacterPart>();

        /// <summary>
        /// The characters in local space.
        /// </summary>
        private List<CharacterPart> characters
        {
            get
            {
                charactersCache.Compute(computeCharacters);
                return charactersBacking;
            }
        }

        private void computeCharacters()
        {
            charactersBacking.Clear();

            if (store == null)
                return;

            Vector2 currentPos = new Vector2(Padding.Left, Padding.Top);

            try
            {
                if (string.IsNullOrEmpty(Text))
                    return;

                float maxWidth = float.PositiveInfinity;
                if (!requiresAutoSizedWidth)
                {
                    // If x axis is auto sized directly or indirectly, allow infinite expansion for the axis.
                    if (!(Parent != null && RelativeSizeAxes.HasFlag(Axes.X) && Parent.DirectlyOrIndirectlyAutoSizedAxes.HasFlag(Axes.X)))
                    {
                        maxWidth = ApplyRelativeAxesBeforeParentAutoSize(RelativeSizeAxes, new Vector2(base.Width, base.Height), FillMode).X - Padding.Right;
                    }
                }

                float currentRowHeight = 0;

                foreach (var character in Text)
                {
                    // Unscaled size (i.e. not multiplied by TextSize)
                    Vector2 textureSize;
                    Texture texture = null;

                    // Retrieve the texture + size
                    if (char.IsWhiteSpace(character))
                    {
                        float size = FixedWidth ? constantWidth : spaceWidth;

                        if (character == 0x3000)
                        {
                            // Double-width space
                            size *= 2;
                        }

                        textureSize = new Vector2(size);
                    }
                    else
                    {
                        texture = GetTextureForCharacter(character);
                        textureSize = texture == null ? new Vector2(FixedWidth ? constantWidth : spaceWidth) : new Vector2(texture.DisplayWidth, texture.DisplayHeight);
                    }

                    bool useFixedWidth = FixedWidth && !FixedWidthExceptionCharacters.Contains(character);

                    // Scaled glyph size to be used for positioning
                    Vector2 glyphSize = new Vector2(useFixedWidth ? constantWidth : textureSize.X, UseFullGlyphHeight ? 1 : textureSize.Y) * TextSize;

                    // Texture size scaled by TextSize
                    Vector2 scaledTextureSize = textureSize * TextSize;

                    // Check if we need to go onto the next line
                    if (AllowMultiline && currentPos.X + glyphSize.X >= maxWidth)
                    {
                        currentPos.X = Padding.Left;
                        currentPos.Y += currentRowHeight + spacing.Y;
                        currentRowHeight = 0;
                    }

                    // The height of the row depends on whether we want to use the full glyph height or not
                    currentRowHeight = Math.Max(currentRowHeight, glyphSize.Y);

                    if (!char.IsWhiteSpace(character) && texture != null)
                    {
                        // If we have fixed width, we'll need to centre the texture to the glyph size
                        float offset = (glyphSize.X - scaledTextureSize.X) / 2;

                        charactersBacking.Add(new CharacterPart
                        {
                            Texture = texture,
                            DrawRectangle = new RectangleF(new Vector2(currentPos.X + offset, currentPos.Y), scaledTextureSize),
                        });
                    }

                    currentPos.X += glyphSize.X + spacing.X;
                }

                // When we added the last character, we also added the spacing, but we should remove it to get the correct size
                currentPos.X -= spacing.X;

                // The last row needs to be included in the height
                currentPos.Y += currentRowHeight;
            }
            finally
            {
                if (requiresAutoSizedWidth)
                    autoSizeCache.X = currentPos.X + Padding.Right;
                if (requiresAutoSizedHeight)
                    autoSizeCache.Y = currentPos.Y + Padding.Bottom;
            }
        }

        private Cached screenSpaceCharactersCache = new Cached { Name = $"{nameof(SpriteText)}.{nameof(screenSpaceCharacters)}" };
        private readonly List<ScreenSpaceCharacterPart> screenSpaceCharactersBacking = new List<ScreenSpaceCharacterPart>();

        /// <summary>
        /// The characters in screen space. These are ready to be drawn.
        /// </summary>
        private List<ScreenSpaceCharacterPart> screenSpaceCharacters
        {
            get
            {
                screenSpaceCharactersCache.Compute(computeScreenSpaceCharacters);
                return screenSpaceCharactersBacking;
            }
        }

        private void computeScreenSpaceCharacters()
        {
            screenSpaceCharactersBacking.Clear();

            foreach (var character in characters)
            {
                screenSpaceCharactersBacking.Add(new ScreenSpaceCharacterPart
                {
                    DrawQuad = ToScreenSpace(character.DrawRectangle),
                    Texture = character.Texture
                });
            }
        }

        private Cached<float> constantWidthCache = new Cached<float> { Name = $"{nameof(SpriteText)}.{nameof(constantWidth)}" };
        private float constantWidth => constantWidthCache.Compute(computeConstantWidth);
        private float computeConstantWidth() => GetTextureForCharacter('D')?.DisplayWidth ?? 0;

        private Cached<Vector2> shadowOffsetCache = new Cached<Vector2> { Name = $"{nameof(SpriteText)}.{nameof(shadowOffset)}" };
        private Vector2 shadowOffset => shadowOffsetCache.Compute(computeShadowOffset);
        private Vector2 computeShadowOffset() => ToScreenSpace(shadow_offset * TextSize) - ToScreenSpace(Vector2.Zero);

        #endregion

        #region Invalidation

        [MustUseReturnValue]
        protected Invalidation InvalidateScreenSpaceCharacters() => !screenSpaceCharactersCache.Invalidate() ? 0 : InvalidateDrawNode();

        [MustUseReturnValue]
        protected Invalidation InvalidateCharacters() => !charactersCache.Invalidate() ? 0 :
            InvalidateScreenSpaceCharacters() | InvalidateDrawSize() | InvalidateRequiredParentSizeToFit() | InvalidateBoundingBoxSizeBeforeParentAutoSize();

        [MustUseReturnValue]
        protected Invalidation InvalidateConstantWidth() => !constantWidthCache.Invalidate() ? 0 : InvalidateCharacters();

        [MustUseReturnValue]
        protected Invalidation InvalidateShadowOffset() => !shadowOffsetCache.Invalidate() ? 0 : InvalidateDrawNode();

        public override void InvalidateFromParent(Invalidation parentInvalidation, Invalidation selfInvalidation = Invalidation.None)
        {
            if ((parentInvalidation & Invalidation.ChildSizeBeforeAutoSize) != 0)
                selfInvalidation |= InvalidateCharacters();

            if ((parentInvalidation & Invalidation.DrawInfo) != 0)
                selfInvalidation |= InvalidateScreenSpaceCharacters() | InvalidateShadowOffset();

            base.InvalidateFromParent(parentInvalidation, selfInvalidation);
        }

        protected override Invalidation InvalidateAll() =>
            base.InvalidateAll() |
            InvalidateScreenSpaceCharacters() | InvalidateCharacters() |
            InvalidateConstantWidth() | InvalidateShadowOffset();

        #endregion

        #region DrawNode

        private readonly SpriteTextDrawNodeSharedData sharedData = new SpriteTextDrawNodeSharedData();

        protected override DrawNode CreateDrawNode() => new SpriteTextDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            base.ApplyDrawNode(node);

            var n = (SpriteTextDrawNode)node;

            n.Shared = sharedData;

            n.Parts.Clear();
            n.Parts.AddRange(screenSpaceCharacters);

            n.Shadow = Shadow;

            if (Shadow)
            {
                n.ShadowColour = ShadowColour;
                n.ShadowOffset = shadowOffset;
            }
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

            return store.GetCharacter(Font, c) ?? store.GetCharacter(null, c) ?? GetFallbackTextureForCharacter(c);
        }

        /// <summary>
        /// Gets a <see cref="Texture"/> that represents a character which doesn't exist in the current font.
        /// </summary>
        /// <param name="c">The character which doesn't exist in the current font.</param>
        /// <returns>The texture for the given character.</returns>
        protected virtual Texture GetFallbackTextureForCharacter(char c) => GetTextureForCharacter('?');

        public override string ToString()
        {
            return $@"""{Text}"" " + base.ToString();
        }

        private Bindable<string> current;

        /// <summary>
        /// Implements the <see cref="IHasCurrentValue{T}"/> interface.
        /// </summary>
        public Bindable<string> Current
        {
            get => current;
            set
            {
                if (current != null)
                    current.ValueChanged -= setText;

                if (value != null)
                {
                    value.ValueChanged += setText;
                    value.TriggerChange();
                }

                current = value;

                void setText(string t) => Text = t;
            }
        }

        /// <summary>
        /// Gets the base height of the font used by this text. If the font of this text is invalid, 0 is returned.
        /// </summary>
        public float LineBaseHeight
        {
            get
            {
                var baseHeight = store.GetBaseHeight(Font);
                if (baseHeight.HasValue)
                    return baseHeight.Value * TextSize;

                if (string.IsNullOrEmpty(Text))
                    return 0;

                return store.GetBaseHeight(Text[0]).GetValueOrDefault() * TextSize;
            }
        }

        public IEnumerable<string> FilterTerms
        {
            get { yield return Text; }
        }
    }
}
