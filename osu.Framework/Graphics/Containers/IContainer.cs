// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public interface IContainer : IDrawable
    {
        IEnumerable<Drawable> Children { get; set; }

        void Add(IEnumerable<Drawable> collection);
        void Add(Drawable drawable);
        void Remove(IEnumerable<Drawable> range, bool dispose = false);
        bool Remove(Drawable drawable, bool dispose = false);
        int RemoveAll(Predicate<Drawable> match, bool dispose = false);

        void InvalidateFromChild(Invalidation invalidation, Drawable source);

        Vector2 ChildSize { get; }
        Vector2 ChildScale { get; }
        Vector2 ChildOffset { get; }
    }
}