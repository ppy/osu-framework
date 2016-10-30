// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    public struct BlendingInfo
    {
        public BlendingFactorSrc Source;
        public BlendingFactorDest Destination;

        public BlendingInfo(BlendingFactorSrc? source = null, BlendingFactorDest? destination = null)
        {
            Source = source ?? BlendingFactorSrc.SrcAlpha;
            Destination = destination ?? BlendingFactorDest.OneMinusSrcAlpha;
        }

        public BlendingInfo(bool additive)
        {
            Source = BlendingFactorSrc.SrcAlpha;
            Destination = additive ? BlendingFactorDest.One : BlendingFactorDest.OneMinusSrcAlpha;
        }

        public bool Additive
        {
            get { return Source == BlendingFactorSrc.SrcAlpha && Destination == BlendingFactorDest.One; }
            set { Destination = value ? BlendingFactorDest.One : BlendingFactorDest.OneMinusSrcAlpha; }
        }

        /// <summary>
        /// Copies the current BlendingInfo into target.
        /// </summary>
        /// <param name="target">The BlendingInfo to be filled with the copy.</param>
        public void Copy(ref BlendingInfo target)
        {
            target.Source = Source;
            target.Destination = Destination;
        }

        public bool Equals(BlendingInfo other)
        {
            return other.Source == Source && other.Destination == Destination;
        }
    }
}
