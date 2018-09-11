// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Caching;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Threading;

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

        public virtual Drawable Content { get; protected set; }

        /// <summary>
        /// The amount of time on-screen in milliseconds before we begin a load of children.
        /// </summary>
        private readonly double timeBeforeLoad;

        private double timeVisible;

        protected bool ShouldLoadContent => timeBeforeLoad == 0 || timeVisible > timeBeforeLoad;

        private Task loadTask;

        protected override void Update()
        {
            base.Update();

            // This code can be expensive, so only run if we haven't yet loaded.
            if (DelayedLoadCompleted || DelayedLoadTriggered) return;

            if (!IsIntersecting)
                timeVisible = 0;
            else
                timeVisible += Time.Elapsed;

            if (ShouldLoadContent)
                BeginDelayedLoad();
        }

        protected void BeginDelayedLoad()
        {
            if (loadTask != null) throw new InvalidOperationException("Load is already started!");
            loadTask = LoadComponentAsync(Content, EndDelayedLoad);
        }

        protected virtual void EndDelayedLoad(Drawable content)
        {
            timeVisible = 0;
            loadTask = null;
            AddInternal(content);
        }

        protected override void LoadComplete()
        {
            if (OptimisingContainer == null)
            {
                CompositeDrawable cursor = this;
                while (OptimisingContainer == null && (cursor = cursor.Parent) != null)
                    OptimisingContainer = cursor as IOnScreenOptimisingContainer;

                if (OptimisingContainer != null)
                {
                    PropagateInvalidation(InvalidateIsIntersecting());
                    OptimisingContainer.OnScreenSpaceDrawQuadInvalidated = () => PropagateInvalidation(InvalidateIsIntersecting());
                }
            }

            base.LoadComplete();
        }

        /// <summary>
        /// True if the load task for our content has been started.
        /// Will remain true even after load is completed.
        /// </summary>
        protected bool DelayedLoadTriggered => loadTask != null;

        public bool DelayedLoadCompleted => InternalChildren.Count > 0;

        private Cached<bool> isIntersectingBacking = new Cached<bool> { Name = $"{nameof(DelayedLoadWrapper)}.{nameof(IsIntersecting)}" };
        protected bool IsIntersecting => isIntersectingBacking.Compute(checkScrollIntersection);

        internal IOnScreenOptimisingContainer OptimisingContainer { get; private set; }

        private bool checkScrollIntersection() =>
            OptimisingContainer?.ScreenSpaceDrawQuad.Intersects(ScreenSpaceDrawQuad) ?? true;

        [MustUseReturnValue]
        protected Invalidation InvalidateIsIntersecting()
        {
            if (!isIntersectingBacking.Invalidate()) return Invalidation.None;
            // no dependency (delayed load is always performed on Update())
            return Invalidation.None;
        }

        protected override void PropagateInvalidation(Invalidation invalidation)
        {
            if ((invalidation & Invalidation.ScreenSpaceDrawQuad) != 0)
                invalidation |= InvalidateIsIntersecting();
            base.PropagateInvalidation(invalidation);
        }

        /// <summary>
        /// A container which acts as a masking parent for on-screen delayed load optimisations.
        /// </summary>
        internal interface IOnScreenOptimisingContainer
        {
            Quad ScreenSpaceDrawQuad { get; }

            Action OnScreenSpaceDrawQuadInvalidated { set; }

            /// <summary>
            /// Schedule a repeating action from a child to perform checks even when the child is potentially masked.
            /// Repeats every frame until manually cancelled.
            /// </summary>
            /// <param name="action">The action to perform.</param>
            /// <returns>The scheduled delegate.</returns>
            ScheduledDelegate ScheduleCheckAction(Action action);
        }
    }
}
