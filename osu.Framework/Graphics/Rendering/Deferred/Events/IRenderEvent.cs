// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal interface IRenderEvent
    {
        /// <summary>
        /// The type of the event. This must be implemented as the first field in the struct.
        /// </summary>
        RenderEventType Type { get; }
    }
}
