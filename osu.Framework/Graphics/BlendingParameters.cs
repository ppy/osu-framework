// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains information about how an <see cref="IDrawable"/> should be blended into its destination.
    /// </summary>
    public struct BlendingParameters : IEquatable<BlendingParameters>
    {
        private BlendingFactors blendingFactors;
        private BlendingMode mode;

        /// <summary>
        /// The blending factors that represent the current blending mode.
        /// </summary>
        public BlendingFactors BlendingFactors
        {
            get => blendingFactors;
            set
            {
                blendingFactors = value;
                mode = BlendingMode.Custom;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="BlendingMode"/> to use.
        /// </summary>
        public BlendingMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                if (mode != BlendingMode.Custom)
                    blendingFactors = new BlendingFactors(value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="BlendingEquation"/> to use for the RGB components of the blend.
        /// </summary>
        public BlendingEquation RGBEquation;

        /// <summary>
        /// Gets or sets the <see cref="BlendingEquation"/> to use for the alpha component of the blend.
        /// </summary>
        public BlendingEquation AlphaEquation;

        /// <summary>
        /// Gets the <see cref="BlendEquationMode"/> for the currently specified RGB Equation.
        /// </summary>
        public BlendEquationMode RGBEquationMode => translateEquation(RGBEquation);

        /// <summary>
        /// Gets the <see cref="BlendEquationMode"/> for the currently specified Alpha Equation.
        /// </summary>
        public BlendEquationMode AlphaEquationMode => translateEquation(AlphaEquation);

        private static BlendEquationMode translateEquation(BlendingEquation blendingEquation)
        {
            switch (blendingEquation)
            {
                default:
                case BlendingEquation.Inherit:
                case BlendingEquation.Add:
                    return BlendEquationMode.FuncAdd;

                case BlendingEquation.Min:
                    return BlendEquationMode.Min;

                case BlendingEquation.Max:
                    return BlendEquationMode.Max;

                case BlendingEquation.Subtract:
                    return BlendEquationMode.FuncSubtract;

                case BlendingEquation.ReverseSubtract:
                    return BlendEquationMode.FuncReverseSubtract;
            }
        }

        public BlendingParameters(BlendingMode mode)
        {
            RGBEquation = default;
            AlphaEquation = default;
            blendingFactors = new BlendingFactors(mode);
            this.mode = mode;
        }

        public static implicit operator BlendingParameters(BlendingMode blendingMode) => new BlendingParameters { Mode = blendingMode };

        public static implicit operator BlendingParameters(BlendingEquation blendingEquation) => new BlendingParameters
        {
            RGBEquation = blendingEquation,
            AlphaEquation = blendingEquation
        };

        public bool IsDisabled =>
            BlendingFactors.IsDisabled
            && RGBEquation == BlendingEquation.Add
            && AlphaEquation == BlendingEquation.Add;

        public bool Equals(BlendingParameters other) =>
            other.BlendingFactors.Equals(BlendingFactors)
            && other.RGBEquation == RGBEquation
            && other.AlphaEquation == AlphaEquation
            && other.Mode == Mode;

        public override string ToString() => $"BlendingParameter Mode: {Mode} BlendingFactor: {BlendingFactors} RGBEquation: {RGBEquation} AlphaEquation: {AlphaEquation}";
    }

    public struct BlendingFactors : IEquatable<BlendingFactors>
    {
        public readonly BlendingFactorSrc Source;
        public readonly BlendingFactorDest Destination;
        public readonly BlendingFactorSrc SourceAlpha;
        public readonly BlendingFactorDest DestinationAlpha;

        public BlendingFactors(BlendingMode mode)
        {
            switch (mode)
            {
                case BlendingMode.Custom:
                case BlendingMode.Inherit:
                case BlendingMode.Mixture:
                    Source = BlendingFactorSrc.SrcAlpha;
                    Destination = BlendingFactorDest.OneMinusSrcAlpha;
                    SourceAlpha = BlendingFactorSrc.One;
                    DestinationAlpha = BlendingFactorDest.One;
                    break;

                case BlendingMode.Additive:
                    Source = BlendingFactorSrc.SrcAlpha;
                    Destination = BlendingFactorDest.One;
                    SourceAlpha = BlendingFactorSrc.One;
                    DestinationAlpha = BlendingFactorDest.One;
                    break;

                default:
                    Source = BlendingFactorSrc.One;
                    Destination = BlendingFactorDest.Zero;
                    SourceAlpha = BlendingFactorSrc.One;
                    DestinationAlpha = BlendingFactorDest.Zero;
                    break;
            }
        }

        public BlendingFactors(BlendingFactorSrc source, BlendingFactorDest destination, BlendingFactorSrc sourceAlpha, BlendingFactorDest destinationAlpha)
        {
            Source = source;
            Destination = destination;
            SourceAlpha = sourceAlpha;
            DestinationAlpha = destinationAlpha;
        }

        public bool Equals(BlendingFactors other) =>
            other.Source == Source
            && other.Destination == Destination
            && other.SourceAlpha == SourceAlpha
            && other.DestinationAlpha == DestinationAlpha;

        public bool IsDisabled =>
            Source == BlendingFactorSrc.One
            && Destination == BlendingFactorDest.Zero
            && SourceAlpha == BlendingFactorSrc.One
            && DestinationAlpha == BlendingFactorDest.Zero;

        public override string ToString() => $"{Source}/{Destination}/{SourceAlpha}/{DestinationAlpha}";
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
        /// The blending mode will be manually provided.
        /// </summary>
        Custom,

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
