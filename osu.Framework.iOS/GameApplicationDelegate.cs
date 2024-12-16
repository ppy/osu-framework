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

        private IOSGameHost host = null!;

        private bool lockScreenOrientation { get; set; }

        /// <summary>
        /// Whether the screen orientation should be locked from rotation.
        /// </summary>
        public bool LockScreenOrientation
        {
            get => lockScreenOrientation;
            set
            {
                if (lockScreenOrientation == value)
                    return;

                lockScreenOrientation = value;

                InvokeOnMainThread(() =>
                {
                    if (OperatingSystem.IsIOSVersionAtLeast(16))
                        ((IOSWindow)host.Window).UIWindow.RootViewController!.SetNeedsUpdateOfSupportedInterfaceOrientations();
                    else
                        UIViewController.AttemptRotationToDeviceOrientation();
                });
            }
        }

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

            host = new IOSGameHost();
            host.Run(CreateGame());
            return true;
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            // copied verbatim from SDL: https://github.com/libsdl-org/SDL/blob/d252a8fe126b998bd1b0f4e4cf52312cd11de378/src/video/uikit/SDL_uikitappdelegate.m#L508-L535
            // the hope is that the SDL app delegate class does not have such handling exist there, but Apple does not provide a corresponding notification to make that possible.
            NSUrl? fileUrl = url.FilePathUrl;
            DragDrop?.Invoke(fileUrl != null ? fileUrl.Path! : url.AbsoluteString!);
            return true;
        }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow? forWindow)
        {
            if (forWindow == null)
                // although not documented anywhere for some reason, this may be called with forWindow = null during initialisation.
                // return this random mask just to continue execution without nullrefs anywhere.
                // this will be called again with a valid forWindow when it's available anyway.
                return UIInterfaceOrientationMask.All;

            var allOrientations = application.SupportedInterfaceOrientationsForWindow(forWindow);
            var currentOrientation = getOrientationMask(forWindow.WindowScene!.InterfaceOrientation);
            return LockScreenOrientation ? currentOrientation : allOrientations;
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

        private static UIInterfaceOrientationMask getOrientationMask(UIInterfaceOrientation orientation)
        {
            switch (orientation)
            {
                case UIInterfaceOrientation.Portrait:
                    return UIInterfaceOrientationMask.Portrait;

                case UIInterfaceOrientation.PortraitUpsideDown:
                    return UIInterfaceOrientationMask.PortraitUpsideDown;

                default:
                case UIInterfaceOrientation.LandscapeRight:
                    return UIInterfaceOrientationMask.LandscapeRight;

                case UIInterfaceOrientation.LandscapeLeft:
                    return UIInterfaceOrientationMask.LandscapeLeft;
            }
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
