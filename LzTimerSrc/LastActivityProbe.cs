using System;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    public interface LastActivityProbe
    {
        long GetLastInputTick();
    }

    public class Win32LastActivityProbe : LastActivityProbe
    {
        public long GetLastInputTick()
        {
            Win32.LASTINPUTINFO lastInputInfo = new Win32.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (Win32.GetLastInputInfo(ref lastInputInfo))
            {
                return (int)lastInputInfo.dwTime;
            }
            throw new Exception();
        }
    }

    public class HookActivityProbe : LastActivityProbe, IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int keyboardHookHandle = 0;
        private int mouseHookHandle = 0;
        private long lastInput = 0;

        private Win32HookProcHandler keyboardHandler;
        private Win32HookProcHandler mouseHandler;

        public HookActivityProbe()
        {
            var lib = Win32.LoadLibrary("user32.dll");
            int lastErrorBefore = Win32.GetLastError();
            log.Debug("Last error code before registering hook " + lastErrorBefore);

            keyboardHandler = new Win32HookProcHandler(KeyboardHook);
            keyboardHookHandle = Win32.SetWindowsHookEx((int)Win32Hook.WH_KEYBOARD_LL,
                keyboardHandler, lib, (int)0);

            int lastErrorAfter = Win32.GetLastError();
            log.Debug("Last error code after registering hook " + lastErrorAfter);

            mouseHandler = new Win32HookProcHandler(MouseHook);
            mouseHookHandle = Win32.SetWindowsHookEx((int)Win32Hook.WH_MOUSE_LL,
                mouseHandler, lib, (int)0);
        }

        private void SetLastInputTime()
        {
            lastInput = DateTime.Now.Ticks;
        }

        private int KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                SetLastInputTime();
            }
            return Win32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        private int MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                SetLastInputTime();
            }
            return Win32.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        public long GetLastInputTick()
        {
            return lastInput;
        }

        public void Dispose()
        {
            Win32.UnhookWindowsHookEx(mouseHookHandle);
            Win32.UnhookWindowsHookEx(keyboardHookHandle);
        }
    }
}
