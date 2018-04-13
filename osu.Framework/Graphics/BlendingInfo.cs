// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    public struct BlendingInfo
    {
        public BlendingFactorSrc Source;
        public BlendingFactorDest Destination;
        public BlendingFactorSrc SourceAlpha;
        public BlendingFactorDest DestinationAlpha;

        public BlendEquationMode RGBEquation;
        public BlendEquationMode AlphaEquation;

        public BlendingInfo(BlendingParameters parameters)
        {
            switch (parameters.Mode)
            {
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

            RGBEquation = translateEquation(parameters.RGBEquation);
            AlphaEquation = translateEquation(parameters.AlphaEquation);

        }

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

        public bool IsDisabled =>
            Source == BlendingFactorSrc.One
            && Destination == BlendingFactorDest.Zero
            && SourceAlpha == BlendingFactorSrc.One
            && DestinationAlpha == BlendingFactorDest.Zero
            && RGBEquation == BlendEquationMode.FuncAdd
            && AlphaEquation == BlendEquationMode.FuncAdd;

        public bool Equals(BlendingInfo other) =>
            other.Source == Source
            && other.Destination == Destination
            && other.SourceAlpha == SourceAlpha
            && other.DestinationAlpha == DestinationAlpha
            && other.RGBEquation == RGBEquation
            && other.AlphaEquation == AlphaEquation;
    }
}
