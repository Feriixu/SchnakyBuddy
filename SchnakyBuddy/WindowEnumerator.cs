using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SchnakyBuddy
{
    internal struct WindowInfo
    {
        public string name;
        public IntPtr windowHandle;
    }

    internal static class WindowEnumerator
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static List<WindowInfo> GetWindows(bool excludeSys)
        {
            var windows = new List<WindowInfo>();

            foreach (var item in FindWindowsWithText(" "))
            {
                var wnd = new WindowInfo
                {
                    name = GetWindowText(item),
                    windowHandle = item
                };

                if (!excludeSys)
                {
                    windows.Add(wnd);
                }
                else
                {
                    if (!wnd.name.Contains("Default IME") &&
                        !wnd.name.Contains("Task Host Window") &&
                        !wnd.name.Contains("MSCTFIME UI") &&
                        !wnd.name.Contains("Program Manager") &&
                        !wnd.name.Contains("AXWIN Frame Window") &&
                        !wnd.name.Contains("Microsoft Text Input Application") &&
                        !wnd.name.Contains("DDE Server Window") &&
                        !wnd.name.Contains("Hidden Window") &&
                        !wnd.name.Contains("Windows Push Notifications Platform") &&
                        !wnd.name.Contains("DWM Notification Window") &&
                        !wnd.name.Contains("Filme & TV") &&
                        !wnd.name.Contains("Network Flyout")
                        )
                    {
                        windows.Add(wnd);
                    }
                }
            }
            return windows;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            var size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            var found = IntPtr.Zero;
            var windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    windows.Add(wnd);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText) => FindWindows(delegate (IntPtr wnd, IntPtr param)
                                                                                             {
                                                                                                 return GetWindowText(wnd).Contains(titleText);
                                                                                             });
    }
}
