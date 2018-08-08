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
        /// The currently displayed <see cref="Drawable"/>. Null if no drawable is displayed.
        /// </summary>
        protected Drawable DisplayedDrawable { get; private set; }

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

            DisplayedDrawable = CreateDrawable(null);
            AddInternal(CreateDelayedLoadWrapper(DisplayedDrawable, 0));
        }

        private void replaceDrawable(Drawable source, Drawable target, bool placeholder = false)
        {
            // we need to make sure we definitely get a transform so that we can fire off OnComplete
            var transform = ReplaceDrawable(source, target) ?? (source ?? target)?.DelayUntilTransformsFinished();
            transform?.OnComplete(d =>
            {
                if (!placeholder)
                {
                    if (target != nextDrawable)
                    {
                        target?.Expire();
                        return;
                    }
                    nextDrawable = null;
                }
                DisplayedDrawable = target;
                source?.Expire();
            });
        }

        private void updateDrawable()
        {
            var newDrawable = CreateDrawable(model);

            nextDrawable = newDrawable;

            if (newDrawable == null)
            {
                replaceDrawable(DisplayedDrawable, null);
                return;
            }

            if (FadeOutImmediately)
            {
                var placeholder = CreateDrawable(null);
                AddInternal(placeholder);
                replaceDrawable(DisplayedDrawable, placeholder, true);
            }

            newDrawable.OnLoadComplete = loadedDrawable =>
            {
                if (loadedDrawable != nextDrawable)
                {
                    loadedDrawable.Expire();
                    return;
                }
                replaceDrawable(DisplayedDrawable, loadedDrawable);
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
        protected virtual double FadeDuration => 1000;

        /// <summary>
        /// The delay in milliseconds before <see cref="Drawable"/>s will begin loading.
        /// </summary>
        protected virtual double LoadDelay => 0;

        /// <summary>
        /// Allows subclasses to customise the <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        protected virtual DelayedLoadWrapper CreateDelayedLoadWrapper(Drawable content, double timeBeforeLoad) =>
            new DelayedLoadWrapper(content, timeBeforeLoad);

        /// <summary>
        /// Override to instantiate a custom <see cref="Drawable"/> based on the passed model.
        /// May be null to indicate that the model has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="model">The model that the <see cref="Drawable"/> should represent.</param>
        protected abstract Drawable CreateDrawable(T model);

        /// <summary>
        /// Returns a <see cref="TransformSequence{Drawable}"/> that replaces the given <see cref="Drawable"/>s.
        /// Default functionality is to fade in the target from zero, or if it is null, to fade out the source.
        /// </summary>
        /// <returns>The drawable.</returns>
        /// <param name="source">The <see cref="Drawable"/> to be replaced.</param>
        /// <param name="target">The <see cref="Drawable"/> we are replacing with.</param>
        protected virtual TransformSequence<Drawable> ReplaceDrawable(Drawable source, Drawable target) =>
            target?.FadeInFromZero(FadeDuration, Easing.OutQuint) ?? source?.FadeOut(FadeDuration, Easing.OutQuint);
    }
}
