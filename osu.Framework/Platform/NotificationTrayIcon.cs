using System;
using osu.Framework.Platform.Windows;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents an icon located in the OS notification tray.
    /// </summary>
    public abstract class NotificationTrayIcon : IDisposable
    {
        /// <summary>
        /// The hint text shown when hovering over the icon with the cursor
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// The action to perform when the icon gets clicked
        /// </summary>
        public Action? OnClick { get; init; }

        public static NotificationTrayIcon Create(string text, Action? onClick, IWindow window)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return new WindowsNotificationTrayIcon(text, onClick, window);
            }

            throw new PlatformNotSupportedException();
        }

        public abstract void Dispose();
    }
}
