// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Video;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Allows the framework and consumers to reference and resolve the game host
    /// while only exposing the parts important to consumers.
    /// </summary>
    public interface IGameHost
    {
        IAudioBackend AudioBackend { get; }
        IVideoBackend VideoBackend { get; }
    }
}
