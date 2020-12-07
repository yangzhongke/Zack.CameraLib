using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Zack.WinFormCoreCameraPlayer
{
    internal class Win32
    {
        [DllImport("user32.dll")]
        public static extern long GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern int GetClipBox(IntPtr hDC, ref Rectangle lpRect);

        /// <summary>
        /// Is Windows invisible or the window is all covered by another window.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static bool IsWindowPall(IntPtr hwnd)
        {
            if (!IsWindowVisible(hwnd)) return true;
            IntPtr vDC = GetWindowDC(hwnd);
            try
            {
                Rectangle vRect = new Rectangle();
                GetClipBox(vDC, ref vRect);
                return vRect.Width - vRect.Left <= 0 && vRect.Height - vRect.Top <= 0;
            }
            finally
            {
                ReleaseDC(hwnd, vDC);
            }
        }
    }
}
