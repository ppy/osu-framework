// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

        public BlendingInfo(BlendingMode blendingMode)
        {
            switch (blendingMode)
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
        }

        public bool IsDisabled =>
            Source == BlendingFactorSrc.One &&
            Destination == BlendingFactorDest.Zero &&
            SourceAlpha == BlendingFactorSrc.One &&
            DestinationAlpha == BlendingFactorDest.Zero;

        public bool Equals(BlendingInfo other)
        {
            return other.Source == Source && other.Destination == Destination && other.SourceAlpha == SourceAlpha && other.DestinationAlpha == DestinationAlpha;
        }
    }
}
