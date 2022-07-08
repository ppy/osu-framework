// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input
{
    /// <summary>
    /// Marker interface which suppresses logging of keyboard input events.
    /// Useful for password fields, where user input should not be logged.
    /// </summary>
    public interface ISuppressKeyEventLogging
    {
    }
}
