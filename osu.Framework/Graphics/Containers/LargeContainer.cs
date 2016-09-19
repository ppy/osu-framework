//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    public class LargeContainer : Container
    {
        public override InheritMode SizeMode
        {
            get
            {
                return InheritMode.XY;
            }

            set
            {
                throw new NotSupportedException(@"Can't change SizeMode on a LargeContainer");
            }
        }
    }
}
