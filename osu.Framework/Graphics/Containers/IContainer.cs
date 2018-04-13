// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainer : IDrawable
    {
        EdgeEffectParameters EdgeEffect { get; set; }

        Vector2 RelativeChildSize { get; set; }

        Vector2 RelativeChildOffset { get; set; }
    }

    public interface IContainerEnumerable<out T> : IContainer
        where T : IDrawable
    {
        IReadOnlyList<T> Children { get; }

        int RemoveAll(Predicate<T> match);
    }

    public interface IContainerCollection<in T> : IContainer
        where T : IDrawable
    {
        IReadOnlyList<T> Children { set; }

        void Add(T drawable);
        void AddRange(IEnumerable<T> collection);

        bool Remove(T drawable);
        void RemoveRange(IEnumerable<T> range);
    }
}
