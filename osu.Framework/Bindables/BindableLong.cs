// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Bindables
{
    public class BindableLong : BindableNumber<long>
    {
        public BindableLong(long defaultValue = default)
            : base(defaultValue)
        {
        }

        protected override Bindable<long> CreateInstance() => new BindableLong();
    }
}
