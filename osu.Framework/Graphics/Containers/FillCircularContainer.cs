// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    public class FillCircularContainer : CircularContainer
    {
        private readonly Box fill;

        public SRGBColour FillColour
        {
            get
            {
                return fill.Colour;
            }
            set
            {
                fill.Colour = value;
            }
        }

        public ColourInfo FillColourInfo
        {
            get
            {
                return fill.ColourInfo;
            }
            set
            {
                fill.ColourInfo = value;
            }
        }

        public FillCircularContainer()
        {
            Masking = true;

            AddInternal(fill = new Box()
            {
                RelativeSizeAxes = Axes.Both,
            });
        }
    }
}
