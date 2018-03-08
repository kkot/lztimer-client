using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace kkot.LzTimer
{
    internal class ShortcutsManager
    {
        #region fields
        public static int MOD_ALT = 0x1;
        public static int MOD_CONTROL = 0x2;
        public static int MOD_SHIFT = 0x4;
        public static int MOD_WIN = 0x8;
        public static int WM_HOTKEY = 0x312;
        #endregion

        private readonly MainWindow form;

        internal ShortcutsManager(MainWindow mainWindow)
        {
            form = mainWindow;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static void RegisterHotKey(Form f, Keys key, int modifiers)
        {
            Keys k = key;
            int keyId = (int)k; //f.GetHashCode(); // this should be a key unique ID, modify this if you want more than one hotkey
            RegisterHotKey(f.Handle, keyId, modifiers, (int)k);
        }

        private static void UnregisterHotKey(Form f, Keys key)
        {
            try
            {
                UnregisterHotKey(f.Handle, (int)key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        internal void ProcessMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                if ((int)m.WParam == (int)Keys.A)
                {
                    form.ToggleVisible();
                }
            }
        }

        internal void Register()
        {
            RegisterHotKey(form, Keys.A, MOD_WIN);
        }

        internal void UnRegister()
        {
            UnregisterHotKey(form, Keys.A);
        }
    }
}
