// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Statistics
{
    public class GlobalStatistic<T> : IGlobalStatistic
    {
        public string Group { get; }

        public string Name { get; }

        public IBindable<string> DisplayValue => displayValue;

        private readonly Bindable<string> displayValue = new Bindable<string>();

        public Bindable<T> Bindable { get; } = new Bindable<T>();

        public T Value
        {
            get => Bindable.Value;
            set => Bindable.Value = value;
        }

        public GlobalStatistic(string group, string name)
        {
            Group = group;
            Name = name;

            Bindable.BindValueChanged(val =>
            {
                switch (val.NewValue)
                {
                    case double d:
                        displayValue.Value = d.ToString("#,0.##");
                        break;

                    case int i:
                        displayValue.Value = i.ToString("#,0");
                        break;

                    case long l:
                        displayValue.Value = l.ToString("#,0");
                        break;

                    default:
                        displayValue.Value = val.NewValue.ToString();
                        break;
                }
            }, true);
        }

        public virtual void Clear() => Bindable.SetDefault();
    }
}
