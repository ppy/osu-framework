// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
