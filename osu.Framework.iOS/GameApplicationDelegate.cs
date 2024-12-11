// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using SDL;
using UIKit;
using static SDL.SDL3;

namespace osu.Framework.iOS
{
    /// <summary>
    /// Base <see cref="UIApplicationDelegate"/> implementation for osu!framework applications.
    /// </summary>
    public abstract class GameApplicationDelegate : UIApplicationDelegate
    {

        private const string output_volume = "outputVolume";

        private static readonly OutputVolumeObserver output_volume_observer = new OutputVolumeObserver();

        private IOSGameHost host = null!;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            mapLibraryNames();

            SDL_SetMainReady();
            SDL_SetiOSEventPump(true);

            var audioSession = AVAudioSession.SharedInstance();
            audioSession.AddObserver(output_volume_observer, output_volume, NSKeyValueObservingOptions.New, 0);

            host = new IOSGameHost();
            host.Run(CreateGame());
            return true;
        }

        /// <summary>
        /// Creates the <see cref="Game"/> class to launch.
        /// </summary>
        protected abstract Game CreateGame();

        private static void mapLibraryNames()
        {
            NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass.framework/bass", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassFx).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass_fx.framework/bass_fx", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassMix).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bassmix.framework/bassmix", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(SDL3).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/SDL3.framework/SDL3", assembly, path));
        }

        private class OutputVolumeObserver : NSObject
        {
            public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, nint context)
            {
                switch (keyPath)
                {
                    case output_volume:
                        AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
                        break;
                }
            }
        }
    }
}
