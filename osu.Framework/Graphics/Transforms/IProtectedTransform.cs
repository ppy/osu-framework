// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// Interface for <see cref="Transform"/>s that may not be removed by a user through <see cref="Transformable.ClearTransformsAfter(double, bool, string)"/>.
    /// This should be used on <see cref="Transform"/>s private to <see cref="Drawable"/>s that are required to achieve a valid
    /// state for the <see cref="Drawable"/>, such as autosize or flow <see cref="Transform"/>s.
    /// </summary>
    public interface IProtectedTransform
    {
    }
}
