// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace zClock
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool firstInstance;
            using (var instance = new Mutex(true, @"Local\zClock", out firstInstance))
            {
                if (!firstInstance)
                {
                    MessageBox.Show("zClock 已在运行，请从系统托盘打开。", "zClock");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new zClock());
            }
        }
    }
}
