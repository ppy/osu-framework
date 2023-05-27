// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Effects;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainer : IDrawable
    {
        EdgeEffectParameters EdgeEffect { get; set; }

        Vector2 RelativeChildSize { get; set; }

        Vector2 RelativeChildOffset { get; set; }
    }

    public interface IContainerEnumerable<out T> : IContainer
        where T : class, IDrawable
    {
        IReadOnlyList<T> Children { get; }

        /// <summary>
        /// Remove all matching children from this container.
        /// </summary>
        /// <param name="match">A predicate used to find matching items.</param>
        /// <param name="disposeImmediately">Whether removed items should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed items are to be re-used in the future.
        /// If <c>false</c>, ensure removed items are manually disposed else object leakage may occur.
        /// </remarks>
        /// <returns>The number of matching items removed.</returns>
        int RemoveAll(Predicate<T> match, bool disposeImmediately);
    }

    public interface IContainerCollection<in T> : IContainer
        where T : class, IDrawable
    {
        IReadOnlyList<T> Children { set; }

        T Child { set; }

        IEnumerable<T> ChildrenEnumerable { set; }

        void Add(T drawable);
        void AddRange(IEnumerable<T> collection);

        /// <summary>
        /// Remove the provided drawable from this container's children.
        /// </summary>
        /// <param name="drawable">The drawable to be removed.</param>
        /// <param name="disposeImmediately">Whether removed item should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed item is to be re-used in the future.
        /// If <c>false</c>, ensure the removed item is manually disposed (or added back to another part of the hierarchy) else
        /// object leakage may occur.
        /// </remarks>
        /// <returns>Whether the drawable was removed.</returns>
        bool Remove(T drawable, bool disposeImmediately);

        /// <summary>
        /// Remove all matching children from this container.
        /// </summary>
        /// <param name="range">The drawables to be removed.</param>
        /// <param name="disposeImmediately">Whether removed items should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed items are to be re-used in the future.
        /// If <c>false</c>, ensure removed items are manually disposed else object leakage may occur.
        /// </remarks>
        void RemoveRange(IEnumerable<T> range, bool disposeImmediately);
    }
}
