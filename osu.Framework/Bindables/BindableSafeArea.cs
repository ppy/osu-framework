// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// A subclass of <see cref="Bindable{MarginPadding}"/> specifically for representing the "safe areas" of a device.
    /// It exists to prevent regular <see cref="MarginPadding"/>s from being globally cached.
    /// </summary>
    public class BindableSafeArea : Bindable<MarginPadding>
    {
        public BindableSafeArea(MarginPadding value = default)
            : base(value)
        {
        }

        protected override Bindable<MarginPadding> CreateInstance() => new BindableSafeArea();
    }
}
