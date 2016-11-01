// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

        public BlendingInfo(bool additive)
        {
            Source = BlendingFactorSrc.OneMinusDstAlpha;
            Destination = BlendingFactorDest.One;
            SourceAlpha = BlendingFactorSrc.OneMinusDstAlpha;
            DestinationAlpha = BlendingFactorDest.One;

            Additive = additive;
        }

        public bool Additive
        {
            set
            {
                SourceAlpha = value ? BlendingFactorSrc.Zero : BlendingFactorSrc.OneMinusDstAlpha;
                //DestinationAlpha = value ? BlendingFactorDest.One : BlendingFactorDest.One;
            }
        }

        /// <summary>
        /// Copies the current BlendingInfo into target.
        /// </summary>
        /// <param name="target">The BlendingInfo to be filled with the copy.</param>
        public void Copy(ref BlendingInfo target)
        {
            target.Source = Source;
            target.Destination = Destination;
            target.SourceAlpha = Source;
            target.DestinationAlpha = Destination;
        }

        public bool Equals(BlendingInfo other)
        {
            return other.Source == Source && other.Destination == Destination && other.SourceAlpha == SourceAlpha && other.DestinationAlpha == DestinationAlpha;
        }
    }
}
