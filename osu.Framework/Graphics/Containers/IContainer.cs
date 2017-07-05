// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainerEnumerable<out T>
        where T : IDrawable
    {
        IReadOnlyList<T> Children { get; }

        int RemoveAll(Predicate<T> match);
    }

    public interface IContainerCollection<in T>
        where T : IDrawable
    {
        IReadOnlyList<T> Children { set; }

        void Add(T drawable);
        void Add(IEnumerable<T> collection);

        void Remove(T drawable);
        void Remove(IEnumerable<T> range);
    }
}
