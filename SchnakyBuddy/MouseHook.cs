using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SchnakyBuddy
{
    public static class MouseHook
    {
        public static event EventHandler MouseAction_WM_LBUTTONDOWN = delegate { };
        public static event EventHandler MouseAction_WM_LBUTTONUP = delegate { };

        public static void Start() => _hookID = SetHook(_proc);
        public static void stop() => UnhookWindowsHookEx(_hookID);

        private static readonly LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                var hook = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle("user32"), 0);
                if (hook == IntPtr.Zero)
                {
                    throw new System.ComponentModel.Win32Exception();
                }
                return hook;
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
          int nCode, IntPtr wParam, IntPtr lParam)
        {
            Debug.WriteLine(((int)wParam).ToString("x"));
            if ((nCode >= 0) && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            {
                var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                new Task(() =>
                {
                    MouseAction_WM_LBUTTONDOWN(null, new EventArgs());
                }).Start();
            }
            if ((nCode >= 0) && MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
            {
                var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                new Task(() =>
                {
                    MouseAction_WM_LBUTTONUP(null, new EventArgs());
                }).Start();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
          LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
          IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
