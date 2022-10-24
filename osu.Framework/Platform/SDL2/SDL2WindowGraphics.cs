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
    public class SDL2WindowGraphics : IWindowGraphics, IOpenGLWindowGraphics, IMetalWindowGraphics
    {
        private readonly SDL2DesktopWindow window;

        private IntPtr context;

        public IntPtr WindowHandle => window.WindowHandle;
        public IntPtr DisplayHandle => window.DisplayHandle;

        public GraphicsBackend BackendType { get; }

        public SDL2WindowGraphics(SDL2DesktopWindow window, GraphicsBackend backend)
        {
            this.window = window;

            BackendType = backend;

            if (backend == GraphicsBackend.OpenGL)
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
        }

        public void Initialise()
        {
            if (BackendType == GraphicsBackend.OpenGL)
                initialiseOpenGL();
        }

        public Size GetDrawableSize()
        {
            int width, height;

            switch (BackendType)
            {
                case GraphicsBackend.OpenGL:
                    SDL.SDL_GL_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Vulkan:
                    SDL.SDL_Vulkan_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Metal:
                    SDL.SDL_Metal_GetDrawableSize(window.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Direct3D11:
                    // todo: SDL has no "drawable size" method for D3D11, return window size for now.
                    SDL.SDL_GetWindowSize(window.SDLWindowHandle, out width, out height);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected graphics backend: {BackendType}.");
            }

            return new Size(width, height);
        }

        #region OpenGL-specific implementation

        private void initialiseOpenGL()
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY);

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

        bool IOpenGLWindowGraphics.VerticalSync
        {
            get => SDL.SDL_GL_GetSwapInterval() != 0;
            set => SDL.SDL_GL_SetSwapInterval(value ? 1 : 0);
        }

        IntPtr IOpenGLWindowGraphics.WindowContext => context;
        IntPtr IOpenGLWindowGraphics.CurrentContext => SDL.SDL_GL_GetCurrentContext();

        void IOpenGLWindowGraphics.SwapBuffers() => SDL.SDL_GL_SwapWindow(window.SDLWindowHandle);
        void IOpenGLWindowGraphics.CreateContext() => SDL.SDL_GL_CreateContext(window.SDLWindowHandle);
        void IOpenGLWindowGraphics.DeleteContext(IntPtr context) => SDL.SDL_GL_DeleteContext(context);
        void IOpenGLWindowGraphics.MakeCurrent(IntPtr context) => SDL.SDL_GL_MakeCurrent(window.SDLWindowHandle, context);
        void IOpenGLWindowGraphics.ClearCurrent() => SDL.SDL_GL_MakeCurrent(window.SDLWindowHandle, IntPtr.Zero);
        IntPtr IOpenGLWindowGraphics.GetProcAddress(string symbol) => getProcAddress(symbol);

        #endregion

        #region Metal-specific implementation

        IntPtr IMetalWindowGraphics.CreateMetalView() => SDL.SDL_Metal_CreateView(window.SDLWindowHandle);

        #endregion
    }
}
