//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Forms;

namespace osu.Framework.Desktop.Input
{
    public class ImeTextBox : TextBox
    {
        const int WM_IME_STARTCOMPOSITION = 0x010D;
        const int WM_IME_ENDCOMPOSITION = 0x0010E;
        const int WM_IME_NOTIFY = 0x0282;
        const int WM_CONTEXTMENU = 0x007B;
        const int WM_IME_SETCONTEXT = 0x0281;

        // IMEでキーが押されたかのフラグ
        private const int WM_IME_COMPOSITION = 0x010F;
        // 変換確定後文字取得に使用する値(ひらがな)
        private const int GCS_RESULTSTR = 0x0800;
        // 変換確定後文字取得に使用する値(1バイトカタカナ)
        private const int GCS_RESULTREADSTR = 0x0200;
        // IME入力中文字取得に使用する値(ひらがな)
        private const int GCS_COMPSTR = 0x0008;

        // IME入力中文字取得に使用する値(1バイトカタカナ)
        private const int GCS_COMPREADSTR = 0x0001;

        // wParam of report message WM_IME_NOTIFY
        private const int IMN_CLOSESTATUSWINDOW = 0x0001;
        private const int IMN_OPENSTATUSWINDOW = 0x0002;
        private const int IMN_CHANGECANDIDATE = 0x0003;
        private const int IMN_CLOSECANDIDATE = 0x0004;
        private const int IMN_OPENCANDIDATE = 0x0005;
        private const int IMN_SETCONVERSIONMODE = 0x0006;
        private const int IMN_SETSENTENCEMODE = 0x0007;
        private const int IMN_SETOPENSTATUS = 0x0008;
        private const int IMN_SETCANDIDATEPOS = 0x0009;
        private const int IMN_SETCOMPOSITIONFONT = 0x000A;
        private const int IMN_SETCOMPOSITIONWINDOW = 0x000B;
        private const int IMN_SETSTATUSWINDOWPOS = 0x000C;
        private const int IMN_GUIDELINE = 0x000D;
        private const int IMN_PRIVATE = 0x000E;


        const int WM_DRAWCLIPBOARD = 0x0308;

        private bool imeActive;

        public bool ImeActive => imeActive;

        internal double ImeDeactivateTime;
        internal List<string> Candidates = new List<string>();

        public ImeTextBox()
        {
            TabStop = false;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            switch ((Keys)e.KeyChar)
            {
                case Keys.Enter:
                case Keys.Escape:
                    e.Handled = true;
                    break;
            }

            base.OnKeyPress(e);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CandidateList
        {
            public uint dwSize;
            public uint dwStyle;
            public uint dwCount;
            public uint dwSelection;
            public uint dwPageStart;
            public uint dwPageSize;
            /// DWORD[1]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.U4)]
            public uint[] dwOffset;
        }

        [SuppressUnmanagedCodeSecurity()]
        [DllImport("Imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [SuppressUnmanagedCodeSecurity()]
        [DllImport("imm32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int ImmGetCompositionString(IntPtr hIMC, int CompositionStringFlag, byte[] buffer, int bufferLength);

        [SuppressUnmanagedCodeSecurity()]
        [DllImport("Imm32.dll")]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [SuppressUnmanagedCodeSecurity()]
        [DllImport("imm32.dll", CharSet = CharSet.Auto, EntryPoint = "ImmGetCandidateList")]
        public static extern uint ImmGetCandidateList(IntPtr hIMC, uint deIndex, IntPtr candidateList, uint dwBufLen);

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CONTEXTMENU:
                    return;
                case WM_IME_STARTCOMPOSITION:
                    imeActive = true;

                    OnImeActivity?.Invoke(true);
                    return;
                case WM_IME_ENDCOMPOSITION:
                    imeActive = false;
                    //ImeDeactivateTime = OsuGame.Time;

                    OnImeActivity?.Invoke(false);
                    return;
                case WM_IME_COMPOSITION:
                    string str = string.Empty;

                    if (((int)m.LParam & GCS_RESULTSTR) > 0)
                    {
                        str = getIMEString(GCS_RESULTSTR);
                        OnNewImeResult?.Invoke(str);
                    }

                    if (((int)m.LParam & GCS_COMPSTR) > 0)
                    {
                        str = getIMEString(GCS_COMPSTR);
                        OnNewImeComposition?.Invoke(str);
                    }

                    if (string.IsNullOrEmpty(str))
                        OnNewImeComposition?.Invoke(str);
                    return;
                case WM_IME_SETCONTEXT:
                    m.Result = DefWindowProc(Handle, m.Msg, m.WParam, new IntPtr(~0xC000000F));
                    return;
                case WM_IME_NOTIFY:

                    switch ((int)m.WParam.ToInt32())
                    {
                        case IMN_PRIVATE:
                            return;
                        case IMN_OPENCANDIDATE:
                        case IMN_CHANGECANDIDATE:
                            //CandidateList candidate;
                            //IntPtr ptr;
                            //IntPtr hIMC = ImmGetContext(this.Handle);
                            //uint size = ImmGetCandidateList(hIMC, 0, IntPtr.Zero, 0);
                            //Candidates.Clear();
                            //if (size > 0)
                            //{
                            //    ptr = Marshal.AllocHGlobal((int)size);
                            //    size = ImmGetCandidateList(hIMC, 0, ptr, size);
                            //    candidate = (CandidateList)Marshal.PtrToStructure(ptr, typeof(CandidateList));
                            //    if (candidate.dwCount > 1)
                            //    {
                            //        for (int i = (int)candidate.dwPageStart; i < candidate.dwCount; i++)
                            //        {
                            //            //Notice:only support up to 10 candidates now.
                            //            if (i - candidate.dwPageStart > candidate.dwPageSize - 1)
                            //                break;
                            //            int stringOffset = Marshal.ReadInt32(ptr, 24 + 4 * i);
                            //            IntPtr addr = (IntPtr)(ptr.ToInt32() + stringOffset);
                            //            string caStr = Marshal.PtrToStringUni(addr);
                            //            Candidates.Add(caStr);
                            //        }
                            //    }
                            //    Marshal.FreeHGlobal(ptr);
                            //}
                            //ImmReleaseContext(this.Handle, hIMC);
                            return;
                        case IMN_CLOSECANDIDATE:
                            Candidates.Clear();
                            return;

                    }
                    break;
            }

            base.WndProc(ref m);
        }

        private string getIMEString(int type)
        {
            IntPtr hIMC = ImmGetContext(this.Handle);

            int sz = ImmGetCompositionString(hIMC, type, null, 0);

            byte[] str = new byte[sz];

            ImmGetCompositionString(hIMC, type, str, sz);
            ImmReleaseContext(this.Handle, hIMC);

            return Encoding.Unicode.GetString(str);
        }

        public event Action<string> OnNewImeResult;
        public event Action<string> OnNewImeComposition;
        public event Action<bool> OnImeActivity;
    }
}
