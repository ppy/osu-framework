// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Video;

namespace osu.Framework.Backends
{
    /// <summary>
    /// Provides concrete implementations of various <see cref="IBackend"/> classes required by the game.
    /// </summary>
    public interface IBackendProvider
    {
        /// <summary>
        /// Provides the concrete implementation of the audio backend.
        /// </summary>
        IAudioBackend Audio { get; }

        /// <summary>
        /// Provides the concrete implementation of the video backend.
        /// </summary>
        IVideoBackend Video { get; }
    }
}
