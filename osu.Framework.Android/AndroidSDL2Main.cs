// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Runtime.InteropServices;
using osu.Framework.Platform;
using SDL2;

namespace osu.Framework.Android
{
    internal class AndroidSDL2Main
    {
        internal static AndroidGameHost Host { get; private set; }
        internal static Game Game { get; private set; }

        internal static AndroidGameActivity Activity { get; private set; }

        [MonoPInvokeCallback(typeof(customMain))]
        private static int sdl2Main()
        {
            // blocks back button
            SDL.SDL_SetHint(SDL.SDL_HINT_ANDROID_TRAP_BACK_BUTTON, "1");

            // accelerometer spams input thread
            SDL.SDL_SetHint(SDL.SDL_HINT_ACCELEROMETER_AS_JOYSTICK, "0");

            // hints are here because they don't apply well in another location such as SDL2Window

            Host = new AndroidGameHost(Activity);
            Game = Activity.CreateGameInternal();

            Host.Run(Game);

            return 0;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int customMain();

        [DllImport("SDL2AndroidMainSetter", EntryPoint = "setMain", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setMain([MarshalAs(UnmanagedType.FunctionPtr)] customMain main);

        internal static void SetSDL2Main(AndroidGameActivity activity)
        {
            Activity = activity;
            setMain(sdl2Main);
        }
    }
}
