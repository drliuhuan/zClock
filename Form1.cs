// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices; // 添加这个命名空间
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;


namespace zClock
{
    public partial class zClock : Form
    {
        static Process[] pptprocesses;
        static Process[] wpsprocesses;
        Point mousedown, mouseup;
        Point offsetPoint;
        bool firsttimehide = true;
        const int DPI_SCALE = 96; // DPI 的缩放比例
        const int TOLERANCE = 3; // 判断是否全屏的容差
        const int CLOCK_WIDTH_RATIO = 3; // 时钟宽度占屏幕宽度的比例
        const int CLOCK_HEIGHT_RATIO = 20; // 时钟高度占屏幕高度的比例
        const int CLOCK_MARGIN = 10; // 时钟的边距
        Point location = new Point();
        bool hideWhenNotPresentation = false;
        
        readonly Stopwatch countdownElapsed = new Stopwatch();
        long countdownAppliedSeconds;
        bool paused = false;
        bool mouseOver = false;
        bool mouseOut = false;
        static bool presentationState = false;
        //presentationState= false;
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002; 
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hWnd);
        static IntPtr intPtr;
        static dynamic pptWindow;
        static dynamic pptApp;// 使用 Marshal 类
        //static Timer timer = Form1.clock_Timer;

        [DllImport("user32.dll")]
        static extern int GetDpiForWindow(IntPtr hWnd);
        //定义窗口位置和大小的结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width;
            public int Height;
        }

        int clocktimer = (int)Properties.Settings.Default.time;
        private static bool IsFullScreen(IntPtr hWnd)
        {

            Screen screen = Screen.FromHandle(hWnd);
            //获取窗口的位置和大小
            RECT rect;
            GetWindowRect(hWnd, out rect);
            //获取窗口的DPI值
            int dpi = GetDpiForWindow(hWnd);
            //Console.WriteLine("App:"+hWnd.ToString()+" DPI:"+dpi.ToString());

            //根据DPI值调整位置和大小
            rect.Left = rect.Left * DPI_SCALE / dpi;
            rect.Top = rect.Top * DPI_SCALE / dpi;
            rect.Right = rect.Right * DPI_SCALE / dpi;
            rect.Bottom = rect.Bottom * DPI_SCALE / dpi;
            rect.Width = rect.Right - rect.Left;
            rect.Height = rect.Bottom - rect.Top;

            //Console.WriteLine(hWnd.ToString()+" Screen:"+screen.DeviceName.ToString() +" " + (rect.Right-rect.Left).ToString()+" "+(rect.Bottom-rect.Top).ToString()+" "+(screen.Bounds.Width * DPI_SCALE / dpi).ToString()+" "+ (screen.Bounds.Height * DPI_SCALE / dpi).ToString());
            return ((almostSame(rect.Width,screen.Bounds.Width * DPI_SCALE / dpi) && almostSame(rect.Height, screen.Bounds.Height * DPI_SCALE / dpi))||(almostSame(rect.Width, screen.WorkingArea.Width * DPI_SCALE / dpi) && almostSame(rect.Height, screen.WorkingArea.Height * DPI_SCALE / dpi)));
        }
        private static bool almostSame(int x, int y)
        {
            if (Math.Abs(x - y) < TOLERANCE) return true;
            else return false;
        }

        //定义一个方法，用于判断窗口的标题是否包含“放映”
        private static bool HasProjectionTitle(Process process)
        {
            //你可以使用我之前给你的代码来获取窗口的标题，或者直接使用Process类的MainWindowTitle属性
            //这里只是一个简单的示例，可能不适用于所有情况
            return process.MainWindowTitle.Contains("放映");
        }

        //定义一个方法，用于获取全屏且标题包含“放映”的窗口
        private static IntPtr GetFullScreenProjectionHandle()
        {
            try
            {
                if (pptApp == null) pptApp = Marshal.GetActiveObject("PowerPoint.Application");
                object windows = null;
                try
                {
                    windows = pptApp.SlideShowWindows;
                    dynamic shows = windows;
                    if ((int)shows.Count == 0) return IntPtr.Zero;
                    object next = shows.Item(1);
                    if (!ReferenceEquals((object)pptWindow, next)) ReleasePrompterCom((object)pptWindow);
                    else ReleasePrompterCom(next);
                    pptWindow = next;
                    presentationState = true;
                    return new IntPtr(GetSlideShowHandle((object)pptWindow));
                }
                finally { ReleasePrompterCom(windows); }
            }
            catch (COMException) { }
            catch (InvalidComObjectException) { }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { }
            ReleasePrompterCom((object)pptWindow);
            ReleasePrompterCom((object)pptApp);
            pptWindow = null;
            pptApp = null;
            presentationState = false;
            return IntPtr.Zero;
        }

        internal static int GetSlideShowHandle(object window)
        {
            return ((ISlideShowWindowHandle)window).HWND;
        }

        static bool IsMultipleProcessRunning(string processName)
        {
            // 使用Process类的GetProcessesByName方法，根据进程名获取进程数组
            Process[] processes = Process.GetProcessesByName(processName);
            // 如果进程数组的长度大于1，说明有多个进程正在运行，返回true
            if (processes.Length > 1)
            {
                return true;
            }
            // 否则，返回false
            else
            {
                return false;
            }
        }
        static Process[] GetProcesses(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if ((processes ?? Array.Empty<Process>()).Length>0)return processes;
            else return Array.Empty<Process>();
        }
        private void ShowMyFormOnScreen(IntPtr hWnd)
        {
            //this.Show();
            //获取显示某个窗口的显示器
            if (location.IsEmpty) 
            {
                Screen screen = Screen.FromHandle(hWnd);
                location.Y = screen.Bounds.Top;
                location.X = screen.Bounds.Left + screen.Bounds.Width / 3 * 2;
            }
            this.Location = location;
        }
        private void MoveBackForm()
        {
            if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(this.Bounds))) return;
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            location = new Point(area.Left + Math.Max(0, area.Width - Width) * 2 / 3, area.Top);
            this.Location = location;
        }

        private static bool IsPresentationWindowValid()
        {
            if (intPtr == IntPtr.Zero || !IsWindow(intPtr)) return false;
            try
            {
                return pptWindow != null && new IntPtr(GetSlideShowHandle((object)pptWindow)) == intPtr;
            }
            catch (COMException) { return false; }
            catch (InvalidComObjectException) { return false; }
        }

        internal static string FormatRemainingTime(int seconds)
        {
            long absolute = Math.Abs((long)seconds);
            string text = absolute >= 3600
                ? string.Format("{0:00}:{1:00}:{2:00}", absolute / 3600, absolute / 60 % 60, absolute % 60)
                : string.Format("{0:00}:{1:00}", absolute / 60, absolute % 60);
            return seconds < 0 ? "-" + text : text;
        }

        void ApplyElapsedCountdown()
        {
            long elapsed = countdownElapsed.ElapsedMilliseconds / 1000;
            long difference = elapsed - countdownAppliedSeconds;
            if (difference > 0)
            {
                clocktimer = (int)Math.Max(int.MinValue, (long)clocktimer - difference);
                countdownAppliedSeconds = elapsed;
            }
        }

        void StartCountdown() { countdownElapsed.Start(); clock_Timer.Start(); }
        void StopCountdown() { countdownElapsed.Stop(); ApplyElapsedCountdown(); clock_Timer.Stop(); }
        void ResetCountdownEpoch()
        {
            bool running = countdownElapsed.IsRunning;
            countdownElapsed.Reset();
            countdownAppliedSeconds = 0;
            if (running) countdownElapsed.Start();
        }

        public zClock()
        {
            InitializeComponent();
            InitializePrompter();
        }
        private void timeFormat(bool mouseover)
        {
            if (mouseover&presentationState)
            {
                if (mouseOut)
                {
                    if (!paused) clock_label.Text = "PAUSE";
                    else clock_label.Text = "PLAY!";
                    clock_label.ForeColor = Color.Yellow;
                    LayoutClockText();
                    return;
                }
                
            }
            //Console.WriteLine(clocktimer.ToString());
            clock_label.ForeColor = Color.White;
            if (paused)
            {
                clock_label.Text = "PAUSED";
                LayoutClockText();
                return;
            }
            clock_label.Text = FormatRemainingTime(clocktimer);
            clock_label.ForeColor = clocktimer < 0 ? Color.Red : Color.White;
            LayoutClockText();
        }
        private void LayoutClockText()
        {
            clock_label.Location = new Point(CLOCK_MARGIN, CLOCK_MARGIN);
            ClientSize = new Size(clock_label.PreferredSize.Width + 2 * CLOCK_MARGIN, clock_label.PreferredSize.Height + 2 * CLOCK_MARGIN);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.Visible = true;
            timeFormat(mouseOver);
            clock_label.ForeColor = Color.White;
            this.Top = 0;
            this.Left = Screen.PrimaryScreen.Bounds.Width / 3 * 2;
            ResetToolStripMenuItem.Visible = false;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            StringBuilder sb=new StringBuilder();
            if ((pptprocesses ?? Array.Empty<Process>()).Length != (GetProcesses("powerpnt") ?? Array.Empty<Process>()).Length)
            {
                pptprocesses = GetProcesses("powerpnt");

                if ((pptprocesses ?? Array.Empty<Process>()).Length > 1)
                {
                    sb.AppendLine("检测到" + (pptprocesses ?? Array.Empty<Process>()).Length.ToString() + "个PowerPoint进程");
                    sb.AppendLine("本工具仅适用于单个PPT倒计时，为确保能够正确运行，请每次只打开一个幻灯片。");
                }
            }
            //Console.WriteLine((pptprocesses ?? Array.Empty<Process>()).Length.ToString());
            Process[] p1 = GetProcesses("WPS") ?? Array.Empty<Process>();
            //Console.WriteLine((p1 ?? Array.Empty<Process>()).Length.ToString());
            Process[] p2 = GetProcesses("WPP") ?? Array.Empty<Process>();
            //Console.WriteLine((p2 ?? Array.Empty<Process>()).Length.ToString());
            Process[] p3 = (p1 ?? Array.Empty<Process>()).Concat(p2 ?? Array.Empty<Process>()).ToArray();
            //Console.WriteLine((p3 ?? Array.Empty<Process>()).Length.ToString());
            if ((wpsprocesses ?? Array.Empty<Process>()).Length != (p3 ?? Array.Empty<Process>()).Length)
            {
                wpsprocesses = p3;
                if ((wpsprocesses ?? Array.Empty<Process>()).Length > 0)
                {
                    sb.AppendLine("检测到WPS进程");
                    sb.AppendLine("本工具仅适用于Microsoft PowerPoint，暂不支持WPS幻灯片倒计时。");
                }
            }
            
            if (!string.IsNullOrEmpty(sb.ToString())) MessageBox.Show(sb.ToString(), "不兼容告警", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (!IsPresentationWindowValid())
                intPtr = GetFullScreenProjectionHandle();
            presentationState = intPtr != IntPtr.Zero;
            ResetToolStripMenuItem.Visible = presentationState;
            if (presentationState)
            {
                ShowMyFormOnScreen(intPtr);
                MoveBackForm();
                if (!paused) StartCountdown();
            }
            else
            {
                StopCountdown();
                paused = false;
                clocktimer = (int)Properties.Settings.Default.time;
                ResetCountdownEpoch();
                timeFormat(mouseOver);
                MoveBackForm();
            }
            if (hideWhenNotPresentation & (!presentationState)) { this.Hide(); if (firsttimehide) notifyIcon1.ShowBalloonTip(10, "zClock", "已隐藏到系统托盘，PPT放映时将自动显示", ToolTipIcon.Info); firsttimehide = false; }
            else this.Show();

            //等待用户按下任意键退出程序
            //Console.WriteLine("Press any key to exit...");
            //Console.ReadKey();
        }

        private void clock_Timer_Tick(object sender, EventArgs e)
        {
            if (!IsPresentationWindowValid())
            {
                timer1_Tick(sender, e);
                return;
            }
            if (paused) return;
            this.Show();
            ApplyElapsedCountdown();
            timeFormat(mouseOver);
            ResetToolStripMenuItem.Visible = presentationState;
        }



        private void ThisMouseDown(object sender, MouseEventArgs e)
        {
            mousedown= this.Location;
            if (e.Button == MouseButtons.Left)
            {
                offsetPoint = new Point(e.X, e.Y); // 鼠标相对于(窗体)左上角位置
            }
        }
        private void ThisMouseUp(object sender, MouseEventArgs e)
        {
            mouseup = this.Location;
            //Console.WriteLine(mouseup.ToString());
            if (mouseup == mousedown)
            {
                ThisMouseClick(sender, e);
            }
        }
        private void ThisMouseMove(object sender, MouseEventArgs e)
        {
            
            if (e.Button == MouseButtons.Left)
            {
                mouseOver = false;
                this.Location = new Point(this.Location.X + e.X - offsetPoint.X, this.Location.Y + e.Y - offsetPoint.Y);
                location = this.Location;
                timeFormat(mouseOver);
            }
        }
        private void ThisMouseClick(object sender, MouseEventArgs e)
        {
            mouseOut = false;
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(new Point(this.Location.X + e.X, this.Location.Y + e.Y));
            }
            else if (presentationState)
            {

                paused = !paused;
                if (paused) StopCountdown(); else StartCountdown();
                timeFormat(mouseOver);

            }
        }

        private void hideToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {

            hideWhenNotPresentation = HideToolStripMenuItem.Checked;
            if (hideWhenNotPresentation & (!presentationState))
            {
                if (firsttimehide) notifyIcon1.ShowBalloonTip(10, "zClock", "已隐藏到系统托盘，PPT放映时将自动显示", ToolTipIcon.Info);
                this.Hide();
                firsttimehide = false;
            }
            else this.Show();
        }



        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void clock_label_MouseEnter(object sender, EventArgs e)
        {
            mouseOver = true;
            if (!mouseOut) return;
            timeFormat(mouseOver); 
        }

        private void Form1_MouseEnter(object sender, EventArgs e)
        {
            mouseOver = true;
            if (!mouseOut) return;
            timeFormat(mouseOver);
        }

        private void Form1_MouseLeave(object sender, EventArgs e)
        {

            mouseOver = false;
            mouseOut = true;
            timeFormat(mouseOver);

        }

        private void 时间设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool wasPaused = paused;
            timer_checkFullScreen.Stop();
            StopCountdown();
            if (presentationState) paused = true;
            timeFormat(false);
            try
            {
                using (InputForm inputForm = new InputForm())
                {
                    if (inputForm.ShowDialog(this) == DialogResult.OK)
                    {
                        Properties.Settings.Default.time = (uint)inputForm.Input;
                        Properties.Settings.Default.Save();
                        clocktimer = inputForm.Input;
                        ResetCountdownEpoch();
                    }
                }
            }
            finally
            {
                paused = wasPaused;
                timer1_Tick(sender, e);
                timeFormat(false);
                timer_checkFullScreen.Start();
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            mouseOver = false;
            timeFormat(mouseOver);
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clocktimer = (int)Properties.Settings.Default.time;
            ResetCountdownEpoch();

                paused = false;
                StartCountdown();
                timeFormat(false);
        }
    }
}
