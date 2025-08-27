// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using ManagedBass.Wasapi;
using osu.Framework.Audio.Manager.Bass;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework
{
    public abstract partial class Game
    {
        public AudioBackend ResolvedAudioBackend { get; private set; }

        public IEnumerable<AudioBackend> GetPreferredAudioBackendsForCurrentPlatform()
        {
            yield return AudioBackend.Automatic;

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    yield return AudioBackend.Bass;

                    bool wasapiSupported = false;

                    try
                    {
                        // Querying device info is the most reliable way to determine if WASAPI is supported.
                        if (BassWasapi.GetDeviceInfo(BassWasapi.DefaultDevice, out var _) || Bass.LastError != Errors.Wasapi)
                        {
                            wasapiSupported = true;
                        }
                    }
                    catch
                    {
                        // Ignore any errors in querying bass wasapi devices.
                    }

                    if (wasapiSupported)
                    {
                        // Give preference to WASAPI if available.
                        yield return AudioBackend.BassWasapiSharedMode;
                        yield return AudioBackend.BassWasapiExclusiveMode;
                    }

                    break;

                default:
                    yield return AudioBackend.Bass;

                    break;
            }
        }

        protected virtual void ChooseAndSetupAudio(FrameworkConfigManager config)
        {
            var tracks = new ResourceStore<byte[]>();
            tracks.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"));
            tracks.AddStore(CreateOnlineStore());

            var samples = new ResourceStore<byte[]>();
            samples.AddStore(new NamespacedResourceStore<byte[]>(Resources, @"Samples"));
            samples.AddStore(CreateOnlineStore());

            // Always give preference to environment variables.
            if (FrameworkEnvironment.PreferredAudioBackend != null || FrameworkEnvironment.PreferredAudioDevice != null)
            {
                Logger.Log("🔈 Using environment variables for audio backend and device selection.", level: LogLevel.Important);

                // And allow this to hard fail with no fallbacks.
                SetupAudio(FrameworkEnvironment.PreferredAudioBackend ?? AudioBackend.Bass, FrameworkEnvironment.PreferredAudioDevice, tracks, samples);
                PostSetupAudio(config);
            }

            var configAudioBackend = config.GetBindable<AudioBackend>(FrameworkSetting.AudioBackend);
            Logger.Log($"🔈 Configuration audio backend choice: {configAudioBackend}");

            var audioBackendTypes = GetPreferredAudioBackendsForCurrentPlatform().Where(b => b != AudioBackend.Automatic).ToList();

            // Move user's preference to the start of the attempts.
            if (!configAudioBackend.IsDefault)
            {
                audioBackendTypes.Remove(configAudioBackend.Value);
                audioBackendTypes.Insert(0, configAudioBackend.Value);
            }

            Logger.Log($"🔈 Audio backend fallback order: [ {string.Join(", ", audioBackendTypes)} ]");

            foreach (AudioBackend backend in audioBackendTypes)
            {
                try
                {
                    SetupAudio(backend, config.Get<string>(FrameworkSetting.AudioDevice), tracks, samples);
                    Logger.Log($"🔈 Using audio backend: {backend}");
                    ResolvedAudioBackend = backend;
                    break;
                }
                catch
                {
                    if (configAudioBackend.Value != AudioBackend.Automatic)
                    {
                        // If we fail, assume the user may have had a custom setting and switch it back to automatic.
                        Logger.Log($"The selected audio backend ({configAudioBackend.Value}) failed to initialise. Audio backend selection has been reverted to automatic.",
                            level: LogLevel.Important);
                        configAudioBackend.Value = AudioBackend.Automatic;
                    }
                }
            }

            PostSetupAudio(config);
        }

        protected void SetupAudio(AudioBackend backend, string? device, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
        {
            switch (backend)
            {
                case AudioBackend.BassWasapiExclusiveMode:
                    var bassWasapiExclusiveMode = new BassWasapiAudioManager(Host.AudioThread, trackStore, sampleStore, true);
                    bassWasapiExclusiveMode.AudioDevice.Value = device ?? bassWasapiExclusiveMode.DefaultDevice;
                    Audio = bassWasapiExclusiveMode;
                    break;

                case AudioBackend.BassWasapiSharedMode:
                    var bassWasapiSharedMode = new BassWasapiAudioManager(Host.AudioThread, trackStore, sampleStore, false);
                    bassWasapiSharedMode.AudioDevice.Value = device ?? bassWasapiSharedMode.DefaultDevice;
                    Audio = bassWasapiSharedMode;
                    break;

                default:
                case AudioBackend.Bass:
                    var bass = new BassPrimitiveAudioManager(Host.AudioThread, trackStore, sampleStore);
                    bass.AudioDevice.Value = device ?? bass.DefaultDevice;
                    Audio = bass;
                    break;
            }
        }

        protected void PostSetupAudio(FrameworkConfigManager config)
        {
            Audio.EventScheduler = Scheduler;

            dependencies.CacheAs(Audio);
            dependencies.CacheAs(Audio.Tracks);
            dependencies.CacheAs(Audio.Samples);

            // attach our bindables to the audio subsystem.
            config.BindWith(FrameworkSetting.AudioDevice, Audio.AudioDevice);
            config.BindWith(FrameworkSetting.VolumeUniversal, Audio.Volume);
            config.BindWith(FrameworkSetting.VolumeEffect, Audio.VolumeSample);
            config.BindWith(FrameworkSetting.VolumeMusic, Audio.VolumeTrack);
        }
    }
}
