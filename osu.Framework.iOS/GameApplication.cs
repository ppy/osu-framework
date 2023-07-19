// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using ObjCRuntime;
using SDL2;
using Veldrid.SPIRV;

namespace osu.Framework.iOS
{
    public static class GameApplication
    {
        private const string output_volume = @"outputVolume";

        private static IOSGameHost host = null!;
        private static Game game = null!;

        private static readonly OutputVolumeObserver output_volume_observer = new OutputVolumeObserver();

        public static void Main(Game target)
        {
            NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass.framework/bass", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassFx).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass_fx.framework/bass_fx", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassMix).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bassmix.framework/bassmix", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(SpirvCompilation).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/veldrid-spirv.framework/veldrid-spirv", assembly, path));

            game = target;

            SDL.PrepareLibraryForIOS();
            SDL.SDL_UIKitRunApp(0, IntPtr.Zero, main);
        }

        [MonoPInvokeCallback(typeof(SDL.SDL_main_func))]
        private static int main(int argc, IntPtr argv)
        {
            var audioSession = AVAudioSession.SharedInstance();
            audioSession.AddObserver(output_volume_observer, output_volume, NSKeyValueObservingOptions.New, 0);

            host = new IOSGameHost();
            host.Run(game);

            return 0;
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
