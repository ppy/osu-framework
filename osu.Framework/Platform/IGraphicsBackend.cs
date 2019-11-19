// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    public interface IGraphicsBackend
    {
        bool VerticalSync { get; set; }

        void Initialise(IWindowBackend windowBackend);

        void SwapBuffers();

        void MakeCurrent();
    }
}
