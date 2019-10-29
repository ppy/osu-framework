// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.MathUtils;
using osu.Framework.Text;
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
        private static readonly char[] default_never_fixed_width_characters = { '.', ',', ':', ' ' };

        [Resolved]
        private FontStore store { get; set; }

        [Resolved]
        private LocalisationManager localisation { get; set; }

        private ILocalisedBindableString localisedText;

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

            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // Pre-cache the characters in the texture store
            foreach (var character in displayedText)
            {
                var unused = store.Get(font.FontName, character) ?? store.Get(null, character);
            }
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

        private Bindable<string> currentBound;

        public Bindable<string> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (currentBound != null) current.UnbindFrom(currentBound);
                current.BindTo(currentBound = value);
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
                font = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        private bool allowMultiline = true;

        /// <summary>
        /// True if the text should be wrapped if it gets too wide. Note that \n does NOT cause a line break. If you need explicit line breaks, use <see cref="TextFlowContainer"/> instead.
        /// </summary>
        /// <remarks>
        /// If enabled, <see cref="Truncate"/> will be disabled.
        /// </remarks>
        public bool AllowMultiline
        {
            get => allowMultiline;
            set
            {
                if (allowMultiline == value)
                    return;

                if (value)
                    Truncate = false;

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

        private Vector2 shadowOffset = new Vector2(0, 0.06f);

        /// <summary>
        /// The offset of the shadow displayed around the text. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Vector2 ShadowOffset
        {
            get => shadowOffset;
            set
            {
                if (shadowOffset == value)
                    return;

                shadowOffset = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
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

        private bool truncate;

        /// <summary>
        /// If true, text should be truncated when it exceeds the <see cref="Drawable.DrawWidth"/> of this <see cref="SpriteText"/>.
        /// </summary>
        /// <remarks>
        /// Has no effect if no <see cref="Width"/> or custom sizing is set.
        /// If enabled, <see cref="AllowMultiline"/> will be disabled.
        /// </remarks>
        public bool Truncate
        {
            get => truncate;
            set
            {
                if (truncate == value) return;

                if (value)
                    AllowMultiline = false;

                truncate = value;
                invalidate(true);
            }
        }

        private string ellipsisString = "…";

        /// <summary>
        /// When <see cref="Truncate"/> is enabled, this decides what string is used to signify that truncation has occured.
        /// Defaults to "…".
        /// </summary>
        public string EllipsisString
        {
            get => ellipsisString;
            set
            {
                if (ellipsisString == value) return;

                ellipsisString = value;
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

        private float maxWidth = float.PositiveInfinity;

        /// <summary>
        /// The maximum width of this <see cref="SpriteText"/>. Affects both auto and fixed sizing modes.
        /// </summary>
        /// <remarks>
        /// This becomes a relative value if this <see cref="SpriteText"/> is relatively-sized on the X-axis.
        /// </remarks>
        public float MaxWidth
        {
            get => maxWidth;
            set
            {
                if (maxWidth == value)
                    return;

                maxWidth = value;
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

        private readonly Cached charactersCache = new Cached();
        private readonly List<TextBuilderGlyph> charactersBacking = new List<TextBuilderGlyph>();

        /// <summary>
        /// The characters in local space.
        /// </summary>
        private List<TextBuilderGlyph> characters
        {
            get
            {
                computeCharacters();
                return charactersBacking;
            }
        }

        private bool isComputingCharacters;

        /// <summary>
        /// Compute character textures and positions.
        /// </summary>
        private void computeCharacters()
        {
            if (LoadState >= LoadState.Loaded)
                ThreadSafety.EnsureUpdateThread();

            if (store == null)
                return;

            if (charactersCache.IsValid)
                return;

            charactersBacking.Clear();

            Debug.Assert(!isComputingCharacters, "Cyclic invocation of computeCharacters()!");
            isComputingCharacters = true;

            TextBuilder textBuilder = null;

            try
            {
                if (string.IsNullOrEmpty(displayedText))
                    return;

                textBuilder = CreateTextBuilder(store);
                textBuilder.AddText(displayedText);
            }
            finally
            {
                if (requiresAutoSizedWidth)
                    base.Width = (textBuilder?.Bounds.X ?? 0) + Padding.Right;
                if (requiresAutoSizedHeight)
                    base.Height = (textBuilder?.Bounds.Y ?? 0) + Padding.Bottom;

                base.Width = Math.Min(base.Width, MaxWidth);

                isComputingCharacters = false;
                charactersCache.Validate();
            }
        }

        private readonly Cached screenSpaceCharactersCache = new Cached();
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

            Vector2 inflationAmount = DrawInfo.MatrixInverse.ExtractScale().Xy;

            foreach (var character in characters)
            {
                screenSpaceCharactersBacking.Add(new ScreenSpaceCharacterPart
                {
                    DrawQuad = ToScreenSpace(character.DrawRectangle.Inflate(inflationAmount)),
                    InflationPercentage = Vector2.Divide(inflationAmount, character.DrawRectangle.Size),
                    Texture = character.Texture
                });
            }

            screenSpaceCharactersCache.Validate();
        }

        private readonly Cached<Vector2> shadowOffsetCache = new Cached<Vector2>();

        private Vector2 premultipliedShadowOffset =>
            shadowOffsetCache.IsValid ? shadowOffsetCache.Value : shadowOffsetCache.Value = ToScreenSpace(shadowOffset * Font.Size) - ToScreenSpace(Vector2.Zero);

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

        /// <summary>
        /// The characters that should be excluded from fixed-width application. Defaults to (".", ",", ":", " ") if null.
        /// </summary>
        protected virtual char[] FixedWidthExcludeCharacters { get; } = null;

        /// <summary>
        /// The character to fallback to use if a character glyph lookup failed.
        /// </summary>
        protected virtual char FallbackCharacter => '?';

        /// <summary>
        /// Creates a <see cref="TextBuilder"/> to generate the character layout for this <see cref="SpriteText"/>.
        /// </summary>
        /// <param name="store">The <see cref="ITexturedGlyphLookupStore"/> where characters should be retrieved from.</param>
        /// <returns>The <see cref="TextBuilder"/>.</returns>
        protected virtual TextBuilder CreateTextBuilder(ITexturedGlyphLookupStore store)
        {
            var excludeCharacters = FixedWidthExcludeCharacters ?? default_never_fixed_width_characters;

            float builderMaxWidth = requiresAutoSizedWidth
                ? MaxWidth
                : ApplyRelativeAxes(RelativeSizeAxes, new Vector2(Math.Min(MaxWidth, base.Width), base.Height), FillMode).X - Padding.Right;

            if (AllowMultiline)
            {
                return new MultilineTextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, new Vector2(Padding.Left, Padding.Top), Spacing, charactersBacking,
                    excludeCharacters, FallbackCharacter);
            }

            if (Truncate)
            {
                return new TruncatingTextBuilder(store, Font, builderMaxWidth, ellipsisString, UseFullGlyphHeight, new Vector2(Padding.Left, Padding.Top), Spacing, charactersBacking,
                    excludeCharacters, FallbackCharacter);
            }

            return new TextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, new Vector2(Padding.Left, Padding.Top), Spacing, charactersBacking,
                excludeCharacters, FallbackCharacter);
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

        public IEnumerable<string> FilterTerms => displayedText.Yield();
    }
}
