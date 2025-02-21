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
using System.Transactions;

namespace osu.Framework.Platform.SDL3
{
    public unsafe class SDL3TrayIcon : IDisposable
    {
        private SDL_Tray* innerTray;
        private SDL_TrayMenu* rootMenu;

        internal SDL3TrayIcon(TrayIcon tray)
        {
            innerTray = SDL_CreateTray(null, tray.Label);

            if (tray.Menu is null)
                return;

            rootMenu = SDL_CreateTrayMenu(innerTray);
            insertMenu(rootMenu, tray.Menu);
        }

        private void insertMenu(SDL_TrayMenu* menu, TrayMenuEntry[] entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry is TrayButton button)
                {
                    SDL_TrayEntryFlags flags = SDL_TrayEntryFlags.SDL_TRAYENTRY_BUTTON;

                    if (!button.Enabled)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_DISABLED;

                    SDL_InsertTrayEntryAt(menu, -1, button.Label, flags);
                    // TODO: add callback
                }
                else if (entry is TrayCheckBox checkbox)
                {
                    SDL_TrayEntryFlags flags = SDL_TrayEntryFlags.SDL_TRAYENTRY_CHECKBOX;

                    if (!checkbox.Enabled)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_DISABLED;

                    if (checkbox.Checked)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_CHECKED;

                    SDL_InsertTrayEntryAt(menu, -1, checkbox.Label, flags);
                    // TODO: add callback
                }
                else if (entry is TraySeparator)
                {
                    // a button/checkmark with a null label is a tray separator
                    SDL_InsertTrayEntryAt(menu, -1, (Utf8String)null, SDL_TrayEntryFlags.SDL_TRAYENTRY_BUTTON);
                }
                else if (entry is TraySubMenu submenu)
                {
                    SDL_TrayEntryFlags flags = SDL_TrayEntryFlags.SDL_TRAYENTRY_SUBMENU;

                    if (!submenu.Enabled)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_DISABLED;

                    SDL_TrayMenu* smenu = (SDL_TrayMenu*)SDL_InsertTrayEntryAt(menu, -1, submenu.Label, flags);

                    if (submenu.Menu is not null)
                    {
                        insertMenu(smenu, submenu.Menu);
                    }
                }
            }
        }

        ~SDL3TrayIcon()
        {
            SDL_DestroyTray(innerTray);
        }

        public void Dispose()
        {
            SDL_DestroyTray(innerTray);
            GC.SuppressFinalize(this);
        }
    }
}