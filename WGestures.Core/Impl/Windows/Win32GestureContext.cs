using System;
using System.Runtime.InteropServices;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Common.OsSpecific.Windows.Win32;

namespace WGestures.Core.Impl.Windows
{
    internal class Win32GestureContext : GestureContext
    {
        //todo: 活动窗口时shell的情况

        private bool IsTopMostWindow(IntPtr hwnd)
        {
            var info = new User32.WINDOWINFO();
            info.cbSize = (uint) Marshal.SizeOf(info);
            User32.GetWindowInfo(hwnd, ref info);

            const uint TOP_MOST = (uint) User32.WS_EX.WS_EX_TOPMOST;

            return (info.dwExStyle & TOP_MOST) == TOP_MOST;
        }


        public override void ActivateTargetWindow()
        {
            var rootWindow = Native.GetAncestor(WinId, Native.GetAncestorFlags.GetRoot);

            var fgWin = User32.GetForegroundWindow();
            if (fgWin != rootWindow)
            {
                User32.SetForegroundWindow(rootWindow);
            }
        }
    }
}
