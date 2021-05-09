// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using System.Collections.Generic;
using osu.Framework.Extensions.EnumExtensions;

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
        /// <typeparam name="TContainer">The type of the <paramref name="container"/>.</typeparam>
        /// <typeparam name="TChild">The type of the children of <paramref name="container"/>.</typeparam>
        /// <param name="container">The <paramref name="container"/> that should wrap the given <paramref name="drawable"/>.</param>
        /// <param name="drawable">The <paramref name="drawable"/> that should be wrapped by the given <paramref name="container"/>.</param>
        /// <returns>The given <paramref name="container"/>.</returns>
        public static TContainer Wrap<TContainer, TChild>(this TContainer container, TChild drawable)
            where TContainer : Container<TChild>
            where TChild : Drawable
        {
            if (container.Children.Count != 0)
                throw new InvalidOperationException($"You may not wrap a {nameof(Container<TChild>)} that has children.");

            container.RelativeSizeAxes = drawable.RelativeSizeAxes;
            container.AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes;
            container.Anchor = drawable.Anchor;
            container.Origin = drawable.Origin;
            container.Position = drawable.Position;
            container.Rotation = drawable.Rotation;

            drawable.Position = Vector2.Zero;
            drawable.Rotation = 0;

            // For anchor/origin positioning to be preserved correctly,
            // relatively sized axes must be lifted to the wrapping container.
            if (container.RelativeSizeAxes.HasFlagFast(Axes.X))
            {
                container.Width = drawable.Width;
                drawable.Width = 1;
            }

            if (container.RelativeSizeAxes.HasFlagFast(Axes.Y))
            {
                container.Height = drawable.Height;
                drawable.Height = 1;
            }

            container.Add(drawable);

            return container;
        }

        /// <summary>
        /// Set a specified <paramref name="child"/> on <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TChild">The type of children contained by <paramref name="container"/>.</typeparam>
        /// <param name="container">The <paramref name="container"/> that will have a child set.</param>
        /// <param name="child">The <paramref name="child"/> that should be set to the <paramref name="container"/>.</param>
        /// <returns>The given <paramref name="container"/>.</returns>
        public static TContainer WithChild<TContainer, TChild>(this TContainer container, TChild child)
            where TContainer : IContainerCollection<TChild>
            where TChild : Drawable
        {
            container.Child = child;

            return container;
        }

        /// <summary>
        /// Set specified <paramref name="children"/> on <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TChild">The type of children contained by <paramref name="container"/>.</typeparam>
        /// <param name="container">The <paramref name="container"/> that will have children set.</param>
        /// <param name="children">The <paramref name="children"/> that should be set to the <paramref name="container"/>.</param>
        /// <returns>The given <paramref name="container"/>.</returns>
        public static TContainer WithChildren<TContainer, TChild>(this TContainer container, IEnumerable<TChild> children)
            where TContainer : IContainerCollection<TChild>
            where TChild : Drawable
        {
            container.ChildrenEnumerable = children;

            return container;
        }
    }
}
