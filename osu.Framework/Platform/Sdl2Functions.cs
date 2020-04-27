// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    internal static unsafe class Sdl2Functions
    {
        #region GL-specific Calls

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SdlGLGetDrawableSizeDelegate(SDL_Window window, int* w, int* h);

        private static readonly SdlGLGetDrawableSizeDelegate sdl_gl_get_drawable_size = Sdl2Native.LoadFunction<SdlGLGetDrawableSizeDelegate>("SDL_GL_GetDrawableSize");

        public static Vector2 SDL_GL_GetDrawableSize(SDL_Window window)
        {
            int w, h;
            sdl_gl_get_drawable_size(window, &w, &h);
            return new Vector2(w, h);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGLGetSwapIntervalDelegate();

        private static readonly SdlGLGetSwapIntervalDelegate sdl_gl_get_swap_interval = Sdl2Native.LoadFunction<SdlGLGetSwapIntervalDelegate>("SDL_GL_GetSwapInterval");

        public static int SDL_GL_GetSwapInterval() => sdl_gl_get_swap_interval();

        #endregion

        #region Display Modes

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetCurrentDisplayModeDelegate(int displayIndex, SDL_DisplayMode* mode);

        private static readonly SdlGetCurrentDisplayModeDelegate sdl_get_current_display_mode = Sdl2Native.LoadFunction<SdlGetCurrentDisplayModeDelegate>("SDL_GetCurrentDisplayMode");

        public static SDL_DisplayMode SDL_GetCurrentDisplayMode(int displayIndex)
        {
            SDL_DisplayMode mode;
            sdl_get_current_display_mode(displayIndex, &mode);
            return mode;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate SDL_DisplayMode* SdlGetClosestDisplayModeDelegate(int displayIndex, SDL_DisplayMode* mode, SDL_DisplayMode* closest);

        private static readonly SdlGetClosestDisplayModeDelegate sdl_get_closest_display_mode = Sdl2Native.LoadFunction<SdlGetClosestDisplayModeDelegate>("SDL_GetClosestDisplayMode");

        public static SDL_DisplayMode SDL_GetClosestDisplayMode(int displayIndex, SDL_DisplayMode requested)
        {
            SDL_DisplayMode closest;
            sdl_get_closest_display_mode(displayIndex, &requested, &closest);
            return closest;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetDisplayModeDelegate(int displayIndex, int modeIndex, SDL_DisplayMode* mode);

        private static readonly SdlGetDisplayModeDelegate sdl_get_display_mode = Sdl2Native.LoadFunction<SdlGetDisplayModeDelegate>("SDL_GetDisplayMode");

        public static SDL_DisplayMode SDL_GetDisplayMode(int displayIndex, int modeIndex)
        {
            SDL_DisplayMode mode;
            sdl_get_display_mode(displayIndex, modeIndex, &mode);
            return mode;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetWindowDisplayIndexDelegate(IntPtr window);

        private static readonly SdlGetWindowDisplayIndexDelegate sdl_get_window_display_index = Sdl2Native.LoadFunction<SdlGetWindowDisplayIndexDelegate>("SDL_GetWindowDisplayIndex");

        public static int SDL_GetWindowDisplayIndex(SDL_Window window) => sdl_get_window_display_index(window);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetWindowDisplayModeDelegate(IntPtr window, SDL_DisplayMode* mode);

        private static readonly SdlGetWindowDisplayModeDelegate sdl_get_window_display_mode = Sdl2Native.LoadFunction<SdlGetWindowDisplayModeDelegate>("SDL_GetWindowDisplayMode");

        public static SDL_DisplayMode SDL_GetWindowDisplayMode(SDL_Window window)
        {
            SDL_DisplayMode mode;
            sdl_get_window_display_mode(window.NativePointer, &mode);
            return mode;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlSetWindowDisplayModeDelegate(IntPtr window, SDL_DisplayMode* mode);

        private static readonly SdlSetWindowDisplayModeDelegate sdl_set_window_display_mode = Sdl2Native.LoadFunction<SdlSetWindowDisplayModeDelegate>("SDL_SetWindowDisplayMode");

        public static int SDL_SetWindowDisplayMode(SDL_Window window, SDL_DisplayMode mode) => sdl_set_window_display_mode(window.NativePointer, &mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetNumDisplayModesDelegate(int displayIndex);

        private static readonly SdlGetNumDisplayModesDelegate sdl_get_num_display_modes = Sdl2Native.LoadFunction<SdlGetNumDisplayModesDelegate>("SDL_GetNumDisplayModes");

        public static int SDL_GetNumDisplayModes(int displayIndex) => sdl_get_num_display_modes(displayIndex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetNumVideoDisplaysDelegate();

        private static readonly SdlGetNumVideoDisplaysDelegate sdl_get_num_video_displays = Sdl2Native.LoadFunction<SdlGetNumVideoDisplaysDelegate>("SDL_GetNumVideoDisplays");

        public static int SDL_GetNumVideoDisplays() => sdl_get_num_video_displays();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGetDisplayBoundsDelegate(int displayIndex, SDL_Rect* rect);

        private static readonly SdlGetDisplayBoundsDelegate sdl_get_display_bounds = Sdl2Native.LoadFunction<SdlGetDisplayBoundsDelegate>("SDL_GetDisplayBounds");

        public static Rectangle SDL_GetDisplayBounds(int displayIndex)
        {
            SDL_Rect rect;
            sdl_get_display_bounds(displayIndex, &rect);
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SdlGetDisplayNameDelegate(int displayIndex);

        private static readonly SdlGetDisplayNameDelegate sdl_get_display_name = Sdl2Native.LoadFunction<SdlGetDisplayNameDelegate>("SDL_GetDisplayName");

        public static string SDL_GetDisplayName(int displayIndex) => Marshal.PtrToStringAnsi(sdl_get_display_name(displayIndex));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SdlGetPixelFormatNameDelegate(uint format);

        private static readonly SdlGetPixelFormatNameDelegate sdl_get_pixel_format_name = Sdl2Native.LoadFunction<SdlGetPixelFormatNameDelegate>("SDL_GetPixelFormatName");

        public static string SDL_GetPixelFormatName(uint format) => Marshal.PtrToStringAnsi(sdl_get_pixel_format_name(format));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SdlPixelFormatEnumToMasksDelegate(uint format, int* bpp, uint* rMask, uint* gMask, uint* bMask, uint* aMask);

        private static readonly SdlPixelFormatEnumToMasksDelegate sdl_pixel_format_enum_to_masks = Sdl2Native.LoadFunction<SdlPixelFormatEnumToMasksDelegate>("SDL_PixelFormatEnumToMasks");

        public static bool SDL_PixelFormatEnumToMasks(uint format, out int bpp, out uint rMask, out uint gMask, out uint bMask, out uint aMask)
        {
            int lBpp;
            uint lRMask, lGMask, lBMask, lAMask;
            bool rv = sdl_pixel_format_enum_to_masks(format, &lBpp, &lRMask, &lGMask, &lBMask, &lAMask);

            bpp = lBpp;
            rMask = lRMask;
            gMask = lGMask;
            bMask = lBMask;
            aMask = lAMask;

            return rv;
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct SDL_DisplayMode
    {
        public uint Format;
        public int Width;
        public int Height;
        public int RefreshRate;
        public IntPtr DriverData;
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct SDL_Rect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }
}
