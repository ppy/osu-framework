// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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
        /// The currently displayed <see cref="Drawable"/>. Null if no drawable is displayed.
        /// </summary>
        protected Drawable DisplayedDrawable => displayedWrapper?.Content;

        /// <summary>
        /// The <see cref="IEqualityComparer{T}"/> used to compare models to ensure that <see cref="Drawable"/>s are not updated unnecessarily.
        /// </summary>
        protected readonly IEqualityComparer<T> Comparer;

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

        /// <summary>
        /// The wrapper which has the current displayed content.
        /// </summary>
        private DelayedLoadWrapper displayedWrapper;

        /// <summary>
        /// The wrapper which is currently loading, or has finished loading (i.e <see cref="displayedWrapper"/>).
        /// </summary>
        private DelayedLoadWrapper currentWrapper;

        /// <summary>
        /// The wrapper for the placeholder.
        /// </summary>
        private DelayedLoadWrapper placeholderWrapper;

        private bool placeholderVisible = true;

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
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            placeholderWrapper = createWrapper(CreatePlaceholder, 0);

            if (placeholderWrapper != null)
                AddInternal(placeholderWrapper);

            updateDrawable();
        }

        private void updateDrawable()
        {
            if (model == null || TransformImmediately)
                loadDrawable(null);

            if (model != null)
                loadDrawable(() => CreateDrawable(model));
        }

        private void loadDrawable(Func<Drawable> createDrawableFunc)
        {
            // Remove the previous wrapper if the inner drawable hasn't finished loading.
            if (currentWrapper?.DelayedLoadCompleted == false)
            {
                RemoveInternal(currentWrapper);
                DisposeChildAsync(currentWrapper);
            }

            currentWrapper = null;

            if (createDrawableFunc != null)
            {
                AddInternal(currentWrapper = createWrapper(createDrawableFunc, LoadDelay));
                currentWrapper.DelayedLoadComplete += _ => finishLoad(currentWrapper);
            }
            else
                finishLoad(null);
        }

        /// <summary>
        /// Invoked when a <see cref="DelayedLoadWrapper"/> has finished loading its contents.
        /// May be invoked multiple times for each <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        /// <param name="wrapper">The <see cref="DelayedLoadWrapper"/>. This is never <see cref="placeholderWrapper"/>.</param>
        private void finishLoad(DelayedLoadWrapper wrapper)
        {
            var lastWrapper = displayedWrapper;

            // If the wrapper hasn't changed then this invocation must be a result of a reload (e.g. DelayedLoadUnloadWrapper)
            // In this case, we do not want to transform/expire the wrapper
            if (lastWrapper == wrapper)
                return;

            ApplyHideTransforms(currentWrapper);
            currentWrapper?.FinishTransforms();

            if (wrapper == null)
                showPlaceholder();
            else
                hidePlaceholder();

            var hideTransforms = ApplyHideTransforms(lastWrapper);
            var showTransforms = ApplyShowTransforms(wrapper);

            // Expire the last wrapper after the front-most transform has completed (the last wrapper is assumed to be invisible by that point)
            (showTransforms ?? hideTransforms)?.OnComplete(_ => lastWrapper?.Expire());

            displayedWrapper = wrapper;
        }

        /// <summary>
        /// Shows the placeholder.
        /// </summary>
        private void showPlaceholder()
        {
            if (placeholderVisible)
                return;

            placeholderVisible = true;

            if (placeholderWrapper?.DelayedLoadCompleted == true)
                ApplyShowTransforms(placeholderWrapper);
        }

        /// <summary>
        /// Hides the placeholder.
        /// </summary>
        private void hidePlaceholder()
        {
            if (!placeholderVisible)
                return;

            placeholderVisible = false;

            if (placeholderWrapper?.DelayedLoadCompleted == true)
                ApplyHideTransforms(placeholderWrapper);
        }

        /// <summary>
        /// Creates a <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        /// <param name="createContentFunc">A function that creates the wrapped <see cref="Drawable"/>.</param>
        /// <param name="timeBeforeLoad">The time before loading should begin.</param>
        /// <returns>A <see cref="DelayedLoadWrapper"/> or null if <see cref="createContentFunc"/> returns null.</returns>
        private DelayedLoadWrapper createWrapper([NotNull] Func<Drawable> createContentFunc, double timeBeforeLoad)
        {
            var content = createContentFunc();

            if (content == null)
                return null;

            bool first = true;

            return CreateDelayedLoadWrapper(() =>
            {
                if (first)
                {
                    first = false;
                    return content;
                }

                return createContentFunc();
            }, timeBeforeLoad);
        }

        /// <summary>
        /// Determines whether <see cref="ApplyHideTransforms"/> should be applied immediately to the current <see cref="Drawable"/> when switching to a new model,
        /// or whether it should wait until the new <see cref="Drawable"/> has finished loading.
        /// </summary>
        protected virtual bool TransformImmediately => false;

        /// <summary>
        /// The default time in milliseconds for transforms applied through <see cref="ApplyHideTransforms"/> and <see cref="ApplyShowTransforms"/>.
        /// </summary>
        protected virtual double TransformDuration => 1000;

        /// <summary>
        /// The delay in milliseconds before <see cref="Drawable"/>s will begin loading.
        /// </summary>
        protected virtual double LoadDelay => 0;

        /// <summary>
        /// Allows subclasses to customise the <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        protected virtual DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad) =>
            new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);

        /// <summary>
        /// Override to instantiate a custom <see cref="Drawable"/> based on the passed model.
        /// May be null to indicate that the model has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="model">The model that the <see cref="Drawable"/> should represent.</param>
        protected abstract Drawable CreateDrawable(T model);

        protected virtual Drawable CreatePlaceholder() => CreateDrawable(null);

        /// <summary>
        /// Hides a drawable.
        /// </summary>
        /// <param name="drawable">The drawable that is to be hidden.</param>
        /// <returns>The transform sequence.</returns>
        protected virtual TransformSequence<Drawable> ApplyHideTransforms(Drawable drawable)
            => drawable?.Delay(TransformDuration).FadeOut(TransformDuration, Easing.OutQuint);

        /// <summary>
        /// Shows a drawable.
        /// </summary>
        /// <param name="drawable">The drawable that is to be shown.</param>
        /// <returns>The transform sequence.</returns>
        protected virtual TransformSequence<Drawable> ApplyShowTransforms(Drawable drawable)
            => drawable?.FadeIn(TransformDuration, Easing.OutQuint);
    }
}
