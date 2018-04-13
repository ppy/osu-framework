// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains information about how an <see cref="IDrawable"/> should be blended into its destination.
    /// </summary>
    public struct BlendingParameters
    {
        /// <summary>
        /// Gets or sets <see cref="BlendingMode"/> to use.
        /// </summary>
        public BlendingMode Mode;

        /// <summary>
        /// Gets or sets the <see cref="BlendingEquation"/> to use for the RGB components of the blend.
        /// </summary>
        public BlendingEquation RGBEquation;

        /// <summary>
        /// Gets or sets the <see cref="BlendingEquation"/> to use for the alpha component of the blend.
        /// </summary>
        public BlendingEquation AlphaEquation;

        public static implicit operator BlendingParameters(BlendingMode blendingMode) => new BlendingParameters { Mode = blendingMode };
        public static implicit operator BlendingParameters(BlendingEquation blendingEquation) => new BlendingParameters
        {
            RGBEquation = blendingEquation,
            AlphaEquation = blendingEquation
        };

        public bool Equals(BlendingParameters other) => other.Mode == Mode && other.RGBEquation == RGBEquation && other.AlphaEquation == AlphaEquation;
    }

    public enum BlendingMode
    {
        /// <summary>
        /// Inherits from parent.
        /// </summary>
        Inherit = 0,
        /// <summary>
        /// Mixes with existing colour by a factor of the colour's alpha.
        /// </summary>
        Mixture,
        /// <summary>
        /// Purely additive (by a factor of the colour's alpha) blending.
        /// </summary>
        Additive,
        /// <summary>
        /// No alpha blending whatsoever.
        /// </summary>
        None,
    }

    public enum BlendingEquation
    {
        /// <summary>
        /// Inherits from parent.
        /// </summary>
        Inherit = 0,
        /// <summary>
        /// Adds the source and destination colours.
        /// </summary>
        Add,
        /// <summary>
        /// Chooses the minimum of each component of the source and destination colours.
        /// </summary>
        Min,
        /// <summary>
        /// Chooses the maximum of each component of the source and destination colours.
        /// </summary>
        Max,
        /// <summary>
        /// Subtracts the destination colour from the source colour.
        /// </summary>
        Subtract,
        /// <summary>
        /// Subtracts the source colour from the destination colour.
        /// </summary>
        ReverseSubtract,
    }
}
