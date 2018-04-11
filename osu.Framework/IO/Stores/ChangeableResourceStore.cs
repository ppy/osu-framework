// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
