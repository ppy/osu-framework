﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

        void InvalidateFromChild(Invalidation invalidation, IDrawable source);

        void Clear(bool dispose = true);
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

        bool Remove(T drawable);
        void Remove(IEnumerable<T> range);
    }
}
