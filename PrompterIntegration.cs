// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zClock
{
    public partial class zClock
    {
        PrompterSettings prompterSettings;
        PrompterServer prompterServer;
        PrompterWindow prompterWindow;
        Timer prompterTimer;
        PrompterState prompterState = PrompterState.Waiting("等待 PowerPoint 放映…");
        readonly List<PrompterSlide> prompterSlides = new List<PrompterSlide>();
        string prompterDeck = "";
        string prompterRevision = "";
        string prompterError = "";
        int prompterReadIndex;
        bool prompterSuppressed;
        bool prompterManualWindow;
        bool prompterWasPresenting;
        bool closingPrompter;
        bool prompterFirewallBusy;

        void InitializePrompter()
        {
            prompterSettings = PrompterSettings.Load();
            var menu = new ToolStripMenuItem("提词器");
            menu.DropDownItems.Add("打开本机网页", null, (s, e) =>
            {
                if (prompterServer == null) { ShowPrompterOptions(); return; }
                try { Process.Start(new ProcessStartInfo(prompterServer.LocalUrl) { UseShellExecute = true }); }
                catch (Exception error) { MessageBox.Show("无法打开浏览器：" + error.Message); }
            });
            menu.DropDownItems.Add("打开提词窗口", null, (s, e) => OpenPrompterWindow(null, true));
            menu.DropDownItems.Add("手机扫码连接", null, (s, e) => ShowPrompterConnect());
            menu.DropDownItems.Add("设置 / 手机连接地址", null, (s, e) => ShowPrompterOptions());
            contextMenuStrip1.Items.Insert(contextMenuStrip1.Items.Count - 1, menu);
            RestartPrompterServer();
            Shown += (s, e) => BeginInvoke(new Action(MaybeRequestPrompterFirewall));
            prompterTimer = new Timer(components) { Interval = 600, Enabled = true };
            prompterTimer.Tick += (s, e) => UpdatePrompter();
            FormClosed += (s, e) =>
            {
                closingPrompter = true;
                if (prompterServer != null) prompterServer.Dispose();
                if (prompterWindow != null) prompterWindow.Close();
                SavePrompterSettings();
                ReleasePrompterCom((object)pptWindow);
                ReleasePrompterCom((object)pptApp);
                pptWindow = null;
                pptApp = null;
                intPtr = IntPtr.Zero;
            };
        }

        void SavePrompterSettings()
        {
            try { prompterSettings.Save(); }
            catch (Exception error)
            {
                if (!(error is System.IO.IOException || error is UnauthorizedAccessException || error is InvalidOperationException)) throw;
                Debug.WriteLine("提词器设置保存失败：" + error.Message);
            }
        }

        void RestartPrompterServer()
        {
            if (prompterServer != null) { prompterServer.Dispose(); prompterServer = null; }
            prompterError = "";
            if (!prompterSettings.Enabled) return;
            try
            {
                prompterServer = new PrompterServer(prompterSettings.Port, prompterSettings.AllowLan, prompterSettings.AccessKey);
                prompterServer.Publish(CreateSharedPrompterState(prompterState, prompterSettings.SharePreviews));
            }
            catch (Exception error)
            {
                prompterError = "网页服务启动失败：" + error.Message + "（可在设置中更换端口）";
                notifyIcon1.ShowBalloonTip(5000, "zClock 提词器", prompterError, ToolTipIcon.Warning);
            }
        }

        void ShowPrompterOptions()
        {
            using (var dialog = new PrompterOptions(prompterSettings, prompterServer == null ? null : prompterServer.Urls(), prompterError))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                bool oldEnabled = prompterSettings.Enabled;
                bool oldLan = prompterSettings.AllowLan;
                int oldPort = prompterSettings.Port;
                string oldKey = prompterSettings.AccessKey;
                dialog.Apply();
                SavePrompterSettings();
                if (oldEnabled != prompterSettings.Enabled || oldLan != prompterSettings.AllowLan || oldPort != prompterSettings.Port || oldKey != prompterSettings.AccessKey || (prompterSettings.Enabled && prompterServer == null)) RestartPrompterServer();
                if (!string.IsNullOrEmpty(prompterError)) MessageBox.Show(prompterError, "提词器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdatePrompter();
                MaybeRequestPrompterFirewall();
            }
        }

        void ShowPrompterConnect()
        {
            if (prompterServer == null || !prompterSettings.AllowLan)
            {
                MessageBox.Show("请先在提词器设置中启用网页服务，并勾选“允许同一局域网的手机 / 平板 / 电脑连接”。", "手机扫码连接");
                ShowPrompterOptions();
                if (prompterServer == null || !prompterSettings.AllowLan) return;
            }
            try { using (var dialog = new PrompterConnect(prompterServer.Urls(), EnablePrompterFirewall)) dialog.ShowDialog(); }
            catch (InvalidOperationException error) { MessageBox.Show(error.Message, "手机扫码连接"); }
        }

        async void EnablePrompterFirewall()
        {
            if (prompterFirewallBusy || closingPrompter || prompterServer == null || !prompterSettings.AllowLan) return;
            prompterFirewallBusy = true;
            prompterSettings.FirewallRequestFor = PrompterFirewallIdentity();
            SavePrompterSettings();
            string script = BuildPrompterFirewallScript(Process.GetCurrentProcess().MainModule.FileName, prompterSettings.Port);
            try
            {
                var start = new ProcessStartInfo(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"), "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
                { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                using (var process = Process.Start(start))
                {
                    await Task.Run(() => process.WaitForExit());
                    if (!closingPrompter) MessageBox.Show(process.ExitCode == 0 ? "已允许本程序当前端口的专用局域网访问。现在可重新扫码连接。\n如果仍无法访问，请确认电脑网络类型为“专用网络”，且路由器没有隔离设备。" : "未能添加访问规则，可在“手机扫码连接”窗口点击“允许局域网访问”重试。", "局域网访问");
                }
            }
            catch (System.ComponentModel.Win32Exception error)
            {
                // UAC cancellation is an intentional choice; do not prompt again at every startup.
                if (!closingPrompter && error.NativeErrorCode != 1223) MessageBox.Show("未更改防火墙规则：" + error.Message, "局域网访问");
            }
            finally { prompterFirewallBusy = false; }
        }

        string PrompterFirewallIdentity()
        {
            return Process.GetCurrentProcess().MainModule.FileName.ToLowerInvariant() + "|" + prompterSettings.Port;
        }

        internal static string BuildPrompterFirewallScript(string executable, int port)
        {
            if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException("port");
            return "$ErrorActionPreference='Stop'; try { Get-NetFirewallRule -Name 'zClock-Prompter' -ErrorAction SilentlyContinue | Remove-NetFirewallRule; " +
                "New-NetFirewallRule -Name 'zClock-Prompter' -DisplayName 'zClock 提词器 (Private LAN)' -Direction Inbound -Action Allow -Profile Private -Protocol TCP -LocalPort " +
                port + " -RemoteAddress LocalSubnet -Program '" + executable.Replace("'", "''") + "' | Out-Null; exit 0 } catch { exit 1 }";
        }

        internal static bool ShouldRequestPrompterFirewall(PrompterSettings settings, bool serverRunning, string identity)
        {
            return serverRunning && settings.Enabled && settings.AllowLan && settings.AutoRequestFirewall && settings.FirewallRequestFor != identity;
        }

        void MaybeRequestPrompterFirewall()
        {
            if (closingPrompter || IsDisposed || prompterFirewallBusy) return;
            if (!ShouldRequestPrompterFirewall(prompterSettings, prompterServer != null, PrompterFirewallIdentity())) return;
            notifyIcon1.ShowBalloonTip(5000, "zClock 局域网访问", "即将请求 Windows 管理员授权，以允许手机访问提词器。取消后仍可使用本机提词。", ToolTipIcon.Info);
            EnablePrompterFirewall();
        }

        Screen NonPresentationScreen()
        {
            if (!IsPresentationWindowValid()) return null;
            RECT bounds;
            if (!GetWindowRect(intPtr, out bounds)) return null;
            Rectangle projected = Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            // Exclude every screen touched by the slide-show window, including spanned shows.
            Screen[] screens = Screen.AllScreens;
            int selected = FindPrompterScreen(screens.Select(screen => screen.Bounds).ToArray(), projected);
            return selected < 0 ? null : screens[selected];
        }

        internal static int FindPrompterScreen(Rectangle[] screens, Rectangle projected)
        {
            return Array.FindIndex(screens, screen => !screen.IntersectsWith(projected));
        }

        void OpenPrompterWindow(Screen screen, bool manual = false)
        {
            if (manual) prompterManualWindow = true;
            prompterSuppressed = false;
            if (prompterWindow == null || prompterWindow.IsDisposed)
            {
                prompterWindow = new PrompterWindow(prompterSettings);
                prompterWindow.PlaceOn(screen ?? NonPresentationScreen() ?? Screen.PrimaryScreen);
                prompterWindow.FormClosed += (s, e) => { prompterSuppressed = true; prompterManualWindow = false; prompterWindow = null; if (!closingPrompter) SavePrompterSettings(); };
                prompterWindow.UpdateState(prompterState);
                prompterWindow.Show();
            }
            else { if (screen != null) prompterWindow.PlaceOn(screen); prompterWindow.Show(); }
        }

        void UpdatePrompter()
        {
            if (closingPrompter) return;
            bool active = IsPresentationWindowValid();
            if (!active)
            {
                prompterState = PrompterState.Waiting("等待 PowerPoint 放映…");
                prompterDeck = "";
                prompterSlides.Clear();
                prompterSuppressed = false;
            }
            else
            {
                object slide = null, presentation = null, slides = null;
                try
                {
                    slide = pptWindow.View.Slide;
                    dynamic current = slide;
                    presentation = current.Parent;
                    dynamic deck = presentation;
                    slides = deck.Slides;
                    dynamic all = slides;
                    int count = (int)all.Count;
                    int index = (int)current.SlideIndex;
                    string name = (string)deck.Name;
                    string key = (string)deck.FullName + "|" + count;
                    if (key != prompterDeck)
                    {
                        prompterDeck = key;
                        prompterRevision = Guid.NewGuid().ToString("N");
                        prompterSlides.Clear();
                        prompterReadIndex = 1;
                    }
                    UpsertPrompterSlide(ReadPrompterSlide(slide, index));
                    // Populate previews in small batches to keep the WinForms UI responsive.
                    for (int batch = 0; batch < 3 && prompterReadIndex <= count; batch++, prompterReadIndex++)
                    {
                        if (prompterReadIndex == index) continue;
                        object other = null;
                        try { other = all.Item(prompterReadIndex); UpsertPrompterSlide(ReadPrompterSlide(other, prompterReadIndex)); }
                        finally { ReleasePrompterCom(other); }
                    }
                    prompterState = new PrompterState
                    {
                        presenting = true, presentation = name, current = index, total = count,
                        timer = FormatRemainingTime(clocktimer), paused = paused, status = "放映中", deck = prompterRevision,
                        slides = prompterSlides.OrderBy(s => s.index).ToList()
                    };
                }
                catch (Exception error)
                {
                    if (!(error is COMException || error is InvalidComObjectException || error is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)) throw;
                    // PowerPoint can be temporarily busy (for example while editing). Never crash the clock.
                    prompterState = PrompterState.Waiting("PowerPoint 暂时无法读取备注，正在重试…");
                    Debug.WriteLine(error.Message);
                }
                finally { ReleasePrompterCom(slides); ReleasePrompterCom(presentation); ReleasePrompterCom(slide); }
            }
            if (prompterServer != null) prompterServer.Publish(CreateSharedPrompterState(prompterState, prompterSettings.SharePreviews));
            if (prompterWindow != null && !prompterWindow.IsDisposed) prompterWindow.UpdateState(prompterState);
            Screen target = active ? NonPresentationScreen() : null;
            if (active && prompterSettings.AutoWindow && target != null && !prompterSuppressed)
            {
                if (prompterWindow == null || prompterWindow.IsDisposed) OpenPrompterWindow(target);
                else
                {
                    // A screen reassignment must not leave notes over the projected slide.
                    RECT bounds;
                    if (GetWindowRect(intPtr, out bounds) && prompterWindow.Bounds.IntersectsWith(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom))) prompterWindow.PlaceOn(target);
                    if (!prompterWindow.Visible) prompterWindow.Show();
                }
            }
            else if (active && target == null && prompterSettings.AutoWindow && !prompterManualWindow && prompterWindow != null) prompterWindow.Hide();
            if (prompterWasPresenting && !active && prompterWindow != null) prompterWindow.Hide();
            prompterWasPresenting = active;
        }

        internal static PrompterState CreateSharedPrompterState(PrompterState state, bool previews)
        {
            return new PrompterState
            {
                presenting = state.presenting, presentation = state.presentation,
                current = state.current, total = state.total, timer = state.timer,
                paused = state.paused, status = state.status, deck = state.deck,
                previewEnabled = previews,
                slides = state.slides.Where(slide => previews || slide.index == state.current).ToList()
            };
        }

        void UpsertPrompterSlide(PrompterSlide slide)
        {
            int existing = prompterSlides.FindIndex(s => s.index == slide.index);
            if (existing < 0) prompterSlides.Add(slide); else prompterSlides[existing] = slide;
        }

        static void ReleasePrompterCom(object value)
        {
            if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
        }

        static PrompterSlide ReadPrompterSlide(object value, int index)
        {
            dynamic slide = value;
            string title = "第 " + index + " 页";
            var text = new StringBuilder();
            object notePage = null, placeholders = null, shapes = null;
            try
            {
                title = (string)slide.Name;
                object slideShapes = null, titleShape = null, titleFrame = null, titleRange = null;
                try
                {
                    slideShapes = slide.Shapes;
                    dynamic contentShapes = slideShapes;
                    if ((int)contentShapes.HasTitle != 0)
                    {
                        titleShape = contentShapes.Title;
                        dynamic heading = titleShape;
                        titleFrame = heading.TextFrame;
                        dynamic frame = titleFrame;
                        titleRange = frame.TextRange;
                        dynamic words = titleRange;
                        string headingText = (string)words.Text;
                        if (!string.IsNullOrWhiteSpace(headingText)) title = headingText.Replace('\r', ' ').Replace('\n', ' ').Trim();
                    }
                }
                finally { ReleasePrompterCom(titleRange); ReleasePrompterCom(titleFrame); ReleasePrompterCom(titleShape); ReleasePrompterCom(slideShapes); }
                notePage = slide.NotesPage;
                dynamic page = notePage;
                shapes = page.Shapes;
                dynamic shapeCollection = shapes;
                placeholders = shapeCollection.Placeholders;
                dynamic items = placeholders;
                for (int i = 1; i <= (int)items.Count; i++)
                {
                    object shape = null, format = null, frame = null, range = null;
                    try
                    {
                        shape = items.Item(i);
                        dynamic item = shape;
                        format = item.PlaceholderFormat;
                        dynamic kind = format;
                        // ppPlaceholderBody: excludes slide images, dates, headers and page numbers.
                        if ((int)kind.Type != 2 || (int)item.HasTextFrame == 0) continue;
                        frame = item.TextFrame;
                        dynamic content = frame;
                        range = content.TextRange;
                        dynamic words = range;
                        string note = (string)words.Text;
                        if (!string.IsNullOrWhiteSpace(note)) text.AppendLine(note.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\v', '\n').Trim());
                    }
                    finally { ReleasePrompterCom(range); ReleasePrompterCom(frame); ReleasePrompterCom(format); ReleasePrompterCom(shape); }
                }
            }
            finally { ReleasePrompterCom(placeholders); ReleasePrompterCom(shapes); ReleasePrompterCom(notePage); }
            return new PrompterSlide { index = index, title = title, notes = text.ToString().Trim() };
        }
    }
}
