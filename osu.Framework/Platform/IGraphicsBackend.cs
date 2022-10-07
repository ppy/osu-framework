// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Platform
{
    /// <summary>
    /// Provides an implementation-agnostic interface on the backing graphics API.
    /// </summary>
    public interface IGraphicsBackend
    {
        /// <summary>
        /// Whether buffer swapping should be synced to the monitor's refresh rate.
        /// </summary>
        bool VerticalSync { get; set; }

        /// <summary>
        /// Initialises everything needed to create a window such as backbuffer parameters.
        /// </summary>
        void InitialiseBeforeWindowCreation();

        /// <summary>
        /// Initialises the graphics backend, given the current window backend.
        /// It is assumed that the window backend has been initialised.
        /// </summary>
        /// <param name="window">The <see cref="IWindow"/> being used for display.</param>
        void Initialise(IWindow window);

        /// <summary>
        /// Performs a backbuffer swap immediately if <see cref="VerticalSync"/> is false,
        /// or on the next screen refresh if true.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Makes the graphics backend the current context, if appropriate for the driver.
        /// </summary>
        void MakeCurrent();

        /// <summary>
        /// Clears the current context, if appropriate for the driver.
        /// </summary>
        void ClearCurrent();
    }
}
