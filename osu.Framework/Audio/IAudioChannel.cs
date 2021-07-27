// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Threading.Tasks;
using osu.Framework.Audio.Mixing;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Interface for audio channels. Audio channels can be routed into different mixers via <see cref="IAudioMixer.Add"/>.
    /// </summary>
    public interface IAudioChannel
    {
        /// <summary>
        /// The mixer in which all audio produced by this channel should be routed into.
        /// </summary>
        internal AudioMixer? Mixer { get; set; }

        /// <summary>
        /// Enqueues an action to be performed on the audio thread at time this channel is updated.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>A task which can be used for continuation logic. May return a <see cref="Task.CompletedTask"/> if called while already on the audio thread.</returns>
        internal Task EnqueueAction(Action action);
    }
}
