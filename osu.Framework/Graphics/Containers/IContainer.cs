// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainer : IDrawable
    {
        Vector2 ChildSize { get; }
        Vector2 ChildOffset { get; }
        Vector2 RelativeToAbsoluteFactor { get; }

        float CornerRadius { get; }

        void InvalidateFromChild(Invalidation invalidation);

        void Clear(bool dispose = true);

        Axes AutoSizeAxes { get; set; }
    }

    public interface IContainerEnumerable<out T> : IContainer
        where T : IDrawable
    {
        IEnumerable<T> InternalChildren { get; }
        IEnumerable<T> Children { get; }

        int RemoveAll(Predicate<T> match);
    }

    public interface IContainerCollection<in T> : IContainer
        where T : IDrawable
    {
        IEnumerable<T> InternalChildren { set; }
        IEnumerable<T> Children { set; }

        void Add(T drawable);
        void Add(IEnumerable<T> collection);

        void Remove(T drawable);
        void Remove(IEnumerable<T> range);
    }
}
