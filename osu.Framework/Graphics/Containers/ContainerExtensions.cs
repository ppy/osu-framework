// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Holds extension methods for <see cref="Container{T}"/>.
    /// </summary>
    public static class ContainerExtensions
    {
        /// <summary>
        /// Wraps the given <paramref name="drawable"/> with the given <paramref name="container"/>
        /// such that the <paramref name="container"/> can be used instead of the <paramref name="drawable"/>
        /// without affecting the layout. The <paramref name="container"/> must not contain any children before wrapping.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="container"/>.</typeparam>
        /// <typeparam name="U">The type of the children of <paramref name="container"/>.</typeparam>
        /// <param name="container">The <paramref name="container"/> that should wrap the given <paramref name="drawable"/>.</param>
        /// <param name="drawable">The <paramref name="drawable"/> that should be wrapped by the given <paramref name="container"/>.</param>
        /// <returns>The given <paramref name="container"/>.</returns>
        public static T Wrap<T, U>(this T container, U drawable)
            where T : Container<U>
            where U : Drawable
        {
            if (container.Children.Count != 0)
                throw new InvalidOperationException($"You may not wrap a {nameof(Container<U>)} that has children.");

            container.RelativeSizeAxes = drawable.RelativeSizeAxes;
            container.AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes;
            container.Anchor = drawable.Anchor;
            container.Origin = drawable.Origin;
            container.Position = drawable.Position;
            container.Rotation = drawable.Rotation;

            drawable.Position = Vector2.Zero;
            drawable.Rotation = 0;
            drawable.Anchor = Anchor.TopLeft;
            drawable.Origin = Anchor.TopLeft;

            // For anchor/origin positioning to be preserved correctly,
            // relatively sized axes must be lifted to the wrapping container.
            if ((container.RelativeSizeAxes & Axes.X) > 0)
            {
                container.Width = drawable.Width;
                drawable.Width = 1;
            }

            if ((container.RelativeSizeAxes & Axes.Y) > 0)
            {
                container.Height = drawable.Height;
                drawable.Height = 1;
            }

            container.Add(drawable);

            return container;
        }
    }
}
