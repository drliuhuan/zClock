// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using zClock;

class IntegrationChecks
{
    static Assembly assembly = typeof(PrompterState).Assembly;
    static int passed;
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); passed++; Console.WriteLine("PASS " + label); }
    static string Get(string url) { using (var web = new WebClient()) { web.Encoding = Encoding.UTF8; return web.DownloadString(url); } }
    static int Status(string url, string method)
    {
        try { var request = (HttpWebRequest)WebRequest.Create(url); request.Method = method; request.Timeout = 5000; using (var response = (HttpWebResponse)request.GetResponse()) return (int)response.StatusCode; }
        catch (WebException error) { using (var response = (HttpWebResponse)error.Response) { if (response == null) throw; return (int)response.StatusCode; } }
    }
    static void Release(object value) { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            string key = Guid.NewGuid().ToString("N");
            MethodInfo automatic = typeof(zClock.zClock).GetMethod("ShouldRequestPrompterFirewall", BindingFlags.Static | BindingFlags.NonPublic);
            var firewallSettings = new PrompterSettings();
            Func<bool, string, bool> shouldRequest = (running, identity) => (bool)automatic.Invoke(null, new object[] { firewallSettings, running, identity });
            Check(shouldRequest(true, "app|18765"), "First LAN startup requests firewall authorization");
            Check(!shouldRequest(false, "app|18765"), "Stopped web service never requests firewall authorization");
            firewallSettings.AllowLan = false; Check(!shouldRequest(true, "app|18765"), "Local-only service never requests firewall authorization");
            firewallSettings.AllowLan = true; firewallSettings.AutoRequestFirewall = false;
            Check(!shouldRequest(true, "app|18765"), "Automatic firewall request can be disabled");
            firewallSettings.AutoRequestFirewall = true; firewallSettings.FirewallRequestFor = "app|18765";
            Check(!shouldRequest(true, "app|18765"), "Accepted or cancelled attempt is not repeated at every startup");
            Check(shouldRequest(true, "app|18766"), "Changed port requests new authorization");
            Check(shouldRequest(true, "moved-app|18765"), "Moved executable requests new authorization");
            MethodInfo buildRule = typeof(zClock.zClock).GetMethod("BuildPrompterFirewallScript", BindingFlags.Static | BindingFlags.NonPublic);
            string rule = (string)buildRule.Invoke(null, new object[] { "D:\\John's tools\\zClock.exe", 18765 });
            Check(rule.Contains("-Profile Private") && rule.Contains("-RemoteAddress LocalSubnet") && rule.Contains("-LocalPort 18765") && rule.Contains("-Program 'D:\\John''s tools\\zClock.exe'"), "Firewall rule is scoped and executable path safely quoted");
            Type serverType = assembly.GetType("zClock.PrompterServer");
            var probe = new TcpListener(IPAddress.Loopback, 0); probe.Start(); int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
            object server = Activator.CreateInstance(serverType, new object[] { port, false, key });
            string url = (string)serverType.GetProperty("LocalUrl").GetValue(server, null);
            try
            {
                Check(Get(url).Contains("跟随放映"), "Embedded responsive page serves UTF-8");
                Check(Status("http://127.0.0.1:" + port + "/", "GET") == 404, "Missing access key denied");
                Check(Status("http://127.0.0.1:" + port + "/wrong/state", "GET") == 404, "Incorrect access key denied");
                Check(Status(url + "state", "POST") == 405, "Remote writes denied");
                Check(Status(url + "../../App.config", "GET") == 404, "No filesystem traversal");
                var state = new PrompterState { presenting = true, previewEnabled = true, current = 1, total = 2, deck = "test", presentation = "示例演讲", timer = "15:00", status = "放映中", slides = new List<PrompterSlide> { new PrompterSlide { index = 1, title = "开场", notes = "中文备注\n<script>window.injected=1</script>" }, new PrompterSlide { index = 2, title = "第二页", notes = "下一页备注" } } };
                serverType.GetMethod("Publish").Invoke(server, new object[] { state });
                var parsed = new JavaScriptSerializer().Deserialize<PrompterState>(Get(url + "state"));
                Check(parsed.slides[0].notes == state.slides[0].notes, "Notes preserve Chinese, newlines and hostile text as data");
                Check(parsed.current == 1 && parsed.slides.Count == 2, "Current slide and preview data are synchronized");
                MethodInfo share = typeof(zClock.zClock).GetMethod("CreateSharedPrompterState", BindingFlags.Static | BindingFlags.NonPublic);
                var limited = (PrompterState)share.Invoke(null, new object[] { state, false });
                Check(limited.slides.Count == 1 && limited.slides[0].index == state.current && !limited.previewEnabled, "Default web sharing excludes notes from other slides");
                var allNotes = (PrompterState)share.Invoke(null, new object[] { state, true });
                Check(allNotes.slides.Count == 2 && allNotes.previewEnabled && state.slides.Count == 2, "Full notes sharing requires opt-in and leaves desktop state intact");
                MethodInfo subnet = serverType.GetMethod("IsSameSubnet", BindingFlags.Static | BindingFlags.NonPublic);
                Check((bool)subnet.Invoke(null, new object[] { IPAddress.Parse("192.168.1.50"), IPAddress.Parse("192.168.1.2"), IPAddress.Parse("255.255.255.0") }), "Local-subnet clients match");
                Check(!(bool)subnet.Invoke(null, new object[] { IPAddress.Parse("203.0.113.50"), IPAddress.Parse("192.168.1.2"), IPAddress.Parse("255.255.255.0") }), "Off-subnet clients are excluded");
                Check(!(bool)subnet.Invoke(null, new object[] { IPAddress.Parse("203.0.113.50"), IPAddress.Parse("192.168.1.2"), IPAddress.Any }), "Zero subnet mask cannot admit all Internet addresses");
                MethodInfo selectScreen = typeof(zClock.zClock).GetMethod("FindPrompterScreen", BindingFlags.Static | BindingFlags.NonPublic);
                var screens = new[] { new System.Drawing.Rectangle(0, 0, 1920, 1080), new System.Drawing.Rectangle(-1920, 0, 1920, 1080) };
                Check((int)selectScreen.Invoke(null, new object[] { screens, screens[0] }) == 1, "Multi-screen placement handles a secondary screen with negative coordinates");
                Check((int)selectScreen.Invoke(null, new object[] { screens, screens[1] }) == 0, "Multi-screen placement excludes the actual projection screen");
                Check((int)selectScreen.Invoke(null, new object[] { screens, new System.Drawing.Rectangle(-1920, 0, 3840, 1080) }) == -1, "Spanned projection does not expose notes on either projected screen");
                for (int i = 0; i < 30; i++) Check(Get(url + "state").Contains("示例演讲"), "Repeated polling " + i);
                Type windowType = assembly.GetType("zClock.PrompterWindow");
                using (var window = (Form)Activator.CreateInstance(windowType, new object[] { new PrompterSettings() }))
                {
                    windowType.GetMethod("UpdateState").Invoke(window, new object[] { state });
                    var notes = (RichTextBox)windowType.GetField("notes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                    Check(notes.Text == state.slides[0].notes, "Native window displays current notes");
                    windowType.GetMethod("Browse", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, new object[] { 1 });
                    Check(notes.Text == "下一页备注", "Native window previews adjacent notes");
                    state.current = 1;
                    windowType.GetMethod("UpdateState").Invoke(window, new object[] { state });
                    Check(notes.Text == "下一页备注", "Preview stays independent of current slide");
                    window.Size = new System.Drawing.Size(400, 500);
                    Check(notes.WordWrap && notes.Font.Size >= 18, "Native window wraps readable text after resize");
                }
                Type connectType = assembly.GetType("zClock.PrompterConnect");
                using (var bitmap = (System.Drawing.Bitmap)connectType.GetMethod("CreateCode", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { url, 360 }))
                {
                    bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qr-test.png"), System.Drawing.Imaging.ImageFormat.Png);
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qr-expected.txt"), url);
                    Check(bitmap.Width >= 200 && bitmap.GetPixel(0, 0).ToArgb() == System.Drawing.Color.White.ToArgb(), "Locally generated QR includes a clear white quiet zone");
                }
                using (var dialog = (Form)Activator.CreateInstance(connectType, new object[] { new[] { url, "http://192.168.1.10:" + port + "/" + key + "/" } }))
                {
                    var addresses = (ComboBox)connectType.GetField("addresses", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dialog);
                    Check(addresses.Items.Count == 1 && addresses.SelectedItem.ToString().Contains("192.168.1.10"), "Scan dialog excludes loopback and selects a LAN address");
                    dialog.Show(); Application.DoEvents();
                    using (var picture = new System.Drawing.Bitmap(dialog.Width, dialog.Height))
                    {
                        dialog.DrawToBitmap(picture, dialog.ClientRectangle);
                        picture.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qr-dialog.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                    dialog.Close();
                }
                if (args.Length > 0 && args[0] == "--serve")
                {
                    state.slides[0].notes = string.Join("\n\n", new string[] { "欢迎来到 zClock 提词器。", "这段备注会随着 PowerPoint 的当前页自动同步。\n无论是电脑、平板，还是手机，都可以清晰阅读。", "演讲时保持自然的语速。\n把重点留给听众，把提醒留在眼前。", state.slides[0].notes, new string('长', 180), "演讲结束，谢谢。" });
                    serverType.GetMethod("Publish").Invoke(server, new object[] { state });
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-url.txt"), url);
                    Console.WriteLine("SERVING " + url);
                    Thread.Sleep(300000);
                }
            }
            finally { ((IDisposable)server).Dispose(); }
            server = Activator.CreateInstance(serverType, new object[] { port, false, key });
            ((IDisposable)server).Dispose(); Check(true, "Service restarts and releases listening port");
            if (args.Length > 0 && args[0] == "--powerpoint") TestPowerPoint();
            Console.WriteLine("PASSED " + passed + " integration checks");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    static void TestPowerPoint()
    {
        if (Process.GetProcessesByName("POWERPNT").Length != 0) throw new Exception("PowerPoint is already running; skipping to protect existing work.");
        object appObject = null, deckObject = null, slideObject = null, showObject = null;
        try
        {
            appObject = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application"));
            dynamic app = appObject;
            deckObject = app.Presentations.Add(-1);
            dynamic deck = deckObject;
            slideObject = deck.Slides.Add(1, 1);
            dynamic slide = slideObject;
            slide.Shapes.Title.TextFrame.TextRange.Text = "集成测试标题";
            slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = "第一段备注\r第二段中文备注";
            MethodInfo read = typeof(zClock.zClock).GetMethod("ReadPrompterSlide", BindingFlags.Static | BindingFlags.NonPublic);
            var result = (PrompterSlide)read.Invoke(null, new object[] { slideObject, 1 });
            Check(result.title == "集成测试标题", "Real PowerPoint slide title extraction");
            Check(result.notes == "第一段备注\n第二段中文备注", "Real PowerPoint notes extraction and line normalization");
            slide.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = "";
            result = (PrompterSlide)read.Invoke(null, new object[] { slideObject, 1 });
            Check(result.notes == "", "Empty notes exclude page-number and date placeholders");
            deck.Slides.Add(2, 12);
            deck.SlideShowSettings.ShowType = 2;
            showObject = deck.SlideShowSettings.Run();
            dynamic show = showObject;
            show.View.GotoSlide(2);
            dynamic current = show.View.Slide;
            Check((int)current.SlideIndex == 2, "PowerPoint slide-show current-page API");
            int showHandle = (int)typeof(zClock.zClock).GetMethod("GetSlideShowHandle", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { showObject });
            IntPtr detected = (IntPtr)typeof(zClock.zClock).GetMethod("GetFullScreenProjectionHandle", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Check(detected == new IntPtr(showHandle), "Native COM discovers running slide shows without an Office assembly");
            using (var clock = new zClock.zClock())
            {
                var settings = (PrompterSettings)typeof(zClock.zClock).GetField("prompterSettings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clock);
                bool originalAutoRequest = settings.AutoRequestFirewall;
                settings.AutoRequestFirewall = false; // Integration tests must not trigger a real UAC request.
                clock.Show();
                FieldInfo timerValue = typeof(zClock.zClock).GetField("clocktimer", BindingFlags.Instance | BindingFlags.NonPublic);
                int initialSeconds = (int)timerValue.GetValue(clock);
                typeof(zClock.zClock).GetMethod("StartCountdown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(clock, null);
                Thread.Sleep(2150);
                typeof(zClock.zClock).GetMethod("StopCountdown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(clock, null);
                Check(initialSeconds - (int)timerValue.GetValue(clock) >= 2, "Blocked UI does not lose elapsed countdown seconds");
                typeof(zClock.zClock).GetField("pptWindow", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, showObject);
                typeof(zClock.zClock).GetField("intPtr", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new IntPtr(showHandle));
                MethodInfo update = typeof(zClock.zClock).GetMethod("UpdatePrompter", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo snapshot = typeof(zClock.zClock).GetField("prompterState", BindingFlags.Instance | BindingFlags.NonPublic);
                update.Invoke(clock, null);
                var state = (PrompterState)snapshot.GetValue(clock);
                Check(state.presenting && state.current == 2 && state.total == 2, "Full app reads running presentation and builds slide snapshot");
                show.View.GotoSlide(1);
                update.Invoke(clock, null);
                state = (PrompterState)snapshot.GetValue(clock);
                Check(state.current == 1 && state.slides.Count == 2, "Full app tracks slide changes and adjacent notes");
                typeof(zClock.zClock).GetMethod("OpenPrompterWindow", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(clock, new object[] { null, true });
                update.Invoke(clock, null);
                var localWindow = (Form)typeof(zClock.zClock).GetField("prompterWindow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clock);
                Check(localWindow.Visible, "Manually opened prompter remains visible on a single screen");
                localWindow.Close();
                MethodInfo settingDialog = typeof(zClock.zClock).GetMethod("时间设置ToolStripMenuItem_Click", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo paused = typeof(zClock.zClock).GetField("paused", BindingFlags.Instance | BindingFlags.NonPublic);
                paused.SetValue(clock, true);
                typeof(zClock.zClock).GetMethod("timeFormat", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(clock, new object[] { false });
                var clockLabel = (Label)typeof(zClock.zClock).GetField("clock_label", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clock);
                Check(clockLabel.Right <= clock.ClientSize.Width, "Paused clock label fits within the window");
                foreach (bool wasPaused in new[] { false, true })
                {
                    paused.SetValue(clock, wasPaused);
                    using (var dismiss = new System.Windows.Forms.Timer { Interval = 100 })
                    {
                        dismiss.Tick += (s, e) => { foreach (Form form in Application.OpenForms) { if (form is InputForm) { form.DialogResult = DialogResult.Cancel; break; } } };
                        dismiss.Start(); settingDialog.Invoke(clock, new object[] { null, EventArgs.Empty }); dismiss.Stop();
                    }
                    Check((bool)paused.GetValue(clock) == wasPaused, "Settings cancel restores previous pause state: " + wasPaused);
                }
                show.View.Exit();
                update.Invoke(clock, null);
                state = (PrompterState)snapshot.GetValue(clock);
                Check(!state.presenting && state.slides.Count == 0, "Full app clears notes when presentation ends");
                settings.AutoRequestFirewall = originalAutoRequest;
                clock.Close();
            }
        }
        finally
        {
            if (deckObject != null) { try { dynamic deck = deckObject; deck.Saved = -1; deck.Close(); } catch { } }
            if (appObject != null) { try { dynamic app = appObject; app.Quit(); } catch { } }
            Release(showObject); Release(slideObject); Release(deckObject); Release(appObject);
        }
    }
}
