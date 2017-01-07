using System;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    internal enum Win32Hook : int
    {
        WH_MIN = -1,
        WH_MSGFILTER = -1,
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    internal delegate int Win32HookProcHandler(int nCode, IntPtr wParam, IntPtr lParam);

	public class Win32
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto,
				    CallingConvention = CallingConvention.StdCall)]
		internal static extern int SetWindowsHookEx(int idHook, Win32HookProcHandler lpfn,
												   IntPtr hInstance, int threadId);
		[DllImport("user32.dll", CharSet = CharSet.Auto,
				   CallingConvention = CallingConvention.StdCall)]
		internal static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int CallNextHookEx(int idHook, int nCode,
                                                 IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto,
           CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetLastError();

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
	}
}
