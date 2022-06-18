// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// An audio channel which plays audio. Audio channels can be routed into different mixers via <see cref="IAudioMixer.Add"/>.
    /// </summary>
    public interface IAudioChannel
    {
        /// <summary>
        /// The mixer in which all audio produced by this channel should be routed into.
        /// </summary>
        internal AudioMixer? Mixer { get; set; }

        /// <summary>
        /// Enqueues an action to be performed on the audio thread as part of this channel.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>A task which can be used for continuation logic. May return a <see cref="Task.CompletedTask"/> if called while already on the audio thread.</returns>
        internal Task EnqueueAction(Action action);
    }
}
