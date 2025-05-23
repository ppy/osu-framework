// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    internal unsafe class SDL3GraphicsSurface : IGraphicsSurface, IOpenGLGraphicsSurface, IMetalGraphicsSurface, ILinuxGraphicsSurface, IAndroidGraphicsSurface
    {
        private readonly SDL3Window window;

        private SDL_GLContextState* context;

        public IntPtr WindowHandle => window.WindowHandle;

        public GraphicsSurfaceType Type { get; }

        public SDL3GraphicsSurface(SDL3Window window, GraphicsSurfaceType surfaceType)
        {
            this.window = window;
            Type = surfaceType;

            switch (surfaceType)
            {
                case GraphicsSurfaceType.OpenGL:
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_RED_SIZE, 8);
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_GREEN_SIZE, 8);
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_BLUE_SIZE, 8);
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_ACCUM_ALPHA_SIZE, 0);
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, 16);
                    SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, 8);
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
            SDL_GetWindowSizeInPixels(window.SDLWindowHandle, &width, &height);
            return new Size(width, height);
        }

        #region OpenGL-specific implementation

        private void initialiseOpenGL()
        {
            if (RuntimeInfo.IsMobile)
            {
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_ES);

                // Minimum OpenGL version for ES profile:
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            }
            else
            {
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE);

                // Minimum OpenGL version for core profile:
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            }

            context = SDL_GL_CreateContext(window.SDLWindowHandle);

            if (context == null)
                throw new InvalidOperationException($"Failed to create an SDL3 GL context ({SDL_GetError()})");

            SDL_GL_MakeCurrent(window.SDLWindowHandle, context);

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

        private void loadEntryPoints(GraphicsBindingsBase bindings)
        {
            var type = bindings.GetType();
            var pointsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointsInstance");
            var namesInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNamesInstance");
            var offsetsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNameOffsetsInstance");

            IntPtr[]? entryPointsInstance = (IntPtr[]?)pointsInfo.GetValue(bindings);
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
            const SDL_LogCategory error_category = SDL_LogCategory.SDL_LOG_CATEGORY_ERROR;
            SDL_LogPriority oldPriority = SDL_GetLogPriority(error_category);

            // Prevent logging calls to SDL_GL_GetProcAddress() that fail on systems which don't have the requested symbol (typically macOS).
            SDL_SetLogPriority(error_category, SDL_LogPriority.SDL_LOG_PRIORITY_INFO);

            IntPtr ret = SDL_GL_GetProcAddress(symbol);

            // Reset the logging behaviour.
            SDL_SetLogPriority(error_category, oldPriority);

            return ret;
        }

        int? IOpenGLGraphicsSurface.BackbufferFramebuffer
        {
            get
            {
                if (window.SDLWindowHandle == null)
                    return null;

                var props = SDL_GetWindowProperties(window.SDLWindowHandle);

                if (SDL_HasProperty(props, SDL_PROP_WINDOW_UIKIT_OPENGL_FRAMEBUFFER_NUMBER))
                    return (int)SDL_GetNumberProperty(props, SDL_PROP_WINDOW_UIKIT_OPENGL_FRAMEBUFFER_NUMBER, 0);

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

                int interval;
                SDL_GL_GetSwapInterval(&interval);
                return (verticalSync = interval != 0).Value;
            }
            set
            {
                if (RuntimeInfo.IsDesktop)
                {
                    SDL_GL_SetSwapInterval(value ? 1 : 0);
                    verticalSync = value;
                }
            }
        }

        IntPtr IOpenGLGraphicsSurface.WindowContext => (IntPtr)context;
        IntPtr IOpenGLGraphicsSurface.CurrentContext => (IntPtr)SDL_GL_GetCurrentContext();

        void IOpenGLGraphicsSurface.SwapBuffers() => SDL_GL_SwapWindow(window.SDLWindowHandle);
        void IOpenGLGraphicsSurface.CreateContext() => SDL_GL_CreateContext(window.SDLWindowHandle);
        void IOpenGLGraphicsSurface.DeleteContext(IntPtr context) => SDL_GL_DestroyContext((SDL_GLContextState*)context);
        void IOpenGLGraphicsSurface.MakeCurrent(IntPtr context) => SDL_GL_MakeCurrent(window.SDLWindowHandle, (SDL_GLContextState*)context);
        void IOpenGLGraphicsSurface.ClearCurrent() => SDL_GL_MakeCurrent(window.SDLWindowHandle, null);
        IntPtr IOpenGLGraphicsSurface.GetProcAddress(string symbol) => getProcAddress(symbol);

        #endregion

        #region Metal-specific implementation

        IntPtr IMetalGraphicsSurface.CreateMetalView() => SDL_Metal_CreateView(window.SDLWindowHandle);

        #endregion

        #region Linux-specific implementation

        bool ILinuxGraphicsSurface.IsWayland => window.IsWayland;

        [SupportedOSPlatform("linux")]
        IntPtr ILinuxGraphicsSurface.DisplayHandle => window.DisplayHandle;

        #endregion

        #region Android-specific implementation

        [SupportedOSPlatform("android")]
        IntPtr IAndroidGraphicsSurface.JniEnvHandle => SDL_GetAndroidJNIEnv();

        [SupportedOSPlatform("android")]
        IntPtr IAndroidGraphicsSurface.SurfaceHandle => window.SurfaceHandle;

        #endregion
    }
}
