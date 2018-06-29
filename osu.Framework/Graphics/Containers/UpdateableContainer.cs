// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Manages dynamically displaying a custom Drawable based on a "source" object.
    /// Useful for replacing Drawables on the fly.
    /// </summary>
    public abstract class UpdateableContainer<T> : Container where T : class
    {
        /// <summary>
        /// The placeholder Drawable created when the container was instantiated.
        /// </summary>
        public readonly Drawable PlaceholderDrawable;

        /// <summary>
        /// The currently displayed Drawable. Null if no Drawable or the placeholder is displayed.
        /// </summary>
        public Drawable DisplayedDrawable { get; private set; }

        /// <summary>
        /// The Drawable that will be presented next. Null if there is no pending drawable.
        /// </summary>
        public Drawable NextDrawable { get; private set; }

        /// <summary>
        /// Determines whether the current Drawable should fade out straight away when switching to a new source,
        /// or whether it should wait until the new Drawable has finished loading.
        /// </summary>
        protected virtual bool FadeOutImmediately => false;

        /// <summary>
        /// The time in milliseconds that Drawables will fade in and out.
        /// </summary>
        protected virtual double FadeDuration => 300;

        /// <summary>
        /// The delay in milliseconds before Drawables will begin loading.
        /// </summary>
        protected virtual double LoadDelay => 0;

        /// <summary>
        /// The IComparer used to compare source items to ensure that Drawables are not updated unnecessarily.
        /// </summary>
        public readonly IComparer<T> Comparer;

        /// <summary>
        /// Override to instantiate a placeholder Drawable that will be displayed when no source is set.
        /// May be null to indicate no placeholder.
        /// </summary>
        protected virtual Drawable CreatePlaceholder() => null;

        /// <summary>
        /// Override to instantiate a custom Drawable based on the passed source item.
        /// May be null to indicate that the source item has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="item">The source item that the Drawable should represent.</param>
        protected abstract Drawable CreateDrawable(T item);

        private T source;

        /// <summary>
        /// Gets or sets the source item, potentially triggering the current Drawable to update.
        /// Subclasses should expose this via a nicer property name to better represent the data being set.
        /// </summary>
        protected T Source
        {
            get => source;
            set
            {
                if (source == null && value == null)
                    return;

                if (Comparer.Compare(source, value) == 0)
                    return;

                source = value;

                if (IsLoaded)
                    updateDrawable();
            }
        }

        /// <summary>
        /// Constructs a new <see cref="UpdateableContainer{T}"/> with the default <typeparamref name="T"/> comparer.
        /// </summary>
        protected UpdateableContainer()
            : this(Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="UpdateableContainer{T}"/> with a custom comparison function.
        /// </summary>
        /// <param name="comparer">The comparison function.</param>
        protected UpdateableContainer(Func<T, T, int> comparer)
            : this(new ComparisonComparer<T>(comparer))
        {
        }

        /// <summary>
        /// Constructs a new <see cref="UpdateableContainer{T}"/> with a custom <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The comparer to use.</param>
        protected UpdateableContainer(IComparer<T> comparer)
        {
            Comparer = comparer;
            PlaceholderDrawable = CreatePlaceholder();

            if (PlaceholderDrawable != null)
            {
                PlaceholderDrawable.RelativeSizeAxes = Axes.Both;
                AddInternal(PlaceholderDrawable);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateDrawable();
        }

        protected virtual void ShowDrawable(Drawable d) => d?.FadeInFromZero(FadeDuration, Easing.OutQuint);

        protected virtual void HideDrawable(Drawable d) => d?.FadeOut(FadeDuration);

        private void updateDrawable()
        {
            var newDrawable = CreateDrawable(source);

            if (newDrawable == DisplayedDrawable)
                return;

            NextDrawable = newDrawable;

            if (FadeOutImmediately)
            {
                HideDrawable(DisplayedDrawable);
                DisplayedDrawable?.Expire();
            }

            if (newDrawable == null || FadeOutImmediately)
            {
                ShowDrawable(PlaceholderDrawable);
                DisplayedDrawable = null;
                return;
            }

            newDrawable.OnLoadComplete = d =>
            {
                if (d != NextDrawable)
                {
                    d.Expire();
                    return;
                }

                HideDrawable(PlaceholderDrawable);

                if (!FadeOutImmediately)
                {
                    HideDrawable(DisplayedDrawable);
                    DisplayedDrawable?.Expire();
                }

                ShowDrawable(d);

                DisplayedDrawable = d;
                NextDrawable = null;
            };

            Add(new DelayedLoadWrapper(newDrawable, LoadDelay));
        }
    }
}
