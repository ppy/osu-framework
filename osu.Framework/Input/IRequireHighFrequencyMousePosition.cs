// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;

namespace osu.Framework.Input
{
    /// <summary>
    /// Guarantees that a drawable will receive at least one OnMouseMove position update
    /// per update frame (in addition to any input-triggered occurrences).
    /// </summary>
    public interface IRequireHighFrequencyMousePosition : IDrawable
    {
    }
}
