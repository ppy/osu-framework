// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainer : IDrawable
    {
        Vector2 ChildSize { get; }
        Vector2 ChildScale { get; }
        Vector2 ChildOffset { get; }

        void InvalidateFromChild(Invalidation invalidation, IDrawable source);
    }

    public interface IContainer<T> : IContainer
    {
        IEnumerable<T> Children { get; set; }
        IEnumerable<T> AliveChildren { get; }

        void Add(IEnumerable<T> collection);
        void Add(T drawable);
        void Remove(IEnumerable<T> range, bool dispose = false);
        bool Remove(T drawable, bool dispose = false);
        int RemoveAll(Predicate<T> match, bool dispose = false);
    }
}