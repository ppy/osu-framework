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
        internal event Action<string>? DragDrop;

        private const string output_volume = "outputVolume";

        private static readonly OutputVolumeObserver output_volume_observer = new OutputVolumeObserver();

        public IOSGameHost Host { get; private set; } = null!;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            mapLibraryNames();

            SDL_SetMainReady();
            SDL_SetiOSEventPump(true);

            var audioSession = AVAudioSession.SharedInstance();

            // Set the default audio session to one that obeys the mute switch and does not mix with other audio,
            // and insert an observer to disregard the mute switch when the user presses volume up/down.
            audioSession.SetCategory(AVAudioSessionCategory.SoloAmbient);
            audioSession.AddObserver(output_volume_observer, output_volume, NSKeyValueObservingOptions.New, 0);

            Host = new IOSGameHost();
            Host.Run(CreateGame());
            return true;
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            // copied verbatim from SDL: https://github.com/libsdl-org/SDL/blob/d252a8fe126b998bd1b0f4e4cf52312cd11de378/src/video/uikit/SDL_uikitappdelegate.m#L508-L535
            // the hope is that the SDL app delegate class does not have such handling exist there, but Apple does not provide a corresponding notification to make that possible.
            NSUrl? fileUrl = url.FilePathUrl;
            DragDrop?.Invoke(fileUrl != null ? fileUrl.Path! : url.AbsoluteString!);
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
