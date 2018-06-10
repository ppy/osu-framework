// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An abstract class that manages dynamically displaying a custom Drawable based on a "source" object.
    /// Useful for replacing Drawables on the fly.
    /// </summary>
    public abstract class UpdateableContainer<T> : Container where T : class
    {
        private Drawable displayedDrawable;
        private readonly Drawable placeholderDrawable;

        /// <summary>
        /// Determines whether the current Drawable should fade out straight away when switching to a new source,
        /// or whether it should wait until the new Drawable has finished loading.
        /// </summary>
        protected virtual bool FadeOutImmediately => false;

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
        protected virtual Drawable CreateDrawable(T item) => null;

        /// <summary>
        /// Override to perform a custom comparison of two source items.  By default an object reference comparison
        /// is used, but it may be desirable to compare based on properties of the items.
        /// </summary>
        /// <returns><c>true</c>, if the items are logically equivalent, <c>false</c> otherwise.</returns>
        protected virtual bool CompareItems(T lhs, T rhs) => lhs == rhs;

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
                
                if (CompareItems(source, value))
                    return;

                source = value;

                if (IsLoaded)
                    updateDrawable();
            }
        }

        protected UpdateableContainer()
        {
            placeholderDrawable = CreatePlaceholder();

            if (placeholderDrawable != null)
            {
                placeholderDrawable.RelativeSizeAxes = Axes.Both;
                AddInternal(placeholderDrawable);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateDrawable();
        }

        private void updateDrawable()
        {
            var newDrawable = CreateDrawable(source);

            if (newDrawable == displayedDrawable)
                return;

            var previousDrawable = displayedDrawable;
            displayedDrawable = newDrawable;

            if (previousDrawable != null && newDrawable == null)
                placeholderDrawable?.FadeInFromZero(300, Easing.OutQuint);
            
            if (newDrawable == null || FadeOutImmediately)
                previousDrawable?.FadeOut(300).Expire();

            if (newDrawable != null)
            {
                newDrawable.OnLoadComplete = d =>
                {
                    if (d != displayedDrawable)
                    {
                        d.Expire();
                        return;
                    }

                    placeholderDrawable?.FadeOut(300);

                    if (!FadeOutImmediately)
                        previousDrawable?.FadeOut(300).Expire();
                    
                    d.FadeInFromZero(300, Easing.OutQuint);
                };
                Add(new DelayedLoadWrapper(newDrawable));
            }
        }
    }
}
