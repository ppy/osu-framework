// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Stores;
using osu.Framework.Platform.Linux.Native;
using osu.Framework.Threading;

namespace osu.Framework.Audio.Manager.Bass
{
    using Bass = ManagedBass.Bass;
    using Configuration = ManagedBass.Configuration;

    /// <summary>
    /// An abstract audio manager implementation using the BASS audio library.
    /// </summary>
    /// <remarks>
    /// See <see cref="BassPrimitiveAudioManager"/> for a concrete implementation of the BASS audio manager without any additional add-ons.
    /// </remarks>
    public abstract class BassAudioManager : AudioManager
    {
        protected BassAudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            PreloadBass();
        }

        protected abstract bool IsDeviceChanged(int device);

        /// <summary>
        /// This method calls <see cref="AudioManager.InitDevice"/>.
        /// It can be overridden for unit testing.
        /// </summary>
        protected virtual bool InitBass(int device)
        {
            if (!IsDeviceChanged(device))
                return true;

            // this likely doesn't help us but also doesn't seem to cause any issues or any cpu increase.
            Bass.UpdatePeriod = 5;

            // reduce latency to a known sane minimum.
            Bass.DeviceBufferLength = 10;
            Bass.PlaybackBufferLength = 100;

            // ensure there are no brief delays on audio operations (causing stream stalls etc.) after periods of silence.
            Bass.DeviceNonStop = true;

            // without this, if bass falls back to directsound legacy mode the audio playback offset will be way off.
            Bass.Configure(Configuration.TruePlayPosition, 0);

            // Set BASS_IOS_SESSION_DISABLE here to leave session configuration in our hands (see iOS project).
            Bass.Configure(Configuration.IOSSession, 16);

            // Always provide a default device. This should be a no-op, but we have asserts for this behaviour.
            Bass.Configure(Configuration.IncludeDefaultDevice, true);

            // Enable custom BASS_CONFIG_MP3_OLDGAPS flag for backwards compatibility.
            // - This disables support for ItunSMPB tag parsing to match previous expectations.
            // - This also disables a change which assumes a 529 sample (2116 byte in stereo 16-bit) delay if the MP3 file doesn't specify one.
            //   (That was added in Bass for more consistent results across platforms and standard/mp3-free BASS versions, because OSX/iOS's MP3 decoder always removes 529 samples)
            Bass.Configure((Configuration)68, 1);

            // Disable BASS_CONFIG_DEV_TIMEOUT flag to keep BASS audio output from pausing on device processing timeout.
            // See https://www.un4seen.com/forum/?topic=19601 for more information.
            Bass.Configure((Configuration)70, false);

            if (!InitDevice(device))
                return false;

            return true;
        }

        /// <summary>
        /// Makes BASS available to be consumed.
        /// </summary>
        internal static void PreloadBass()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
        }
    }
}
