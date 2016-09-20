// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Containers
{
    public class MaskingContainer : LargeContainer
    {
        public override void Load()
        {
            base.Load();
            Masking = true;
        }
    }
}
