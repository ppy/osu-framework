// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.IO.Stores
{
    public class ChangeableResourceStore<T> : ResourceStore<T>
    {
        public event Action<string> OnChanged;

        protected void TriggerOnChanged(string name)
        {
            OnChanged?.Invoke(name);
        }
    }
}
