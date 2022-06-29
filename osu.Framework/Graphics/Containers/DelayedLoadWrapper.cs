// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Extensions.PolygonExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Layout;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which asynchronously loads specified content.
    /// Has the ability to delay the loading until it has been visible on-screen for a specified duration.
    /// In order to benefit from delayed load, we must be inside a <see cref="ScrollContainer{T}"/>.
    /// </summary>
    public class DelayedLoadWrapper : CompositeDrawable
    {
        [Resolved]
        protected Game Game { get; private set; }

        private readonly Func<Drawable> createFunc;

        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// </summary>
        /// <remarks>If <see cref="timeBeforeLoad"/> is set to 0, the loading process will begin on the next Update call.</remarks>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        /// <param name="timeBeforeLoad">The delay in milliseconds before loading can begin.</param>
        public DelayedLoadWrapper(Drawable content, double timeBeforeLoad = 500)
            : this(timeBeforeLoad)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content), $@"{nameof(DelayedLoadWrapper)} required non-null {nameof(content)}.");
        }

        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// This constructor is preferred due to avoiding construction of the loadable content until a load is actually triggered.
        /// </summary>
        /// <remarks>If <see cref="timeBeforeLoad"/> is set to 0, the loading process will begin on the next Update call.</remarks>
        /// <param name="createFunc">A function which created future content.</param>
        /// <param name="timeBeforeLoad">The delay in milliseconds before loading can begin.</param>
        public DelayedLoadWrapper(Func<Drawable> createFunc, double timeBeforeLoad = 500)
            : this(timeBeforeLoad)
        {
            this.createFunc = createFunc;
        }

        private DelayedLoadWrapper(double timeBeforeLoad)
        {
            this.timeBeforeLoad = timeBeforeLoad;

            AddLayout(optimisingContainerCache);
            AddLayout(isIntersectingCache);
        }

        private Drawable content;

        public Drawable Content
        {
            get => content;
            protected set
            {
                if (content == value)
                    return;

                content = value;

                if (content == null)
                    return;

                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.None;

                RelativeSizeAxes = content.RelativeSizeAxes;
                AutoSizeAxes = (content as CompositeDrawable)?.AutoSizeAxes ?? AutoSizeAxes;
            }
        }

        /// <summary>
        /// The amount of time on-screen in milliseconds before we begin a load of children.
        /// </summary>
        private readonly double timeBeforeLoad;

        private double timeVisible;

        protected virtual bool ShouldLoadContent => timeVisible > timeBeforeLoad;

        private CancellationTokenSource cancellationTokenSource;
        private ScheduledDelegate scheduledAddition;

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
            if (DelayedLoadTriggered || DelayedLoadCompleted)
                throw new InvalidOperationException("Load has already started!");

            Content ??= createFunc();

            DelayedLoadTriggered = true;
            DelayedLoadStarted?.Invoke(Content);

            cancellationTokenSource = new CancellationTokenSource();

            // The callback is run on the game's scheduler since DelayedLoadUnloadWrapper needs to unload when no updates are being received.
            LoadComponentAsync(Content, EndDelayedLoad, scheduler: Game.Scheduler, cancellation: cancellationTokenSource.Token);
        }

        protected virtual void EndDelayedLoad(Drawable content)
        {
            timeVisible = 0;

            // This code is running on the game's scheduler, while this wrapper may have been async disposed, so the addition is scheduled locally to prevent adding to disposed wrappers.
            scheduledAddition = Schedule(() =>
            {
                AddInternal(content);

                DelayedLoadCompleted = true;
                DelayedLoadComplete?.Invoke(content);
            });
        }

        internal override void UnbindAllBindables()
        {
            base.UnbindAllBindables();
            CancelTasks();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            CancelTasks();
        }

        protected virtual void CancelTasks()
        {
            isIntersectingCache.Invalidate();

            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;

            scheduledAddition?.Cancel();
            scheduledAddition = null;
        }

        /// <summary>
        /// Fired when delayed async load has started.
        /// </summary>
        public event Action<Drawable> DelayedLoadStarted;

        /// <summary>
        /// Fired when delayed async load completes. Should be used to perform transitions.
        /// </summary>
        public event Action<Drawable> DelayedLoadComplete;

        /// <summary>
        /// True if the load task for our content has been started.
        /// Will remain true even after load is completed.
        /// </summary>
        protected bool DelayedLoadTriggered;

        /// <summary>
        /// True if the content has been added to the drawable hierarchy.
        /// </summary>
        public bool DelayedLoadCompleted { get; protected set; }

        private readonly LayoutValue optimisingContainerCache = new LayoutValue(Invalidation.Parent);
        private readonly LayoutValue isIntersectingCache = new LayoutValue(Invalidation.All);
        private ScheduledDelegate isIntersectingResetDelegate;

        protected bool IsIntersecting { get; private set; }

        internal IOnScreenOptimisingContainer OptimisingContainer { get; private set; }

        internal IOnScreenOptimisingContainer FindParentOptimisingContainer() => this.FindClosestParent<IOnScreenOptimisingContainer>();

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool result = base.OnInvalidate(invalidation, source);

            // For every invalidation, we schedule a reset of IsIntersecting to the game.
            // This is done since UpdateSubTreeMasking() may not be invoked in the current frame, as a result of presence/masking changes anywhere in our super-tree.
            // It is important that this is scheduled such that it occurs on the NEXT frame, in order to give this wrapper a chance to load its contents.
            // For example, if a parent invalidated this wrapper every frame, IsIntersecting would be false by the time Update() is run and may only become true at the very end of the frame.
            // The scheduled delegate will be cancelled if this wrapper has its UpdateSubTreeMasking() invoked, as more accurate intersections can be computed there instead.
            if (isIntersectingResetDelegate == null)
            {
                isIntersectingResetDelegate = Game?.Scheduler.AddDelayed(wrapper => wrapper.IsIntersecting = false, this, 0);
                result = true;
            }

            return result;
        }

        public override bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds)
        {
            bool result = base.UpdateSubTreeMasking(source, maskingBounds);

            // We can accurately compute intersections - the scheduled reset is no longer required.
            isIntersectingResetDelegate?.Cancel();
            isIntersectingResetDelegate = null;

            if (!isIntersectingCache.IsValid)
            {
                if (!optimisingContainerCache.IsValid)
                {
                    OptimisingContainer = FindParentOptimisingContainer();
                    optimisingContainerCache.Validate();
                }

                // The first condition is an intersection against the hierarchy, including any parents that may be masking this wrapper.
                // It is the same calculation as Drawable.IsMaskedAway, however IsMaskedAway is optimised out for some CompositeDrawables (which this wrapper is).
                // The second condition is an exact intersection against the optimising container, which further optimises rotated AABBs where the wrapper content is not visible.
                IsIntersecting = maskingBounds.IntersectsWith(ScreenSpaceDrawQuad.AABBFloat)
                                 && OptimisingContainer?.ScreenSpaceDrawQuad.Intersects(ScreenSpaceDrawQuad) != false;

                isIntersectingCache.Validate();
            }

            return result;
        }

        /// <summary>
        /// A container which acts as a masking parent for on-screen delayed load optimisations.
        /// </summary>
        internal interface IOnScreenOptimisingContainer : IDrawable
        {
        }
    }
}
