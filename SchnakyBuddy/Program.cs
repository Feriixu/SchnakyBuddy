using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SchnakyBuddy
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            //var windows = WindowEnumerator.GetWindows(false);
            //foreach (var window in windows)
            //{
            //    ShowWindow(window.windowHandle, 1);
            //    SetForegroundWindow(window.windowHandle);
            //}
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var mainForm = new Schnaky())
            {
                Application.Run(mainForm);
            }
        }
    }
}
