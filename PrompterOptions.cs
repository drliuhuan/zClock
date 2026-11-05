// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Windows.Forms;

namespace zClock
{
    internal sealed class PrompterOptions : Form
    {
        readonly CheckBox enabled = new CheckBox { Text = "启动后台网页服务", AutoSize = true };
        readonly CheckBox lan = new CheckBox { Text = "允许同一局域网的手机 / 平板 / 电脑连接", AutoSize = true };
        readonly CheckBox auto = new CheckBox { Text = "放映时在非放映屏幕自动打开提词窗口", AutoSize = true };
        readonly CheckBox firewall = new CheckBox { Text = "首次启用局域网时自动申请防火墙放行（需管理员确认）", AutoSize = true };
        readonly CheckBox previews = new CheckBox { Text = "允许网页预览全部备注（关闭时只发送当前页）", AutoSize = true };
        readonly NumericUpDown port = new NumericUpDown { Minimum = 1024, Maximum = 65535, Width = 110 };
        readonly TextBox urls = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        readonly PrompterSettings settings;
        bool resetKey;
        public PrompterOptions(PrompterSettings settings, string[] addresses, string error)
        {
            this.settings = settings;
            Text = "提词器设置与连接地址";
            Size = new Size(700, 620);
            MinimumSize = new Size(550, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei", 10);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 10 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            enabled.Checked = settings.Enabled; lan.Checked = settings.AllowLan; auto.Checked = settings.AutoWindow; port.Value = settings.Port;
            firewall.Checked = settings.AutoRequestFirewall;
            previews.Checked = settings.SharePreviews;
            layout.Controls.Add(enabled); layout.Controls.Add(lan); layout.Controls.Add(firewall); layout.Controls.Add(previews); layout.Controls.Add(auto);
            var portRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            portRow.Controls.Add(new Label { Text = "服务端口", AutoSize = true, Padding = new Padding(0, 5, 8, 0) }); portRow.Controls.Add(port); layout.Controls.Add(portRow);
            layout.Controls.Add(new Label { Text = string.IsNullOrEmpty(error) ? "当前连接地址（修改服务设置后，请重新查看）：" : "服务状态：" + error, AutoSize = true, MaximumSize = new Size(600, 0), Padding = new Padding(0, 10, 0, 6) });
            urls.Text = addresses == null || addresses.Length == 0 ? "网页服务未运行。" : string.Join(Environment.NewLine + Environment.NewLine, addresses);
            layout.Controls.Add(urls);
            layout.Controls.Add(new Label { Text = "设备须能访问本机网络；Windows 防火墙提示时允许专用网络。\n地址含访问密钥，请只分享给演讲者。网页仅提供备注显示，不远程控制放映。\n手机浏览器可添加到主屏幕；镜像、字号、滚动速度在各浏览器单独保存。", AutoSize = true, MaximumSize = new Size(610, 0), Padding = new Padding(0, 10, 0, 8) });
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "保存", AutoSize = true, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
            var copy = new Button { Text = "复制连接地址", AutoSize = true };
            var reset = new Button { Text = "重置连接密钥", AutoSize = true };
            reset.Click += (s, e) => { resetKey = true; reset.Text = "保存后旧二维码失效"; reset.Enabled = false; };
            copy.Click += (s, e) => { if (addresses != null && addresses.Length > 0) { try { Clipboard.SetText(string.Join(Environment.NewLine, addresses)); } catch (System.Runtime.InteropServices.ExternalException) { MessageBox.Show("剪贴板正忙，请选中地址后复制。"); } } };
            buttons.Controls.Add(save); buttons.Controls.Add(cancel); buttons.Controls.Add(copy); buttons.Controls.Add(reset); layout.Controls.Add(buttons);
            Controls.Add(layout); AcceptButton = save; CancelButton = cancel;
        }
        public void Apply()
        {
            settings.Enabled = enabled.Checked; settings.AllowLan = lan.Checked; settings.AutoWindow = auto.Checked; settings.Port = (int)port.Value;
            settings.AutoRequestFirewall = firewall.Checked;
            settings.SharePreviews = previews.Checked;
            if (resetKey) settings.AccessKey = Guid.NewGuid().ToString("N");
        }
    }
}
