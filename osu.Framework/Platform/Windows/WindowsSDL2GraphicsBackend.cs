// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;
using osu.Framework.Platform.SDL2;
using osu.Framework.Platform.Windows.Native.NVAPI;
using osuTK.Graphics.ES30;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A version of <see cref="SDL2GraphicsBackend"/> that configures NVIDIA drivers on Windows for better performance.
    /// </summary>
    public class WindowsSDL2GraphicsBackend : SDL2GraphicsBackend
    {
        public override void InitialiseBeforeWindowCreation()
        {
            // Try to initialize NVAPI
            // It's important to do this before graphics API initialization to make sure that the dedicated GPU is being used
            NvLibrary.TryInitialize();
            base.InitialiseBeforeWindowCreation();
        }

        public override void Initialise(IWindow window)
        {
            base.Initialise(window);

            // Set the current GL context for this thread
            MakeCurrent();

            // Check if we're running on a NVIDIA GPU
            if (GL.GetString(StringName.Version).Contains("NVIDIA"))
            {
                Logger.Log("NVIDIA GPU detected. Attempting to disable threaded optimization...");

                try
                {
                    if (NvLibrary.Initialized)
                    {
                        // Open a driver settings session
                        using (var drs = new NvLibrary.DriverSettingsSession())
                        {
                            // Disable threaded optimization
                            IntPtr profileHandle = drs.LoadProfile("osu!framework");
                            drs.SetU32Setting(profileHandle, NvSettingId.OGL_THREAD_CONTROL_ID, 2); // 2 = Disabled

                            // Save the new settings
                            drs.SaveSettings();
                        }

                        Logger.Log("Successfully disabled threaded optimization!");
                    }
                    else
                    {
                        Logger.Log("Couldn't disable threaded optimization because NVAPI couldn't be initialized", level: LogLevel.Error);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Couldn't disable threaded optimization because of an exception");
                }
            }

            // Release the GL context
            // See PassthroughGraphicsBackend.Initialise
            ClearCurrent();
        }
    }
}
