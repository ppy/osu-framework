// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Audio.Asio;

namespace osu.Framework.Threading
{
    public partial class AudioThread
    {
        private AsioOutput? asioOutput;

        internal bool IsAsioActive => asioOutput != null;

        internal string InitAsio(string device, int sampleRate, int bufferSize)
        {
            freeWasapi();
            freeAsio();
            Bass.CurrentDevice = Bass.NoSoundDevice;

            var output = new AsioOutput();

            try
            {
                output.Initialise(device, sampleRate, bufferSize);
                asioOutput = output;
                globalMixerHandle.Value = output.MixerHandle;
                output.Start(bufferSize);
                return output.Status;
            }
            catch
            {
                output.Dispose();
                asioOutput = null;
                globalMixerHandle.Value = null;
                throw;
            }
        }

        internal void FreeAsio() => freeAsio();

        private void freeAsio()
        {
            if (asioOutput == null)
                return;

            // Stop and join the driver's callbacks before freeing any mixer they can read.
            asioOutput.Dispose();
            asioOutput = null;
            globalMixerHandle.Value = null;
        }

        private void checkAsioNotifications()
        {
            if (asioOutput?.ConsumeResetRequest() != true)
                return;

            // Driver notifications are consumed here, never inside the native callback.
            lock (managers)
            {
                foreach (var manager in managers)
                    manager.RetryAsio();
            }
        }
    }
}
