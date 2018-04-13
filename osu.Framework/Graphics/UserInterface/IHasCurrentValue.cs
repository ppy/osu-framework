// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A UI element which supports a <see cref="Bindable{T}"/> current value.
    /// You can bind to <see cref="Current"/>'s <see cref="Bindable{T}.ValueChanged"/> to get updates.
    /// </summary>
    public interface IHasCurrentValue<T>
    {
        Bindable<T> Current { get; }
    }
}
