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
        Vector2 RelativeChildOffset { get; }

        EdgeEffectParameters EdgeEffect { get; set; }
        float CornerRadius { get; }

        void InvalidateFromChild(Invalidation invalidation);

        void Clear(bool dispose = true);

        Axes AutoSizeAxes { get; set; }
    }

    public interface IContainerEnumerable<out T> : IContainer
        where T : IDrawable
    {
        IReadOnlyList<Drawable> InternalChildren { get; }
        IReadOnlyList<T> Children { get; }

        int RemoveAll(Predicate<T> match);
    }

    public interface IContainerCollection<in T> : IContainer
        where T : IDrawable
    {
        IReadOnlyList<Drawable> InternalChildren { set; }
        IReadOnlyList<T> Children { set; }

        void Add(T drawable);
        void Add(IEnumerable<T> collection);

        void Remove(T drawable);
        void Remove(IEnumerable<T> range);
    }
}
