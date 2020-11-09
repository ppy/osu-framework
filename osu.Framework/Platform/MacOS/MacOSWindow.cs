// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.MacOS
{
    /// <summary>
    /// macOS-specific subclass of <see cref="DesktopWindow"/> that specifies a custom
    /// <see cref="IWindowBackend"/>.
    /// </summary>
    public class MacOSWindow : DesktopWindow
    {
        protected override IWindowBackend CreateWindowBackend() => new MacOSWindowBackend();
    }
}
