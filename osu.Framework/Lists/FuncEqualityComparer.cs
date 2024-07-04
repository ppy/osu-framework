// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> func;

        public FuncEqualityComparer(Func<T, T, bool> func)
        {
            this.func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool Equals(T x, T y) => func(x, y);

        public int GetHashCode(T obj) => obj.GetHashCode();
    }
}
