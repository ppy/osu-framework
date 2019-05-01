// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.MathUtils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A container for simple text rendering purposes. If more complex text rendering is required, use <see cref="TextFlowContainer"/> instead.
    /// </summary>
    public partial class SpriteText : Drawable, IHasLineBaseHeight, ITexturedShaderDrawable, IHasText, IHasFilterTerms, IFillFlowContainer, IHasCurrentValue<string>
    {
        private const float default_text_size = 20;
        private static readonly Vector2 shadow_offset = new Vector2(0, 0.06f);

        [Resolved]
        private FontStore store { get; set; }

        [Resolved]
        private LocalisationManager localisation { get; set; }

        private ILocalisedBindableString localisedText;

        private float spaceWidth;

        public IShader TextureShader { get; private set; }
        public IShader RoundedTextureShader { get; private set; }

        public SpriteText()
        {
            current.BindValueChanged(text => Text = text.NewValue);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            localisedText = localisation.GetLocalisedString(text);
            localisedText.BindValueChanged(str =>
            {
                if (string.IsNullOrEmpty(str.NewValue))
                {
                    // We'll become not present and won't update the characters to set the size to 0, so do it manually
                    if (requiresAutoSizedWidth)
                        base.Width = Padding.TotalHorizontal;
                    if (requiresAutoSizedHeight)
                        base.Height = Padding.TotalVertical;
                }

                invalidate(true);
            }, true);

            spaceWidth = getTextureForCharacter('.')?.DisplayWidth * 2 ?? 1;

            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // Pre-cache the characters in the texture store
            foreach (var character in displayedText)
                getTextureForCharacter(character);
        }

        private LocalisedString text = string.Empty;

        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        public LocalisedString Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                current.Value = text;

                if (localisedText != null)
                    localisedText.Text = value;
            }
        }

        private readonly Bindable<string> current = new Bindable<string>(string.Empty);

        public Bindable<string> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        private string displayedText => localisedText?.Value ?? text.Text.Original;

        string IHasText.Text
        {
            get => Text;
            set => Text = value;
        }

        private FontUsage font = FontUsage.Default;

        /// <summary>
        /// Contains information on the font used to display the text.
        /// </summary>
        public FontUsage Font
        {
            get => font;
            set
            {
                // The implicit operator can be used to convert strings to fonts, which discards size + fixedwidth in doing so
                // For the time being, we'll forward those members from the original value
                // Todo: Remove this along with all other obsolete members
                if (value.Legacy)
                    value = new FontUsage(value.Family, font.Size, value.Weight, value.Italics, font.FixedWidth);

                font = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        /// <summary>
        /// The size of the text in local space. This means that if TextSize is set to 16, a single line will have a height of 16.
        /// </summary>
        [Obsolete("Setting TextSize directly is deprecated. Use `Font = text.Font.With(size: value)` (see: https://github.com/ppy/osu-framework/pull/2043)")]
        public float TextSize
        {
            get => Font.Size;
            set => Font = Font.With(size: value);
        }

        /// <summary>
        /// True if all characters should be spaced apart the same distance.
        /// </summary>
        [Obsolete("Setting FixedWidth directly is deprecated. Use `Font = text.Font.With(fixedWidth: value)` (see: https://github.com/ppy/osu-framework/pull/2043)")]
        public bool FixedWidth
        {
            get => Font.FixedWidth;
            set => Font = Font.With(fixedWidth: value);
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

                invalidate(true);
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

                Invalidate(Invalidation.DrawNode);
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

                Invalidate(Invalidation.DrawNode);
            }
        }

        private bool useFullGlyphHeight = true;

        /// <summary>
        /// True if the <see cref="SpriteText"/>'s vertical size should be equal to <see cref="FontUsage.Size"/>  (the full height) or precisely the size of used characters.
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

                invalidate(true);
            }
        }

        private bool requiresAutoSizedWidth => explicitWidth == null && (RelativeSizeAxes & Axes.X) == 0;

        private bool requiresAutoSizedHeight => explicitHeight == null && (RelativeSizeAxes & Axes.Y) == 0;

        private float? explicitWidth;

        /// <summary>
        /// Gets or sets the width of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this width when set.
        /// </summary>
        public override float Width
        {
            get
            {
                if (requiresAutoSizedWidth)
                    computeCharacters();
                return base.Width;
            }
            set
            {
                if (explicitWidth == value)
                    return;

                base.Width = value;
                explicitWidth = value;

                invalidate(true);
            }
        }

        private float? explicitHeight;

        /// <summary>
        /// Gets or sets the height of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this height when set.
        /// </summary>
        public override float Height
        {
            get
            {
                if (requiresAutoSizedHeight)
                    computeCharacters();
                return base.Height;
            }
            set
            {
                if (explicitHeight == value)
                    return;

                base.Height = value;
                explicitHeight = value;

                invalidate(true);
            }
        }

        /// <summary>
        /// Gets or sets the size of this <see cref="SpriteText"/>. The <see cref="SpriteText"/> will maintain this size when set.
        /// </summary>
        public override Vector2 Size
        {
            get
            {
                if (requiresAutoSizedWidth || requiresAutoSizedHeight)
                    computeCharacters();
                return base.Size;
            }
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

                invalidate(true);
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

                invalidate(true);
            }
        }

        public override bool IsPresent => base.IsPresent && (AlwaysPresent || !string.IsNullOrEmpty(displayedText));

        #region Characters

        private Cached charactersCache = new Cached();
        private readonly List<CharacterPart> charactersBacking = new List<CharacterPart>();

        /// <summary>
        /// The characters in local space.
        /// </summary>
        private List<CharacterPart> characters
        {
            get
            {
                computeCharacters();
                return charactersBacking;
            }
        }

        private bool isComputingCharacters;

        private void computeCharacters()
        {
            if (store == null)
                return;

            if (charactersCache.IsValid)
                return;

            charactersBacking.Clear();

            Debug.Assert(!isComputingCharacters, "Cyclic invocation of computeCharacters()!");
            isComputingCharacters = true;

            Vector2 currentPos = new Vector2(Padding.Left, Padding.Top);

            try
            {
                if (string.IsNullOrEmpty(displayedText))
                    return;

                float maxWidth = float.PositiveInfinity;
                if (!requiresAutoSizedWidth)
                    maxWidth = ApplyRelativeAxes(RelativeSizeAxes, new Vector2(base.Width, base.Height), FillMode).X - Padding.Right;

                float currentRowHeight = 0;

                foreach (var character in displayedText)
                {
                    bool useFixedWidth = Font.FixedWidth && UseFixedWidthForCharacter(character);

                    // Unscaled size (i.e. not multiplied by Font.Size)
                    Vector2 textureSize;
                    Texture texture = null;

                    // Retrieve the texture + size
                    if (char.IsWhiteSpace(character))
                    {
                        float size = useFixedWidth ? constantWidth : spaceWidth;

                        if (character == 0x3000)
                        {
                            // Double-width space
                            size *= 2;
                        }

                        textureSize = new Vector2(size);
                    }
                    else
                    {
                        texture = getTextureForCharacter(character);
                        textureSize = texture == null ? new Vector2(useFixedWidth ? constantWidth : spaceWidth) : new Vector2(texture.DisplayWidth, texture.DisplayHeight);
                    }

                    // Scaled glyph size to be used for positioning
                    Vector2 glyphSize = new Vector2(useFixedWidth ? constantWidth : textureSize.X, UseFullGlyphHeight ? 1 : textureSize.Y) * Font.Size;

                    // Texture size scaled by Font.Size
                    Vector2 scaledTextureSize = textureSize * Font.Size;

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
                    base.Width = currentPos.X + Padding.Right;
                if (requiresAutoSizedHeight)
                    base.Height = currentPos.Y + Padding.Bottom;

                isComputingCharacters = false;
                charactersCache.Validate();
            }
        }

        private Cached screenSpaceCharactersCache = new Cached();
        private readonly List<ScreenSpaceCharacterPart> screenSpaceCharactersBacking = new List<ScreenSpaceCharacterPart>();

        /// <summary>
        /// The characters in screen space. These are ready to be drawn.
        /// </summary>
        private List<ScreenSpaceCharacterPart> screenSpaceCharacters
        {
            get
            {
                computeScreenSpaceCharacters();
                return screenSpaceCharactersBacking;
            }
        }

        private void computeScreenSpaceCharacters()
        {
            if (screenSpaceCharactersCache.IsValid)
                return;

            screenSpaceCharactersBacking.Clear();

            foreach (var character in characters)
            {
                screenSpaceCharactersBacking.Add(new ScreenSpaceCharacterPart
                {
                    DrawQuad = ToScreenSpace(character.DrawRectangle),
                    Texture = character.Texture
                });
            }

            screenSpaceCharactersCache.Validate();
        }

        private Cached<float> constantWidthCache;
        private float constantWidth => constantWidthCache.IsValid ? constantWidthCache.Value : constantWidthCache.Value = getTextureForCharacter('D')?.DisplayWidth ?? 0;

        private Cached<Vector2> shadowOffsetCache;
        private Vector2 shadowOffset => shadowOffsetCache.IsValid ? shadowOffsetCache.Value : shadowOffsetCache.Value = ToScreenSpace(shadow_offset * Font.Size) - ToScreenSpace(Vector2.Zero);

        #endregion

        #region Invalidation

        private void invalidate(bool layout = false)
        {
            if (layout)
                charactersCache.Invalidate();
            screenSpaceCharactersCache.Invalidate();

            Invalidate(Invalidation.DrawNode, shallPropagate: false);
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            base.Invalidate(invalidation, source, shallPropagate);

            if (source == Parent)
            {
                // Colour captures presence changes
                if ((invalidation & (Invalidation.DrawSize | Invalidation.Presence)) > 0)
                    invalidate(true);

                if ((invalidation & Invalidation.DrawInfo) > 0)
                {
                    invalidate();
                    shadowOffsetCache.Invalidate();
                }
            }
            else if ((invalidation & Invalidation.MiscGeometry) > 0)
                invalidate();

            return true;
        }

        #endregion

        #region DrawNode

        protected override DrawNode CreateDrawNode() => new SpriteTextDrawNode(this);

        #endregion

        private Texture getTextureForCharacter(char c) => GetTextureForCharacter(c) ?? GetFallbackTextureForCharacter(c);

        /// <summary>
        /// Gets the texture for the given character.
        /// </summary>
        /// <param name="c">The character to get the texture for.</param>
        /// <returns>The texture for the given character.</returns>
        protected virtual Texture GetTextureForCharacter(char c)
        {
            if (store == null)
                return null;

            return store.GetCharacter(Font.FontName, c) ?? store.GetCharacter(null, c);
        }

        /// <summary>
        /// Gets a <see cref="Texture"/> that represents a character which doesn't exist in the current font.
        /// </summary>
        /// <param name="c">The character which doesn't exist in the current font.</param>
        /// <returns>The texture for the given character.</returns>
        protected virtual Texture GetFallbackTextureForCharacter(char c) => GetTextureForCharacter('?');

        /// <summary>
        /// Whether the visual representation of a character should use fixed width when <see cref="FontUsage.FixedWidth"/> is true.
        /// By default, this includes the following characters, commonly used in numerical formatting: '.' ',' ':' and ' '
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>Whether the visual representation of <paramref name="c"/> should use a fixed width.</returns>
        protected virtual bool UseFixedWidthForCharacter(char c)
        {
            switch (c)
            {
                case '.':
                case ',':
                case ':':
                case ' ':
                    return false;
            }

            return true;
        }

        public override string ToString() => $@"""{displayedText}"" " + base.ToString();

        /// <summary>
        /// Gets the base height of the font used by this text. If the font of this text is invalid, 0 is returned.
        /// </summary>
        public float LineBaseHeight
        {
            get
            {
                var baseHeight = store.GetBaseHeight(Font.FontName);
                if (baseHeight.HasValue)
                    return baseHeight.Value * Font.Size;

                if (string.IsNullOrEmpty(displayedText))
                    return 0;

                return store.GetBaseHeight(displayedText[0]).GetValueOrDefault() * Font.Size;
            }
        }

        public IEnumerable<string> FilterTerms
        {
            get { yield return displayedText; }
        }
    }
}
