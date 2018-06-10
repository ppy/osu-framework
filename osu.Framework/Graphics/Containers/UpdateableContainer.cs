// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    public abstract class UpdateableContainer<T> : Container
        where T : class
    {
        private Drawable displayedDrawable;
        private readonly Drawable placeholderDrawable;

        protected virtual bool FadeOutImmediately => false;

        protected virtual Drawable CreatePlaceholder() => null;

        protected virtual Drawable CreateDrawable(T item) => null;

        protected virtual bool CompareItems(T lhs, T rhs) => lhs == rhs;

        private T source;
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
