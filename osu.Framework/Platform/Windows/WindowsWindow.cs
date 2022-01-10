// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using osu.Framework.Platform.SDL2;
using osu.Framework.Platform.Windows.Native;
using SDL2;

namespace osu.Framework.Platform.Windows
{
    public class WindowsWindow : SDL2DesktopWindow
    {
        private const int seticon_message = 0x0080;
        private const int icon_big = 1;
        private const int icon_small = 0;

        private const int large_icon_size = 256;
        private const int small_icon_size = 16;

        private Icon smallIcon;
        private Icon largeIcon;

        public WindowsWindow()
        {
            try
            {
                // SDL doesn't handle DPI correctly on windows, but this brings things mostly in-line with expectations. (https://bugzilla.libsdl.org/show_bug.cgi?id=3281)
                SetProcessDpiAwareness(ProcessDpiAwareness.Process_Per_Monitor_DPI_Aware);
            }
            catch
            {
                // API doesn't exist on Windows 7 so it needs to be allowed to fail silently.
            }
        }

        public override void Create()
        {
            base.Create();

            // enable window message events to use with `OnSDLEvent` below.
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_SYSWMEVENT, SDL.SDL_ENABLE);

            OnSDLEvent += handleSDLEvent;
        }

        #region IME handling

        private void handleSDLEvent(SDL.SDL_Event e)
        {
            if (e.type != SDL.SDL_EventType.SDL_SYSWMEVENT) return;

            var wmMsg = Marshal.PtrToStructure<SDL2Structs.SDL_SysWMmsg>(e.syswm.msg);
            var m = wmMsg.msg.win;

            switch (m.msg)
            {
                case Imm.WM_IME_STARTCOMPOSITION:
                case Imm.WM_IME_COMPOSITION:
                case Imm.WM_IME_ENDCOMPOSITION:
                    handleImeMessage(m.hwnd, m.msg, m.lParam);
                    break;
            }
        }

        public override void StartTextInput(bool allowIme)
        {
            base.StartTextInput(allowIme);
            ScheduleCommand(() => Imm.SetImeAllowed(WindowHandle, allowIme));
        }

        public override void ResetIme() => ScheduleCommand(() => Imm.CancelComposition(WindowHandle));

        protected override void HandleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            // block SDL text input if there was a recent result from `handleImeMessage()`.
            if (recentImeResult)
            {
                recentImeResult = false;
                return;
            }

            // also block if there is an ongoing composition (unlikely to occur).
            if (imeCompositionActive) return;

            base.HandleTextInputEvent(evtText);
        }

        protected override void HandleTextEditingEvent(SDL.SDL_TextEditingEvent evtEdit)
        {
            // handled by custom logic below
        }

        /// <summary>
        /// Whether IME composition is active.
        /// </summary>
        /// <remarks>Used for blocking SDL IME results since we handle those ourselves.</remarks>
        private bool imeCompositionActive;

        /// <summary>
        /// Whether an IME result was recently posted.
        /// </summary>
        /// <remarks>Used for blocking SDL IME results since we handle those ourselves.</remarks>
        private bool recentImeResult;

        private void handleImeMessage(IntPtr hWnd, uint uMsg, long lParam)
        {
            switch (uMsg)
            {
                case Imm.WM_IME_STARTCOMPOSITION:
                    imeCompositionActive = true;
                    ScheduleEvent(() => TriggerTextEditing(string.Empty, 0, 0));
                    break;

                case Imm.WM_IME_COMPOSITION:
                    using (var inputContext = new Imm.InputContext(hWnd, lParam))
                    {
                        if (inputContext.TryGetImeResult(out string resultText))
                        {
                            recentImeResult = true;
                            ScheduleEvent(() => TriggerTextInput(resultText));
                        }

                        if (inputContext.TryGetImeComposition(out string compositionText, out int start, out int length))
                        {
                            ScheduleEvent(() => TriggerTextEditing(compositionText, start, length));
                        }
                    }

                    break;

                case Imm.WM_IME_ENDCOMPOSITION:
                    imeCompositionActive = false;
                    ScheduleEvent(() => TriggerTextEditing(string.Empty, 0, 0));
                    break;
            }
        }

        #endregion

        protected override Size SetBorderless()
        {
            SDL.SDL_SetWindowBordered(SDLWindowHandle, SDL.SDL_bool.SDL_FALSE);

            Size positionOffsetHack = new Size(1, 1);

            var newSize = CurrentDisplay.Bounds.Size + positionOffsetHack;
            var newPosition = CurrentDisplay.Bounds.Location - positionOffsetHack;

            // for now let's use the same 1px hack that we've always used to force borderless.
            SDL.SDL_SetWindowSize(SDLWindowHandle, newSize.Width, newSize.Height);
            Position = newPosition;

            return newSize;
        }

        /// <summary>
        /// On Windows, SDL will use the same image for both large and small icons (scaled as necessary).
        /// This can look bad if scaling down a large image, so we use the Windows API directly so as
        /// to get a cleaner icon set than SDL can provide.
        /// If called before the window has been created, or we do not find two separate icon sizes, we fall back to the base method.
        /// </summary>
        internal override void SetIconFromGroup(IconGroup iconGroup)
        {
            smallIcon = iconGroup.CreateIcon(small_icon_size, small_icon_size);
            largeIcon = iconGroup.CreateIcon(large_icon_size, large_icon_size);

            var windowHandle = WindowHandle;

            if (windowHandle == IntPtr.Zero || largeIcon == null || smallIcon == null)
                base.SetIconFromGroup(iconGroup);
            else
            {
                SendMessage(windowHandle, seticon_message, (IntPtr)icon_small, smallIcon.Handle);
                SendMessage(windowHandle, seticon_message, (IntPtr)icon_big, largeIcon.Handle);
            }
        }

        public override Point PointToClient(Point point)
        {
            ScreenToClient(WindowHandle, ref point);
            return point;
        }

        public override Point PointToScreen(Point point)
        {
            ClientToScreen(WindowHandle, ref point);
            return point;
        }

        [DllImport("SHCore.dll", SetLastError = true)]
        internal static extern bool SetProcessDpiAwareness(ProcessDpiAwareness awareness);

        internal enum ProcessDpiAwareness
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref Point point);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref Point point);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
