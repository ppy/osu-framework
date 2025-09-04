// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Manager.Bass
{
    /// <summary>
    /// An interface which allows querying of a global mixer handle, if one is being used.
    /// If this interface is implemented, all game mixers should be added to the global mixer.
    /// </summary>
    public interface IGlobalMixerProvider
    {
        /// <summary>
        /// If a global mixer is being used, this will be the BASS handle for it.
        /// </summary>
        /// <remarks>
        /// All mixers created via <see cref="IAudioManager.CreateAudioMixer"/> will themselves be
        /// added to the global mixer, which will handle playback itself.
        ///
        /// In this mode of operation, nested mixers will be created with the <see cref="BassFlags.Decode"/>
        /// flag, meaning they no longer handle playback directly.
        ///
        /// An eventual goal would be to use a global mixer across all platforms as it can result
        /// in more control and better playback performance.
        /// </remarks>
        IBindable<int> GlobalMixerHandle { get; }
    }
}
