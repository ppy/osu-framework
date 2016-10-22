// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;

namespace osu.Framework.Graphics.Containers
{
    public class CircularContainer : Container
    {
        public CircularContainer()
        {
            Masking = true;
            Origin = Anchor.Centre;
        }

        public override float CornerRadius
        {
            get
            {
                return DrawSize.X / 2f;
            }

            set
            {
                Debug.Assert(false, "Cannot manually set CornerRadius of CircularContainer.");
            }
        }
    }
}
