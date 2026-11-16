// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.Versioning;
using ManagedBass;
using NAudio.CoreAudioApi;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio.Windows
{
    [SupportedOSPlatform("windows")]
    internal sealed class WasapiExclusiveEndpointGain : IDisposable
    {
        private readonly MMDevice device;
        private readonly Scheduler audioScheduler;
        private readonly int mixerHandle;
        private bool disposed;

        public static WasapiExclusiveEndpointGain? TryAttach(string? endpointId, int mixerHandle, Scheduler audioScheduler)
        {
            if (mixerHandle == 0)
                return null;

            MMDevice? device = null;

            try
            {
                device = WindowsAudioFormatQuery.TryOpenPlaybackDevice(endpointId);

                if (device == null)
                {
                    Logger.Log("WASAPI Exclusive endpoint volume: playback device unavailable.", name: "audio", level: LogLevel.Important);
                    return null;
                }

                var gain = new WasapiExclusiveEndpointGain(device, mixerHandle, audioScheduler);
                device = null;
                gain.subscribeAndApply();
                return gain;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI Exclusive endpoint volume attach failed: {ex.Message}", name: "audio", level: LogLevel.Important);
                device?.Dispose();
                return null;
            }
        }

        private WasapiExclusiveEndpointGain(MMDevice device, int mixerHandle, Scheduler audioScheduler)
        {
            this.device = device;
            this.mixerHandle = mixerHandle;
            this.audioScheduler = audioScheduler;
        }

        private void subscribeAndApply()
        {
            apply(device.AudioEndpointVolume.MasterVolumeLevelScalar, device.AudioEndpointVolume.Mute);
            device.AudioEndpointVolume.OnVolumeNotification += onVolumeNotification;
        }

        private void onVolumeNotification(AudioVolumeNotificationData data)
        {
            float scalar = data.MasterVolume;
            bool muted = data.Muted;
            audioScheduler.Add(() => apply(scalar, muted));
        }

        private void apply(float scalar, bool muted)
        {
            if (disposed)
                return;

            float volume = WasapiExclusiveEndpointGainPolicy.MixerVolume(scalar, muted);
            Bass.ChannelSetAttribute(mixerHandle, ChannelAttribute.Volume, volume);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            try
            {
                device.AudioEndpointVolume.OnVolumeNotification -= onVolumeNotification;
            }
            catch
            {
            }

            try
            {
                Bass.ChannelSetAttribute(mixerHandle, ChannelAttribute.Volume, 1f);
            }
            catch
            {
            }

            device.Dispose();
        }
    }
}
