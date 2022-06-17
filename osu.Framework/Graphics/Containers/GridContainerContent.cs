// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A wrapper for the content of a <see cref="GridContainer"/> that provides notifications when elements are changed.
    /// </summary>
    public class GridContainerContent : ObservableArray<ObservableArray<Drawable>>
    {
        private GridContainerContent([NotNull] Drawable[][] drawables)
            : base(new ObservableArray<Drawable>[drawables.Length])
        {
            for (int i = 0; i < drawables.Length; i++)
            {
                if (drawables[i] != null)
                {
                    var observableArray = new ObservableArray<Drawable>(drawables[i]);
                    this[i] = observableArray;
                }
            }
        }

        public static implicit operator GridContainerContent(Drawable[][] drawables)
        {
            if (drawables == null)
                return null;

            return new GridContainerContent(drawables);
        }
    }
}
