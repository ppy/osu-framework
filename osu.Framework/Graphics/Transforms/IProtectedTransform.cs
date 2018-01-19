// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// Interface for <see cref="Transform"/>s that may not be removed from a <see cref="Transformable"/>
    /// through <see cref="Transformable.ClearTransformsAfter(double, bool, string)"/>.
    /// This should be used internally for any <see cref="Transform"/>s that are required for a valid
    /// <see cref="Drawable"/> state to be maintained, such as autosize <see cref="Transform"/>s.
    /// </summary>
    internal interface IProtectedTransform
    {
    }
}
