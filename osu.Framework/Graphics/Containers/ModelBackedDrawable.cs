// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Manages dynamically displaying a custom <see cref="Drawable"/> based on a model object.
    /// Useful for replacing <see cref="Drawable"/>s on the fly.
    /// </summary>
    public abstract class ModelBackedDrawable<T> : CompositeDrawable where T : class
    {
        /// <summary>
        /// The currently displayed <see cref="Drawable"/>. Null if no drawable is displayed (note that the placeholder may still be displayed in this state).
        /// </summary>
        protected Drawable DisplayedDrawable { get; private set; }

        /// <summary>
        /// The <see cref="IEqualityComparer{T}"/> used to compare models to ensure that <see cref="Drawable"/>s are not updated unnecessarily.
        /// </summary>
        protected readonly IEqualityComparer<T> Comparer;

        /// <summary>
        /// True if a placeholder exists and is present.
        /// </summary>
        protected bool IsShowingPlaceholder => placeholderDrawable?.IsPresent ?? false;

        private T model;

        /// <summary>
        /// Gets or sets the model, potentially triggering the current <see cref="Drawable"/> to update.
        /// Subclasses should expose this via a nicer property name to better represent the data being set.
        /// </summary>
        protected T Model
        {
            get => model;
            set
            {
                if (model == null && value == null)
                    return;

                if (Comparer.Equals(model, value))
                    return;

                model = value;

                if (IsLoaded)
                    updateDrawable();
            }
        }

        private readonly Drawable placeholderDrawable;
        private Drawable nextDrawable;

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with the default <typeparamref name="T"/> equality comparer.
        /// </summary>
        protected ModelBackedDrawable()
            : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom equality function.
        /// </summary>
        /// <param name="func">The equality function.</param>
        protected ModelBackedDrawable(Func<T, T, bool> func)
            : this(new FuncEqualityComparer<T>(func))
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The comparer to use.</param>
        protected ModelBackedDrawable(IEqualityComparer<T> comparer)
        {
            Comparer = comparer;
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

        private void replaceDrawable(Drawable source, Drawable target)
        {
            if (source == null && target == null)
                return;

            TransformSequence<Drawable> showTransform = null, hideTransform = null;

            var showingTarget = target ?? placeholderDrawable;
            if (showingTarget != null)
                showTransform = ShowDrawable(showingTarget);

            var hidingSource = source ?? placeholderDrawable;
            if (hidingSource != null)
                hideTransform = HideDrawable(hidingSource);

            (showTransform ?? hideTransform)?.OnComplete((d) => source?.Expire());
        }

        private void updateDrawable()
        {
            var newDrawable = CreateDrawable(model);

            if (newDrawable == DisplayedDrawable)
                return;

            nextDrawable = newDrawable;

            if (newDrawable == null || FadeOutImmediately)
            {
                replaceDrawable(DisplayedDrawable, null);
                DisplayedDrawable = null;
            }

            if (newDrawable == null)
                return;

            newDrawable.OnLoadComplete = d =>
            {
                if (d != nextDrawable)
                {
                    d.Expire();
                    return;
                }

                replaceDrawable(DisplayedDrawable, d);

                DisplayedDrawable = d;
                nextDrawable = null;
            };

            AddInternal(CreateDelayedLoadWrapper(newDrawable, LoadDelay));
        }

        /// <summary>
        /// Determines whether the current <see cref="Drawable"/> should fade out straight away when switching to a new model,
        /// or whether it should wait until the new <see cref="Drawable"/> has finished loading.
        /// </summary>
        protected virtual bool FadeOutImmediately => false;

        /// <summary>
        /// The time in milliseconds that <see cref="Drawable"/>s will fade in and out.
        /// </summary>
        protected virtual double FadeDuration => 300;

        /// <summary>
        /// The delay in milliseconds before <see cref="Drawable"/>s will begin loading.
        /// </summary>
        protected virtual double LoadDelay => 0;

        /// <summary>
        /// Hides the specified <see cref="Drawable"/>.
        /// </summary>
        /// <param name="d">The <see cref="Drawable"/> that will be hidden.</param>
        /// <returns>The <see cref="TransformSequence{T}"/> for chaining.</returns>
        protected virtual TransformSequence<Drawable> HideDrawable(Drawable d) => d.FadeOut(FadeDuration, Easing.OutQuint);

        /// <summary>
        /// Shows the specified <see cref="Drawable"/>.
        /// </summary>
        /// <param name="d">The <see cref="Drawable"/> that will be shown.</param>
        /// <returns>The <see cref="TransformSequence{T}"/> for chaining.</returns>
        protected virtual TransformSequence<Drawable> ShowDrawable(Drawable d) => d.FadeInFromZero(FadeDuration, Easing.OutQuint);

        /// <summary>
        /// Allows subclasses to customise the <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        protected virtual DelayedLoadWrapper CreateDelayedLoadWrapper(Drawable content, double timeBeforeLoad) =>
            new DelayedLoadWrapper(content, timeBeforeLoad);

        /// <summary>
        /// Override to instantiate a placeholder <see cref="Drawable"/> that will be displayed when no model is set.
        /// May be null to indicate no placeholder.
        /// </summary>
        protected virtual Drawable CreatePlaceholder() => null;

        /// <summary>
        /// Override to instantiate a custom <see cref="Drawable"/> based on the passed model.
        /// May be null to indicate that the model has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="model">The model that the <see cref="Drawable"/> should represent.</param>
        protected abstract Drawable CreateDrawable(T model);
    }
}
