// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Manages dynamically displaying a custom Drawable based on a model object.
    /// Useful for replacing Drawables on the fly.
    /// </summary>
    public abstract class ModelBackedDrawable<T> : CompositeDrawable where T : class
    {
        /// <summary>
        /// The placeholder Drawable created when the container was instantiated.
        /// </summary>
        public readonly Drawable PlaceholderDrawable;

        /// <summary>
        /// The currently displayed Drawable. Null if no drawable is displayed (note that the placeholder may still be displayed in this state).
        /// </summary>
        public Drawable DisplayedDrawable { get; private set; }

        /// <summary>
        /// The Drawable that will be presented next. Null if there is no pending drawable.
        /// </summary>
        public Drawable NextDrawable { get; private set; }

        /// <summary>
        /// Determines whether the current Drawable should fade out straight away when switching to a new model,
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
        /// True if the most recently added DelayedLoadWrapper has begun loading.
        /// </summary>
        public bool LoadTriggered => lastDelayedLoadWrapper?.LoadTriggered ?? false;

        /// <summary>
        /// The IComparer used to compare models to ensure that Drawables are not updated unnecessarily.
        /// </summary>
        public readonly IComparer<T> Comparer;

        /// <summary>
        /// Override to instantiate a placeholder Drawable that will be displayed when no model is set.
        /// May be null to indicate no placeholder.
        /// </summary>
        protected virtual Drawable CreatePlaceholder() => null;

        /// <summary>
        /// Override to instantiate a custom Drawable based on the passed model.
        /// May be null to indicate that the model has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="model">The model that the Drawable should represent.</param>
        protected abstract Drawable CreateDrawable(T model);

        private T model;

        /// <summary>
        /// Gets or sets the model, potentially triggering the current Drawable to update.
        /// Subclasses should expose this via a nicer property name to better represent the data being set.
        /// </summary>
        protected T Model
        {
            get => model;
            set
            {
                if (model == null && value == null)
                    return;

                if (Comparer.Compare(model, value) == 0)
                    return;

                model = value;

                if (IsLoaded)
                    updateDrawable();
            }
        }

        private UpdateDelayedLoadWrapper lastDelayedLoadWrapper;

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with the default <typeparamref name="T"/> comparer.
        /// </summary>
        protected ModelBackedDrawable()
            : this(Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom comparison function.
        /// </summary>
        /// <param name="comparer">The comparison function.</param>
        protected ModelBackedDrawable(Func<T, T, int> comparer)
            : this(new ComparisonComparer<T>(comparer))
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The comparer to use.</param>
        protected ModelBackedDrawable(IComparer<T> comparer)
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
            var newDrawable = CreateDrawable(model);

            if (newDrawable == DisplayedDrawable)
                return;

            NextDrawable = newDrawable;

            if (newDrawable == null || FadeOutImmediately)
            {
                HideDrawable(DisplayedDrawable);
                DisplayedDrawable?.Expire();
                DisplayedDrawable = null;
            }

            if (newDrawable == null)
            {
                ShowDrawable(PlaceholderDrawable);
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
                lastDelayedLoadWrapper = null;
            };

            AddInternal(lastDelayedLoadWrapper = new UpdateDelayedLoadWrapper(newDrawable, LoadDelay));
        }

        private class UpdateDelayedLoadWrapper : DelayedLoadWrapper
        {
            internal new bool LoadTriggered => base.LoadTriggered;

            public UpdateDelayedLoadWrapper(Drawable content, double timeBeforeLoad = 500)
                : base(content, timeBeforeLoad)
            {
            }
        }
    }
}
