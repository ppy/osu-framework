// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A text that is drawn with an outline.
    /// </summary>
    public class OutlinedText : Container
    {
        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        public string Text
        {
            get { return text.Text; }
            set
            {
                text.Text = value;
                bfcText.Text = value;
            }
        }
        /// <summary>
        /// The size of the text in local space. This means that if TextSize is set to 16, a single line will have a height of 16.
        /// </summary>
        public float TextSize
        {
            get { return text.TextSize; }
            set
            {
                text.TextSize = value;
                bfcText.TextSize = value;
            }
        }
        /// <summary>
        /// True if the text should be wrapped if it gets too wide. Note that \n does NOT cause a line break. If you need explicit line breaks, use <see cref="TextFlowContainer"/> instead.
        /// </summary>
        public bool AllowMultiline
        {
            get { return text.AllowMultiline; }
            set
            {
                text.AllowMultiline = true;
                bfcText.AllowMultiline = true;
            }
        }
        /// <summary>
        /// True if all characters should be spaced apart the same distance.
        /// </summary>
        public bool FixedWidth
        {
            get { return text.FixedWidth; }
            set
            {
                text.FixedWidth = value;
                bfcText.FixedWidth = value;
            }
        }
        /// <summary>
        /// The name of the font to use when looking up textures for the individual characters.
        /// </summary>
        public string Font
        {
            get { return text.Font; }
            set
            {
                text.Font = value;
                bfcText.Font = value;
            }
        }
        /// <summary>
        /// Gets or sets the colour of the text.
        /// </summary>
        public Color4 TextColour
        {
            get { return text.Colour; }
            set { text.Colour = value; }
        }
        /// <summary>
        /// Gets or sets the colour of the outline.
        /// </summary>
        public Color4 OutlineColour
        {
            get { return bfcText.Colour; }
            set { bfcText.Colour = value; }
        }
        /// <summary>
        /// Gets or sets the strength of the outline.
        /// </summary>
        public float OutlineStrength
        {
            get { return bfc.Alpha; }
            set { bfc.Alpha = value; }
        }
        /// <summary>
        /// Gets or sets the sigma value corresponding to standard deviation of the gaussian distribution of the blur effect used to create the outline.
        /// </summary>
        public float OutlineSigmaX
        {
            get { return bfc.BlurSigma.X; }
            set
            {
                bfc.BlurSigma = new Vector2(value, bfc.BlurSigma.Y);
                updateBfcPadding();
            }
        }
        /// <summary>
        /// Gets or sets the sigma value corresponding to standard deviation of the gaussian distribution of the blur effect used to create the outline.
        /// </summary>
        public float OutlineSigmaY
        {
            get { return bfc.BlurSigma.Y; }
            set
            {
                bfc.BlurSigma = new Vector2(bfc.BlurSigma.X, value);
                updateBfcPadding();
            }
        }

        private SpriteText text;
        private BufferedContainer bfc;
        private SpriteText bfcText;

        /// <summary>
        /// Constructs a new outlined text with default values.
        /// </summary>
        public OutlinedText()
        {
            Children = new Drawable[]
            {
                text = new SpriteText
                {
                    Text = "",
                    TextSize = 24f,
                    Colour = Color4.White,
                    Depth = float.MinValue,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                bfc = (bfcText = new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = text.Text,
                    TextSize = text.TextSize,
                    Colour = Color4.White,
                }).WithEffect(new BlurEffect
                {
                    Sigma = new Vector2(2f, 2f),
                    Strength = 1f
                })
            };

            updateBfcPadding();
            AutoSizeAxes = Axes.Both;
        }

        private void updateBfcPadding()
        {
            bfc.Padding = new MarginPadding
            {
                Horizontal = Blur.KernelSize(bfc.BlurSigma.X),
                Vertical = Blur.KernelSize(bfc.BlurSigma.Y)
            };
        }
    }
}
