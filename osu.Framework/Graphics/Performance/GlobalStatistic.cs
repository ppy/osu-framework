// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.Performance
{
    public class GlobalStatistic<T> : IGlobalStatistic
    {
        public string Group { get; }
        public string Name { get; }

        public IBindable<string> DisplayValue => displayValue;

        private readonly Bindable<string> displayValue = new Bindable<string>();

        public Bindable<T> Value { get; set; } = new Bindable<T>();

        public GlobalStatistic(string group, string name)
        {
            Group = group;
            Name = name;

            Value.ValueChanged += val => displayValue.Value = val.NewValue.ToString();
        }
    }
}
