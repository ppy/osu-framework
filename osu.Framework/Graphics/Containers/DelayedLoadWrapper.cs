// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which asynchronously loads specified content.
    /// Has the ability to delay the loading until it has been visible on-screen for a specified duration.
    /// In order to benefit from delayed load, we must be inside a <see cref="ScrollContainer"/>.
    /// </summary>
    public class DelayedLoadWrapper : CompositeDrawable
    {
        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// </summary>
        /// <remarks>If <see cref="timeBeforeLoad"/> is set to 0, the loading process will begin on the next Update call.</remarks>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        /// <param name="timeBeforeLoad">The delay in milliseconds before loading can begin.</param>
        public DelayedLoadWrapper(Drawable content, double timeBeforeLoad = 500)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content), $@"{nameof(DelayedLoadWrapper)} required non-null {nameof(content)}.");
            this.timeBeforeLoad = timeBeforeLoad;

            RelativeSizeAxes = content.RelativeSizeAxes;
            AutoSizeAxes = (content as CompositeDrawable)?.AutoSizeAxes ?? AutoSizeAxes;
        }

        public override double LifetimeStart => Content.LifetimeStart;

        public override double LifetimeEnd => Content.LifetimeEnd;

        internal readonly Drawable Content;

        /// <summary>
        /// The amount of time on-screen in milliseconds before we begin a load of children.
        /// </summary>
        private readonly double timeBeforeLoad;

        private double timeVisible;

        protected bool ShouldLoadContent => timeBeforeLoad == 0 || timeVisible > timeBeforeLoad;

        private Task loadTask;

        protected override void Update()
        {
            // This code can be expensive, so only run if we haven't yet loaded.
            if (!LoadTriggered)
            {
                if (!isIntersecting)
                    timeVisible = 0;
                else
                    timeVisible += Time.Elapsed;
            }

            base.Update();

            if (!LoadTriggered && ShouldLoadContent)
                loadTask = LoadComponentAsync(Content, AddInternal);
        }

        /// <summary>
        /// True if the load task for our content has been started.
        /// Will remain true even after load is completed.
        /// </summary>
        protected bool LoadTriggered => loadTask != null;

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
