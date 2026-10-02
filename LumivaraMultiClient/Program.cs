using LumivaraMultiClient.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LumivaraMultiClient
{
    internal static class Program
    {
        //private static Mutex mutex = new Mutex(true, "LumivaraMultiClient");
        [STAThread]
        static void Main()
        {
            string processName = Process.GetCurrentProcess().ProcessName;
            var runningProcesses = Process.GetProcessesByName(processName);
            if (runningProcesses.Length > 1)
            {
                MessageBox.Show("พบโปแกรมกำลังทำงานอยู่แล้ว", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
