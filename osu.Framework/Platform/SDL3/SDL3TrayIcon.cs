using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public class SDL3TrayIcon : IDisposable
    {
        internal SDL.SDL_TrayIcon* icon;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}