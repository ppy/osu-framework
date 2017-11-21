// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which asynchronously loads specified content.
    /// Has the ability to delay the loading until it has been visible on-screen for a specified duration.
    /// In order to benefit from delayed load, we must be inside a <see cref="ScrollContainer"/>.
    /// </summary>
    public class LoadWrapper : Container
    {
        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/>.
        /// The loading process can be delayed by changing the <see cref="TimeBeforeLoad"/> field.
        /// </summary>
        /// <remarks>If <see cref="TimeBeforeLoad"/> remains unchanged (at 0), the loading process will not be delayed.</remarks>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        public LoadWrapper(Drawable content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), $@"{nameof(LoadWrapper)} required non-null {nameof(content)}.");

            this.content = content;

            RelativeSizeAxes = content.RelativeSizeAxes;
            AutoSizeAxes = (content as CompositeDrawable)?.AutoSizeAxes ?? AutoSizeAxes;
        }

        public override double LifetimeStart => content.LifetimeStart;

        public override double LifetimeEnd => content.LifetimeEnd;

        protected sealed override Container<Drawable> Content => base.Content;

        private readonly Drawable content;

        /// <summary>
        /// The amount of time on-screen in milliseconds before we begin a load of children.
        /// </summary>
        public double TimeBeforeLoad = 0;

        private double timeVisible;

        // If TimeBeforeLoad is zero (unchanged from default), loading will start immediately.
        protected bool ShouldLoadContent => TimeBeforeLoad != 0 ? timeVisible > TimeBeforeLoad : true;

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
                loadContentAsync();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (ShouldLoadContent)
                loadContentAsync();
        }

        private Task loadTask;

        private void loadContentAsync()
        {
            loadTask = LoadComponentAsync(content, AddInternal);
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
