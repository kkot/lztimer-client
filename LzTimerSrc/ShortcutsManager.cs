using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace kkot.LzTimer
{
    public class ShortcutsManager
    {
        #region fields
        public static int MOD_ALT = 0x1;
        public static int MOD_CONTROL = 0x2;
        public static int MOD_SHIFT = 0x4;
        public static int MOD_WIN = 0x8;
        public static int WM_HOTKEY = 0x312;
        #endregion

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static int keyId;
        public static void RegisterHotKey(Form f, Keys key, int modifiers)
        {
            Keys k = key;
            keyId = (int)k;//f.GetHashCode(); // this should be a key unique ID, modify this if you want more than one hotkey
            RegisterHotKey((IntPtr)f.Handle, keyId, (int)modifiers, (int)k);
        }

        private delegate void Func();

        public static void UnregisterHotKey(Form f, int keyId)
        {
            try
            {
                UnregisterHotKey(f.Handle, keyId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
