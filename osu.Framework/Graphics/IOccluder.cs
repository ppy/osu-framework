// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public interface IHasOccluder : IDrawable
    {
        IDrawable Occluder { get; }
    }

    public static class HasOccluderExtensions
    {
        public static bool Occludes(this IHasOccluder us, IDrawable drawable)
        {
            IDrawable occluder = us.Occluder;
            return occluder.DrawInfo.Colour.AverageColour.Linear.A == 1
                   && occluder.ScreenSpaceBoundingBox.Occludes(drawable.ScreenSpaceBoundingBox);
        }
    }
}
