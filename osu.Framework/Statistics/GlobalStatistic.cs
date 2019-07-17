// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Statistics
{
    public class GlobalStatistic<T> : IGlobalStatistic
    {
        public string Group { get; }

        public string Name { get; }

        public GlobalStatistic(string group, string name)
        {
            Group = group;
            Name = name;
        }

        public string DisplayValue
        {
            get
            {
                switch (value)
                {
                    case double d:
                        return d.ToString("#,0.##");

                    case int i:
                        return i.ToString("#,0");

                    case long l:
                        return l.ToString("#,0");

                    case ulong l:
                        return l.ToString("#,0");

                    default:
                        return value?.ToString() ?? string.Empty;
                }
            }
        }

        private T value;

        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public virtual void Clear() => Value = default;
    }
}
