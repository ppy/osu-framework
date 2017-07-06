// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A wrapper which delays the loading of children until we have been on-screen for a specified duration.
    /// In order to benefit from delayed load, we must be inside a <see cref="ScrollContainer"/>.
    /// </summary>
    public class DelayedLoadWrapper : AsyncLoadWrapper
    {
        public DelayedLoadWrapper(Drawable content)
            : base(content)
        {
        }

        /// <summary>
        /// The amount of time on-screen before we begin a load of children.
        /// </summary>
        public double TimeBeforeLoad = 500;

        private double timeVisible;

        protected override bool ShouldLoadContent => timeVisible > TimeBeforeLoad;

        protected override void Update()
        {
            //this code can be expensive, so only run if we haven't yet loaded.
            if (!LoadTriggered)
            {
                if (!isIntersecting)
                    timeVisible = 0;
                else
                    timeVisible += Time.Elapsed;
            }

            base.Update();
        }

        private Cached<bool> isIntersectingBacking;

        private bool isIntersecting => isIntersectingBacking.IsValid ? isIntersectingBacking : (isIntersectingBacking.Value = checkScrollIntersection());

        private bool checkScrollIntersection()
        {
            IOnScreenOptimisingContainer scroll = null;
            CompositeDrawable cursor = this;
            while (scroll == null && (cursor = cursor.Parent) != null)
                scroll = cursor as IOnScreenOptimisingContainer;

            return scroll?.ScreenSpaceDrawQuad.Intersects(ScreenSpaceDrawQuad) ?? true;
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            isIntersectingBacking.Invalidate();
            return base.Invalidate(invalidation, source, shallPropagate);
        }

        /// <summary>
        /// A container which acts as a masking parent for on-screen delayed load optimisations.
        /// </summary>
        public interface IOnScreenOptimisingContainer
        {
            Quad ScreenSpaceDrawQuad { get; }
        }
    }
}
