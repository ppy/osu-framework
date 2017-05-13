// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public interface IOccluder : IDrawable
    {
    }

    public static class OccluderExtensions
    {
        public static bool Occludes(this IOccluder occluder, IDrawable drawable)
        {
            return occluder.ScreenSpaceBoundingBox.Occludes(drawable.ScreenSpaceBoundingBox);
        }
    }
}
