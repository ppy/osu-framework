// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public interface IHasOccluder : IDrawable
    {
        /// <summary>
        /// The occluding drawable.
        /// </summary>
        IDrawable Occluder { get; }
    }

    public static class HasOccluderExtensions
    {
        /// <summary>
        /// Whether an <see cref="IHasOccluder"/> occludes a drawable. This will be true if the <see cref="IHasOccluder.Occluder"/>
        /// is completely contained within the <see cref="IHasOccluder.Occluder"/> polygon.
        /// </summary>
        /// <param name="us">The <see cref="IHasOccluder"/>.</param>
        /// <param name="drawable">The drawable to check.</param>
        /// <returns>Whether the <see cref="IHasOccluder.Occluder"/> occludes <paramref name="drawable"/>.</returns>
        public static bool Occludes(this IHasOccluder us, IDrawable drawable)
        {
            IDrawable occluder = us.Occluder;

            return occluder != null && occluder.ScreenSpacePolygon.Occludes(drawable.ScreenSpacePolygon);
        }
    }
}
