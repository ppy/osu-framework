// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    public abstract class ModelBackedDrawable<T> : CompositeDrawable
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

                Scheduler.AddOnce(updateDrawable);
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
            Scheduler.AddOnce(updateDrawable);
        }

        private void updateDrawable()
        {
            if (TransformImmediately)
            {
                // If loading to a new model and we've requested to transform immediately, load a null model to allow such transforms to occur
                loadDrawable(null);
            }

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

            currentWrapper = createWrapper(createDrawableFunc, LoadDelay);

            if (currentWrapper == null)
            {
                OnLoadStarted();
                finishLoad(currentWrapper);
                OnLoadFinished();
            }
            else
            {
                AddInternal(currentWrapper);
                currentWrapper.DelayedLoadStarted += _ => OnLoadStarted();
                currentWrapper.DelayedLoadComplete += _ =>
                {
                    finishLoad(currentWrapper);
                    OnLoadFinished();
                };
            }
        }

        /// <summary>
        /// Invoked when a <see cref="DelayedLoadWrapper"/> has finished loading its contents.
        /// May be invoked multiple times for each <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        /// <param name="wrapper">The <see cref="DelayedLoadWrapper"/>.</param>
        private void finishLoad(DelayedLoadWrapper wrapper)
        {
            // Make the wrapper initially hidden.
            ApplyHideTransforms(wrapper);
            wrapper?.FinishTransforms();

            var showTransforms = ApplyShowTransforms(wrapper);

            // If the wrapper hasn't changed then this invocation must be a result of a reload (e.g. DelayedLoadUnloadWrapper)
            // In that case, we do not want to apply hide transforms and expire the last wrapper.
            if (displayedWrapper != null && displayedWrapper != wrapper)
            {
                var lastWrapper = displayedWrapper;

                // If the new wrapper is non-null, we need to wait for the show transformation to complete before hiding the old wrapper,
                // otherwise, we can hide the old wrapper instantaneously and leave a blank display
                var hideTransforms = wrapper == null
                    ? ApplyHideTransforms(lastWrapper)
                    : ((Drawable)lastWrapper)?.Delay(TransformDuration)?.Append(ApplyHideTransforms);

                // Expire the last wrapper after the front-most transform has completed (the last wrapper is assumed to be invisible by that point)
                (showTransforms ?? hideTransforms)?.OnComplete(_ => lastWrapper?.Expire());
            }

            displayedWrapper = wrapper;
        }

        /// <summary>
        /// Creates a <see cref="DelayedLoadWrapper"/> which supports reloading.
        /// </summary>
        /// <param name="createContentFunc">A function that creates the wrapped <see cref="Drawable"/>.</param>
        /// <param name="timeBeforeLoad">The time before loading should begin.</param>
        /// <returns>A <see cref="DelayedLoadWrapper"/> or null if <paramref name="createContentFunc"/> returns null.</returns>
        private DelayedLoadWrapper createWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
        {
            var content = createContentFunc?.Invoke();

            if (content == null)
                return null;

            return CreateDelayedLoadWrapper(() =>
            {
                try
                {
                    // optimisation to use already constructed object (used above for null check).
                    return content ?? createContentFunc();
                }
                finally
                {
                    // consume initial object if not already.
                    content = null;
                }
            }, timeBeforeLoad);
        }

        /// <summary>
        /// Invoked when the <see cref="Drawable"/> representation of a model begins loading.
        /// </summary>
        protected virtual void OnLoadStarted()
        {
        }

        /// <summary>
        /// Invoked when the <see cref="Drawable"/> representation of a model has finished loading.
        /// </summary>
        protected virtual void OnLoadFinished()
        {
        }

        /// <summary>
        /// Determines whether <see cref="ApplyHideTransforms"/> should be invoked immediately on the currently-displayed drawable when switching to a new model.
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
        [NotNull]
        protected virtual DelayedLoadWrapper CreateDelayedLoadWrapper([NotNull] Func<Drawable> createContentFunc, double timeBeforeLoad) =>
            new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);

        /// <summary>
        /// Creates a custom <see cref="Drawable"/> to display a model.
        /// </summary>
        /// <param name="model">The model that the <see cref="Drawable"/> should represent.</param>
        /// <returns>A <see cref="Drawable"/> that represents <paramref name="model"/>, or null if no <see cref="Drawable"/> should be displayed.</returns>
        [CanBeNull]
        protected abstract Drawable CreateDrawable([CanBeNull] T model);

        /// <summary>
        /// Hides a drawable.
        /// </summary>
        /// <param name="drawable">The drawable that is to be hidden.</param>
        /// <returns>The transform sequence.</returns>
        protected virtual TransformSequence<Drawable> ApplyHideTransforms([CanBeNull] Drawable drawable)
            => drawable?.FadeOut(TransformDuration, Easing.OutQuint);

        /// <summary>
        /// Shows a drawable.
        /// </summary>
        /// <param name="drawable">The drawable that is to be shown.</param>
        /// <returns>The transform sequence.</returns>
        protected virtual TransformSequence<Drawable> ApplyShowTransforms([CanBeNull] Drawable drawable)
            => drawable?.FadeIn(TransformDuration, Easing.OutQuint);
    }
}
