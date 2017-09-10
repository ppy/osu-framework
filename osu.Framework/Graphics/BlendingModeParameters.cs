// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics
{
    public struct BlendingModeParameters
    {
        public BlendingMode Mode;
        public BlendingEquation RGBEquation;
        public BlendingEquation AlphaEquation;

        public static implicit operator BlendingModeParameters(BlendingMode blendingMode) => new BlendingModeParameters { Mode = blendingMode };
        public static implicit operator BlendingModeParameters(BlendingEquation blendingEquation) => new BlendingModeParameters
        {
            RGBEquation = blendingEquation,
            AlphaEquation = blendingEquation
        };

        public bool Equals(BlendingModeParameters other) => other.Mode == Mode && other.RGBEquation == RGBEquation && other.AlphaEquation == AlphaEquation;
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
