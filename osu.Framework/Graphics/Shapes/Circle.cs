// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Shapes
{
    public class Circle : CircularContainer
    {
        public Circle()
        {
            Masking = true;

            AddInternal(new Box()
            {
                RelativeSizeAxes = Axes.Both,
            });
        }
    }
}
