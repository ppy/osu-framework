// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Platform.Windows.Native
{
    /// <summary>
    /// Static class for interacting with the Input Method Manager,
    /// the interface between applications and the IME.
    /// </summary>
    internal static class Imm
    {
        /// <summary>
        /// Cancels the currently active IME composition (if any).
        /// Resets the internal composition string and hides the candidate window.
        /// </summary>
        internal static void CancelComposition(IntPtr hWnd)
        {
            using (var inputContext = new InputContext(hWnd))
            {
                inputContext.CancelComposition();
            }
        }

        /// <summary>
        /// Sets whether IME is allowed.
        /// </summary>
        /// <remarks>
        /// If IME is disallowed, text input is active and a language that uses IME is selected,
        /// then IME will be forced into alphanumeric mode, behaving as normal keyboard input (no compositing will happen).
        /// </remarks>
        internal static void SetImeAllowed(IntPtr hWnd, bool allowed)
        {
            ImmAssociateContextEx(hWnd, IntPtr.Zero, allowed ? AssociationFlags.IACE_DEFAULT : AssociationFlags.IACE_IGNORENOCONTEXT);
        }

        /// <summary>
        /// Provides an IMC (Input Method Context) allowing interaction with the IMM (Input Method Manager).
        /// </summary>
        internal class InputContext : IDisposable
        {
            /// <summary>
            /// The IMM handle, used as the <c>hImc</c> param of native functions.
            /// </summary>
            private readonly InputContextHandle handle;

            private readonly CompositionString lParam;

            public InputContext(IntPtr hWnd, long lParam = 0)
            {
                handle = new InputContextHandle(hWnd);
                this.lParam = (CompositionString)lParam;
            }

            /// <summary>
            /// Gets the current composition text and selection.
            /// </summary>
            public bool TryGetImeComposition(out string compositionText, out int start, out int length)
            {
                if (handleInvalidOrClosed())
                {
                    start = 0;
                    length = 0;
                    compositionText = null;
                    return false;
                }

                if (!tryGetCompositionText(CompositionString.GCS_COMPSTR, out compositionText))
                {
                    start = 0;
                    length = 0;
                    return false;
                }

                if (tryGetCompositionTargetRange(out int targetStart, out int targetEnd))
                {
                    start = targetStart;
                    length = targetEnd - targetStart;
                    return true;
                }

                // couldn't get selection length, so default to 0
                length = 0;

                if (tryGetCompositionSize(CompositionString.GCS_CURSORPOS, out int cursorPosition))
                {
                    start = cursorPosition;
                    return true;
                }

                // couldn't get selection start, so default to end of string.
                start = compositionText.Length;
                return true;
            }

            /// <summary>
            /// Gets the current result text.
            /// </summary>
            public bool TryGetImeResult(out string resultText)
            {
                if (handleInvalidOrClosed())
                {
                    resultText = null;
                    return false;
                }

                return tryGetCompositionText(CompositionString.GCS_RESULTSTR, out resultText);
            }

            /// <summary>
            /// Cancels the currently active IME composition (if any).
            /// </summary>
            /// <remarks>
            /// Resets the internal composition string and hides the candidate window.
            /// </remarks>
            public void CancelComposition()
            {
                if (handleInvalidOrClosed()) return;

                ImmNotifyIME(handle, NotificationCode.NI_COMPOSITIONSTR, (uint)CompositionStringAction.CPS_CANCEL, 0);
            }

            /// <summary>
            /// Get the <paramref name="size"/> of the corresponding <paramref name="compositionString"/> from the IMM.
            /// </summary>
            /// <remarks>
            /// The size has a different meaning, depending on the provided <paramref name="compositionString"/>:
            ///   <list type="bullet">
            ///     <item>For most <see cref="CompositionString"/>s, returns the size of buffer required to store the data.</item>
            ///     <item>For <see cref="CompositionString.GCS_CURSORPOS"/>, returns the cursor position in the current composition text.</item>
            ///   </list>
            /// </remarks>
            private bool tryGetCompositionSize(CompositionString compositionString, out int size)
            {
                size = -1;

                if (!lParam.HasFlagFast(compositionString))
                    return false;

                size = ImmGetCompositionString(handle, compositionString, null, 0);

                // negative return value means that an error has occured.
                return size >= 0;
            }

            /// <summary>
            /// Get the <paramref name="size"/> and <paramref name="data"/> of the corresponding <paramref name="compositionString"/> from the IMM.
            /// </summary>
            /// <remarks>
            /// The <paramref name="size"/> and <paramref name="data"/> have different meanings, depending on the provided <paramref name="compositionString"/>:
            ///   <list type="bullet">
            ///     <item>For <see cref="CompositionString.GCS_COMPSTR"/> and <see cref="CompositionString.GCS_RESULTSTR"/> data is UTF-16 encoded text.</item>
            ///     <item>For <see cref="CompositionString.GCS_COMPATTR"/> .</item>
            ///   </list>
            /// </remarks>
            private bool tryGetCompositionString(CompositionString compositionString, out int size, out byte[] data)
            {
                data = null;

                if (!tryGetCompositionSize(compositionString, out size))
                    return false;

                data = new byte[size];
                int ret = ImmGetCompositionString(handle, compositionString, data, (uint)size);

                // negative return value means that an error has occured.
                return ret >= 0;
            }

            /// <summary>
            /// Gets the text of the current composition (<see cref="CompositionString.GCS_COMPSTR"/>) or result (<see cref="CompositionString.GCS_RESULTSTR"/>).
            /// </summary>
            private bool tryGetCompositionText(CompositionString compositionString, out string text)
            {
                if (tryGetCompositionString(compositionString, out _, out byte[] buffer))
                {
                    text = Encoding.Unicode.GetString(buffer);
                    return true;
                }

                text = null;
                return false;
            }

            /// <summary>
            /// Determines whether or not the given attribute represents a target (a.k.a. a selection).
            /// </summary>
            private bool isTargetAttribute(byte attribute) => attribute == (byte)Attribute.ATTR_TARGET_CONVERTED || attribute == (byte)Attribute.ATTR_TARGET_NOTCONVERTED;

            /// <summary>
            /// Gets the target range that's selected by the user in the current composition string.
            /// </summary>
            private bool tryGetCompositionTargetRange(out int targetStart, out int targetEnd)
            {
                targetStart = 0;
                targetEnd = 0;

                if (!tryGetCompositionString(CompositionString.GCS_COMPATTR, out int size, out byte[] attributeData))
                    return false;

                int start;
                int end;
                bool targetFound = false;

                // find the first character that is part of the current conversion.
                for (start = 0; start < size; start++)
                {
                    if (isTargetAttribute(attributeData[start]))
                    {
                        targetFound = true;
                        break;
                    }
                }

                if (!targetFound)
                    return false;

                // find the last character that is part of the current conversion.
                for (end = start; end < size; end++)
                {
                    if (!isTargetAttribute(attributeData[end]))
                        break;
                }

                targetStart = start;
                targetEnd = end;
                return true;
            }

            /// <summary>
            /// Checks whether the <see cref="handle"/> is invalid or closed.
            /// </summary>
            /// <remarks>Should be checked before calling native functions with the <see cref="handle"/>.</remarks>
            /// <returns><c>true</c> if the handle is invalid.</returns>
            /// <exception cref="ObjectDisposedException">Thrown if the <see cref="handle"/> was disposed/closed.</exception>
            private bool handleInvalidOrClosed()
            {
                if (handle.IsClosed)
                    throw new ObjectDisposedException(handle.ToString(), $"Attempted to use a closed {nameof(InputContextHandle)}.");

                return handle.IsInvalid;
            }

            public void Dispose()
            {
                handle.Dispose();
            }
        }

        #region Native functions and handles

        private class InputContextHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("imm32.dll", SetLastError = true)]
            private static extern IntPtr ImmGetContext(IntPtr hWnd);

            [DllImport("imm32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);

            private readonly IntPtr windowHandle;

            public InputContextHandle(IntPtr windowHandle)
                : base(true)
            {
                if (windowHandle == IntPtr.Zero)
                    throw new ArgumentException($"Invalid {nameof(windowHandle)}");

                this.windowHandle = windowHandle;
                SetHandle(ImmGetContext(windowHandle));
            }

            protected override bool ReleaseHandle()
            {
                return ImmReleaseContext(windowHandle, handle);
            }
        }

        // ReSharper disable IdentifierTypo

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        private static extern int ImmGetCompositionString(InputContextHandle hImc, CompositionString dwIndex, byte[] lpBuf, uint dwBufLen);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmNotifyIME(InputContextHandle hImc, NotificationCode dwAction, uint dwIndex, uint dwValue);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmAssociateContextEx(IntPtr hWnd, IntPtr hImc, AssociationFlags dwFlags);

        // window messages
        internal const int WM_IME_STARTCOMPOSITION = 0x010D;
        internal const int WM_IME_ENDCOMPOSITION = 0x010E;
        internal const int WM_IME_COMPOSITION = 0x010F;

        /// <summary>
        /// IME composition string values.
        /// <c>lParam</c> values of <see cref="WM_IME_COMPOSITION"/> event.
        /// Parameter <c>dwIndex</c> of <see cref="ImmGetCompositionString"/>.
        /// </summary>
        [Flags]
        private enum CompositionString : uint
        {
            /// <summary>
            /// Retrieve or update the reading string of the current composition.
            /// </summary>
            GCS_COMPREADSTR = 0x0001,

            /// <summary>
            /// Retrieve or update the <see cref="Attribute"/>s of the reading string of the current composition.
            /// </summary>
            GCS_COMPREADATTR = 0x0002,

            /// <summary>
            /// Retrieve or update the clause information of the reading string of the composition string.
            /// </summary>
            GCS_COMPREADCLAUSE = 0x0004,

            /// <summary>
            /// Retrieve or update the current composition string.
            /// </summary>
            GCS_COMPSTR = 0x0008,

            /// <summary>
            /// Retrieve or update the <see cref="Attribute"/>s of the composition string.
            /// </summary>
            GCS_COMPATTR = 0x0010,

            /// <summary>
            /// Retrieve or update clause information of the composition string.
            /// </summary>
            GCS_COMPCLAUSE = 0x0020,

            /// <summary>
            /// Retrieve or update the cursor position in composition string.
            /// </summary>
            GCS_CURSORPOS = 0x0080,

            /// <summary>
            /// Retrieve or update the starting position of any changes in composition string.
            /// </summary>
            GCS_DELTASTART = 0x0100,

            /// <summary>
            /// Retrieve or update the reading string.
            /// </summary>
            GCS_RESULTREADSTR = 0x0200,

            /// <summary>
            /// Retrieve or update clause information of the result string.
            /// </summary>
            GCS_RESULTREADCLAUSE = 0x0400,

            /// <summary>
            /// Retrieve or update the string of the composition result.
            /// </summary>
            GCS_RESULTSTR = 0x0800,

            /// <summary>
            /// Retrieve or update clause information of the result string.
            /// </summary>
            GCS_RESULTCLAUSE = 0x1000,

            /// <summary>
            /// Insert the wParam composition character at the current insertion point.
            /// An application should display the composition character if it processes this message.
            /// </summary>
            CS_INSERTCHAR = 0x2000,

            /// <summary>
            /// Do not move the caret position as a result of processing the message.
            /// </summary>
            CS_NOMOVECARET = 0x4000,
        }

        /// <summary>
        /// Attribute for each character in the current composition string (<see cref="CompositionString.GCS_COMPSTR"/>).
        /// </summary>
        private enum Attribute : byte
        {
            /// <summary>
            /// Character being entered by the user. The IME has yet to convert this character.
            /// </summary>
            ATTR_INPUT = 0x00,

            /// <summary>
            /// Character selected by the user and then converted by the IME.
            /// </summary>
            ATTR_TARGET_CONVERTED = 0x01,

            /// <summary>
            /// Character that the IME has already converted.
            /// </summary>
            ATTR_CONVERTED = 0x02,

            /// <summary>
            /// Character being converted. The user has selected this character but the IME has not yet converted it.
            /// </summary>
            ATTR_TARGET_NOTCONVERTED = 0x03,

            /// <summary>
            /// An error character that the IME cannot convert. For example, the IME cannot put together some consonants.
            /// </summary>
            ATTR_INPUT_ERROR = 0x04,

            /// <summary>
            /// Character that the IME will no longer convert.
            /// </summary>
            ATTR_FIXEDCONVERTED = 0x05,
        }

        /// <summary>
        /// dwAction for <see cref="ImmNotifyIME"/>.
        /// </summary>
        private enum NotificationCode : uint
        {
            /// <summary>
            /// An application directs the IME to open a candidate list.
            /// The dwIndex parameter specifies the index of the list to open, and dwValue is not used.
            /// </summary>
            NI_OPENCANDIDATE = 0x0010,

            /// <summary>
            /// An application directs the IME to close a candidate list.
            /// The dwIndex parameter specifies an index of the list to close, and dwValue is not used.
            /// </summary>
            NI_CLOSECANDIDATE = 0x0011,

            /// <summary>
            /// An application has selected one of the candidates.
            /// The dwIndex parameter specifies an index of a candidate list to be selected.
            /// The dwValue parameter specifies an index of a candidate string in the selected candidate list.
            /// </summary>
            NI_SELECTCANDIDATESTR = 0x0012,

            /// <summary>
            /// An application changed the current selected candidate.
            /// The dwIndex parameter specifies an index of a candidate list to be selected and dwValue is not used.
            /// </summary>
            NI_CHANGECANDIDATELIST = 0x0013,

            NI_FINALIZECONVERSIONRESULT = 0x0014,

            /// <summary>
            /// An application directs the IME to carry out an action on the composition string.
            /// The dwIndex parameter can be from <see cref="CompositionStringAction"/>.
            /// </summary>
            NI_COMPOSITIONSTR = 0x0015,

            /// <summary>
            /// The application changes the page starting index of a candidate list.
            /// The dwIndex parameter specifies the candidate list to be changed and must have a value in the range 0 to 3.
            /// The dwValue parameter specifies the new page start index.
            /// </summary>
            NI_SETCANDIDATE_PAGESTART = 0x0016,

            /// <summary>
            /// The application changes the page size of a candidate list.
            /// The dwIndex parameter specifies the candidate list to be changed and must have a value in the range 0 to 3.
            /// The dwValue parameter specifies the new page size.
            /// </summary>
            NI_SETCANDIDATE_PAGESIZE = 0x0017,

            /// <summary>
            /// An application directs the IME to allow the application to handle the specified menu.
            /// The dwIndex parameter specifies the ID of the menu and dwValue is an application-defined value for that menu item.
            /// </summary>
            NI_IMEMENUSELECTED = 0x0018,
        }

        /// <summary>
        /// dwIndex for <see cref="ImmNotifyIME"/> when using <see cref="NotificationCode.NI_COMPOSITIONSTR"/>.
        /// </summary>
        private enum CompositionStringAction : uint
        {
            /// <summary>
            /// Set the composition string as the result string.
            /// </summary>
            CPS_COMPLETE = 0x0001,

            /// <summary>
            /// Convert the composition string.
            /// </summary>
            CPS_CONVERT = 0x0002,

            /// <summary>
            /// Cancel the current composition string and set the composition string to be the unconverted string.
            /// </summary>
            CPS_REVERT = 0x0003,

            /// <summary>
            /// Clear the composition string and set the status to no composition string.
            /// </summary>
            CPS_CANCEL = 0x0004,
        }

        /// <summary>
        /// Flags specifying the type of association between the window and the input method context.
        /// dwFlags for <see cref="ImmAssociateContextEx"/>.
        /// </summary>
        private enum AssociationFlags : uint
        {
            /// <summary>
            /// Associate the input method context to the child windows of the specified window only.
            /// </summary>
            IACE_CHILDREN = 0x0001,

            /// <summary>
            /// Restore the default input method context of the window.
            /// </summary>
            IACE_DEFAULT = 0x0010,

            /// <summary>
            /// Do not associate the input method context with windows that are not associated with any input method context.
            /// </summary>
            IACE_IGNORENOCONTEXT = 0x0020,
        }

        // ReSharper restore IdentifierTypo

        #endregion
    }
}
