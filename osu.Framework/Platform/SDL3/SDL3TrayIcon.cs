using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Threading;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vulkan.Xlib;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public unsafe class SDL3TrayIcon : IDisposable
    {
        private SDL_Tray* innerTray;
        private SDL_TrayMenu* rootMenu;

        private TrayIcon trayIcon;

        internal SDL3TrayIcon(TrayIcon tray)
        {
            trayIcon = tray;
        }

        internal void Create()
        {
            innerTray = SDL_CreateTray(null, trayIcon.Label);

            if (trayIcon.Menu is null)
                return;

            rootMenu = SDL_CreateTrayMenu(innerTray);
            insertMenu(rootMenu, trayIcon.Menu);
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

                    SDL_TrayEntry* nativeEntry = SDL_InsertTrayEntryAt(menu, -1, button.Label, flags);

                    if (button.Action is not null)
                    {
                        SetCallback(nativeEntry, button.Action);
                    }
                }
                else if (entry is TrayCheckBox checkbox)
                {
                    SDL_TrayEntryFlags flags = SDL_TrayEntryFlags.SDL_TRAYENTRY_CHECKBOX;

                    if (!checkbox.Enabled)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_DISABLED;

                    if (checkbox.Checked)
                        flags |= SDL_TrayEntryFlags.SDL_TRAYENTRY_CHECKED;

                    SDL_TrayEntry* nativeEntry = SDL_InsertTrayEntryAt(menu, -1, checkbox.Label, flags);

                    if (checkbox.Action is not null)
                    {
                        SetCallback(nativeEntry, checkbox.Action);
                    }
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

        protected static void SetCallback(SDL_TrayEntry* entry, Action callback)
        {
            var objectHandle = new ObjectHandle<Action>(callback, GCHandleType.Normal);
            SDL_SetTrayEntryCallback(entry, &nativeOnSelect, objectHandle.Handle); // this is leaking object handles, figure something out
            // ideally store these in a list or something, and dispose them at the right time.
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void nativeOnSelect(IntPtr userdata, SDL_TrayEntry* entry)
        {
            var objectHandle = new ObjectHandle<Action>(userdata, true);

            if (objectHandle.GetTarget(out var action))
                action();
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