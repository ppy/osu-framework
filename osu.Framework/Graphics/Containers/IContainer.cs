// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        int RemoveAll(Predicate<T> match);
    }

    public interface IContainerCollection<in T> : IContainer
        where T : class, IDrawable
    {
        IReadOnlyList<T> Children { set; }

        T Child { set; }

        IEnumerable<T> ChildrenEnumerable { set; }

        void Add(T drawable);
        void AddRange(IEnumerable<T> collection);

        bool Remove(T drawable);
        void RemoveRange(IEnumerable<T> range);
    }
}
