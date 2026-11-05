// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace zClock
{
    internal sealed class PrompterWindow : Form
    {
        readonly PrompterSettings settings;
        readonly RichTextBox notes = new RichTextBox();
        readonly Label heading = new Label();
        readonly Label clock = new Label();
        readonly FlowLayoutPanel toolbar = new FlowLayoutPanel();
        readonly Timer scroll = new Timer { Interval = 700 };
        readonly Button follow = new Button { Text = "跟随放映", AutoSize = true };
        PrompterState state;
        int page;
        bool following = true;
        string identity = "";
        bool fullScreen;
        Rectangle normalBounds;
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        protected override bool ShowWithoutActivation { get { return true; } }

        public PrompterWindow(PrompterSettings settings)
        {
            this.settings = settings;
            Text = "zClock 提词器";
            MinimumSize = new Size(360, 320);
            Size = new Size(900, 720);
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            heading.Dock = DockStyle.Top;
            heading.Height = 65;
            heading.Padding = new Padding(14, 8, 14, 4);
            heading.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            clock.AutoSize = true;
            clock.Padding = new Padding(8);
            toolbar.Dock = DockStyle.Top;
            toolbar.AutoSize = true;
            toolbar.WrapContents = true;
            toolbar.Padding = new Padding(8);
            AddButton("← 上页", () => Browse(-1));
            follow.Click += (s, e) => { following = true; Render(); };
            toolbar.Controls.Add(follow);
            AddButton("下页 →", () => Browse(1));
            AddButton("A−", () => { settings.FontSize = Math.Max(18, settings.FontSize - 2); Appearance(); });
            AddButton("A＋", () => { settings.FontSize = Math.Min(80, settings.FontSize + 2); Appearance(); });
            AddButton("明暗", () => { settings.LightTheme = !settings.LightTheme; Appearance(); });
            Button auto = null;
            auto = AddButton("自动滚动", () => { scroll.Enabled = !scroll.Enabled; auto.Text = scroll.Enabled ? "停止滚动" : "自动滚动"; });
            AddButton("滚动加速", () => scroll.Interval = Math.Max(80, scroll.Interval - 100));
            AddButton("滚动减速", () => scroll.Interval = Math.Min(3000, scroll.Interval + 100));
            AddButton("回顶部", () => { notes.SelectionStart = 0; notes.ScrollToCaret(); });
            Button pin = null;
            pin = AddButton("窗口置顶", () => { TopMost = !TopMost; pin.Text = TopMost ? "取消置顶" : "窗口置顶"; });
            AddButton("全屏 F11", ToggleFullScreen);
            toolbar.Controls.Add(clock);
            notes.Dock = DockStyle.Fill;
            notes.ReadOnly = true;
            notes.BorderStyle = BorderStyle.None;
            notes.WordWrap = true;
            notes.DetectUrls = false;
            notes.ScrollBars = RichTextBoxScrollBars.Vertical;
            notes.Margin = new Padding(16);
            var padding = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 20) };
            padding.Controls.Add(notes);
            Controls.Add(padding);
            Controls.Add(toolbar);
            Controls.Add(heading);
            scroll.Tick += (s, e) => { if (Visible) SendMessage(notes.Handle, 0x00B6, IntPtr.Zero, new IntPtr(1)); };
            Resize += (s, e) => Appearance();
            FormClosing += (s, e) =>
            {
                if (!fullScreen && WindowState == FormWindowState.Normal) settings.WindowBounds = Bounds;
                else if (fullScreen) settings.WindowBounds = normalBounds;
                else settings.WindowBounds = RestoreBounds;
            };
            FormClosed += (s, e) => scroll.Dispose();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F11) { ToggleFullScreen(); e.Handled = true; }
                if (e.KeyCode == Keys.Escape && fullScreen) { ToggleFullScreen(); e.Handled = true; }
                if (e.KeyCode == Keys.Left) { Browse(-1); e.Handled = true; }
                if (e.KeyCode == Keys.Right) { Browse(1); e.Handled = true; }
            };
            Appearance();
        }

        Button AddButton(string text, Action action)
        {
            var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(0, 34), FlatStyle = FlatStyle.Flat };
            button.Click += (s, e) => action();
            toolbar.Controls.Add(button);
            return button;
        }

        void Appearance()
        {
            Color background = settings.LightTheme ? Color.FromArgb(248, 246, 237) : Color.FromArgb(16, 21, 18);
            Color foreground = settings.LightTheme ? Color.FromArgb(23, 34, 26) : Color.FromArgb(241, 244, 233);
            BackColor = notes.BackColor = background;
            ForeColor = notes.ForeColor = foreground;
            foreach (Control control in toolbar.Controls) { control.BackColor = background; control.ForeColor = foreground; }
            float size = settings.FontSize * Math.Max(.75f, Math.Min(1.6f, ClientSize.Width / 900f));
            if (Math.Abs(notes.Font.Size - size) > .2f)
            {
                Font previous = notes.Font;
                notes.Font = new Font("Microsoft YaHei", size);
                if (previous != Control.DefaultFont) previous.Dispose();
            }
        }

        public void PlaceOn(Screen screen)
        {
            Rectangle saved = settings.WindowBounds;
            Rectangle area = screen.WorkingArea;
            if (saved.Width >= MinimumSize.Width && saved.Height >= MinimumSize.Height && area.Contains(saved)) Bounds = saved;
            else Bounds = new Rectangle(area.Left + area.Width / 20, area.Top + area.Height / 20, Math.Max(360, area.Width * 9 / 10), Math.Max(320, area.Height * 9 / 10));
        }

        public void UpdateState(PrompterState value)
        {
            if (state == null || state.deck != value.deck) { following = true; identity = ""; }
            state = value;
            Render();
        }

        void Browse(int delta)
        {
            if (state == null || !state.presenting) return;
            following = false;
            page = Math.Max(1, Math.Min(state.total, page + delta));
            Render();
        }

        void Render()
        {
            if (state == null) return;
            if (following) page = state.current;
            PrompterSlide slide = state.slides.FirstOrDefault(s => s.index == page);
            heading.Text = state.presenting ? state.presentation + "\n" + page + " / " + state.total + "  ·  " + (slide == null ? "读取中…" : slide.title) : state.status;
            clock.Text = state.presenting ? (state.paused ? "已暂停 " : "倒计时 ") + state.timer : "等待放映";
            follow.Text = following ? "● 跟随中" : "返回当前页";
            string text = !state.presenting ? "放映开始后，自动显示当前幻灯片备注。" : slide == null ? "正在读取备注…" : string.IsNullOrWhiteSpace(slide.notes) ? "本页没有备注。" : slide.notes;
            string next = state.deck + "|" + page + "|" + text;
            if (next != identity) { notes.Text = text; notes.SelectionStart = 0; notes.ScrollToCaret(); identity = next; }
        }

        void ToggleFullScreen()
        {
            if (!fullScreen)
            {
                normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
            }
            else { FormBorderStyle = FormBorderStyle.Sizable; Bounds = normalBounds; }
            fullScreen = !fullScreen;
        }
    }
}
