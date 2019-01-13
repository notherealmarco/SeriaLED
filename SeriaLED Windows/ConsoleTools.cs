using Microsoft.Win32;
using SeriaLED_Windows.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeriaLED.User32
{
    public static class ConsoleTools
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        static NotifyIcon Tray;

        public static void Show(bool show)
        {
            if (show) ShowWindow(GetConsoleWindow(), SW_SHOW);
            else ShowWindow(GetConsoleWindow(), SW_HIDE);
        }
        public static void ShowTray()
        {
            Tray = new NotifyIcon();
            Tray.Icon = Resources.icon;
            Tray.Text = "Service is running...";
            //Tray.MouseClick += OnDoubleClick;
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(new MenuItem("Exit", OnExit));
            Tray.ContextMenu = contextMenu;
            Tray.Visible = true;
        }
        static void OnExit(object sender, object args)
        {
            Application.Exit();
        }
        public static void AddApplicationToStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue("SeriaLED", "\"" + Application.ExecutablePath + "\"");
            }
        }
        public static void RemoveApplicationFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue("SeriaLED", false);
            }
        }
    }
}
