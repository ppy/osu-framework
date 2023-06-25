// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SDL2;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2GraphicsSurface : IGraphicsSurface, IOpenGLGraphicsSurface, IMetalGraphicsSurface, ILinuxGraphicsSurface
    {
        private readonly SDL2Window window;

        private IntPtr context;

        public IntPtr WindowHandle => window.WindowHandle;
        public IntPtr DisplayHandle => window.DisplayHandle;

        public GraphicsSurfaceType Type { get; }

        public SDL2GraphicsSurface(SDL2Window window, GraphicsSurfaceType surfaceType)
        {
            this.window = window;
            Type = surfaceType;

            switch (surfaceType)
            {
                case GraphicsSurfaceType.OpenGL:
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCUM_ALPHA_SIZE, 0);
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
                    break;

                case GraphicsSurfaceType.Vulkan:
                case GraphicsSurfaceType.Metal:
                case GraphicsSurfaceType.Direct3D11:
                    break;

                default:
                    throw new ArgumentException($"Unexpected graphics surface: {Type}.", nameof(surfaceType));
            }
        }

        public void Initialise()
        {
            if (Type == GraphicsSurfaceType.OpenGL)
                initialiseOpenGL();
        }

        public Size GetDrawableSize()
        {
            int width, height;

            switch (Type)
            {
                case GraphicsSurfaceType.OpenGL:
                default:
                    SDL.SDL_GL_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsSurfaceType.Vulkan:
                    SDL.SDL_Vulkan_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsSurfaceType.Metal:
                    SDL.SDL_Metal_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsSurfaceType.Direct3D11:
                    // todo: SDL has no "drawable size" method for D3D11, return window size for now.
                    SDL.SDL_GetWindowSize(window.SDLWindowHandle, out width, out height);
                    break;
            }

            return new Size(width, height);
        }

        #region OpenGL-specific implementation

        private void initialiseOpenGL()
        {
            if (RuntimeInfo.IsMobile)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);

                // Minimum OpenGL version for ES profile:
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            }
            else
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

                // Minimum OpenGL version for core profile:
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            }

            context = SDL.SDL_GL_CreateContext(window.SDLWindowHandle);

            if (context == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create an SDL2 GL context ({SDL.SDL_GetError()})");

            SDL.SDL_GL_MakeCurrent(window.SDLWindowHandle, context);

            loadBindings();
        }

        private void loadBindings()
        {
            loadEntryPoints(new osuTK.Graphics.OpenGL.GL());
            loadEntryPoints(new osuTK.Graphics.OpenGL4.GL());
            loadEntryPoints(new osuTK.Graphics.ES11.GL());
            loadEntryPoints(new osuTK.Graphics.ES20.GL());
            loadEntryPoints(new GL());
        }

        private unsafe void loadEntryPoints(GraphicsBindingsBase bindings)
        {
            var type = bindings.GetType();
            var pointsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointsInstance");
            var namesInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNamesInstance");
            var offsetsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNameOffsetsInstance");

            var entryPointsInstance = (IntPtr[]?)pointsInfo.GetValue(bindings);
            byte[]? entryPointNamesInstance = (byte[]?)namesInfo.GetValue(bindings);
            int[]? entryPointNameOffsetsInstance = (int[]?)offsetsInfo.GetValue(bindings);

            Debug.Assert(entryPointsInstance != null);
            Debug.Assert(entryPointNameOffsetsInstance != null);

            fixed (byte* name = entryPointNamesInstance)
            {
                for (int i = 0; i < entryPointsInstance.Length; i++)
                {
                    byte* ptr = name + entryPointNameOffsetsInstance[i];
                    string? str = Marshal.PtrToStringAnsi(new IntPtr(ptr));

                    Debug.Assert(str != null);
                    entryPointsInstance[i] = getProcAddress(str);
                }
            }

            pointsInfo.SetValue(bindings, entryPointsInstance);
        }

        private IntPtr getProcAddress(string symbol)
        {
            const int error_category = (int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR;
            SDL.SDL_LogPriority oldPriority = SDL.SDL_LogGetPriority(error_category);

            // Prevent logging calls to SDL_GL_GetProcAddress() that fail on systems which don't have the requested symbol (typically macOS).
            SDL.SDL_LogSetPriority(error_category, SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO);

            IntPtr ret = SDL.SDL_GL_GetProcAddress(symbol);

            // Reset the logging behaviour.
            SDL.SDL_LogSetPriority(error_category, oldPriority);

            return ret;
        }

        int? IOpenGLGraphicsSurface.BackbufferFramebuffer
        {
            get
            {
                if (window.SDLWindowHandle == IntPtr.Zero)
                    return null;

                var wmInfo = window.GetWindowSystemInformation();

                switch (wmInfo.subsystem)
                {
                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_UIKIT:
                        return (int)wmInfo.info.uikit.framebuffer;
                }

                return null;
            }
        }

        // cache value locally as requesting from SDL is not free.
        // it is assumed that we are the only thing changing vsync modes.
        private bool? verticalSync;

        bool IOpenGLGraphicsSurface.VerticalSync
        {
            get
            {
                if (verticalSync != null)
                    return verticalSync.Value;

                return (verticalSync = SDL.SDL_GL_GetSwapInterval() != 0).Value;
            }
            set
            {
                if (RuntimeInfo.IsDesktop)
                {
                    SDL.SDL_GL_SetSwapInterval(value ? 1 : 0);
                    verticalSync = value;
                }
            }
        }

        IntPtr IOpenGLGraphicsSurface.WindowContext => context;
        IntPtr IOpenGLGraphicsSurface.CurrentContext => SDL.SDL_GL_GetCurrentContext();

        void IOpenGLGraphicsSurface.SwapBuffers() => SDL.SDL_GL_SwapWindow(window.SDLWindowHandle);
        void IOpenGLGraphicsSurface.CreateContext() => SDL.SDL_GL_CreateContext(window.SDLWindowHandle);
        void IOpenGLGraphicsSurface.DeleteContext(IntPtr context) => SDL.SDL_GL_DeleteContext(context);
        void IOpenGLGraphicsSurface.MakeCurrent(IntPtr context) => SDL.SDL_GL_MakeCurrent(window.SDLWindowHandle, context);
        void IOpenGLGraphicsSurface.ClearCurrent() => SDL.SDL_GL_MakeCurrent(window.SDLWindowHandle, IntPtr.Zero);
        IntPtr IOpenGLGraphicsSurface.GetProcAddress(string symbol) => getProcAddress(symbol);

        #endregion

        #region Metal-specific implementation

        IntPtr IMetalGraphicsSurface.CreateMetalView() => SDL.SDL_Metal_CreateView(window.SDLWindowHandle);

        #endregion

        #region Linux-specific implementation

        bool ILinuxGraphicsSurface.IsWayland => window.IsWayland;

        #endregion
    }
}
