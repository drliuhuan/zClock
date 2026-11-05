// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QRCoder;

namespace zClock
{
    internal sealed class PrompterConnect : Form
    {
        readonly ComboBox addresses = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        readonly PictureBox qr = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.White };
        readonly TextBox link = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
        public PrompterConnect(string[] urls) : this(urls, null) { }
        public PrompterConnect(string[] urls, Action allowNetwork)
        {
            Text = "手机扫码连接 · zClock 提词器";
            Font = new Font("Microsoft YaHei", 10);
            Size = new Size(620, 680);
            MinimumSize = new Size(480, 580);
            StartPosition = FormStartPosition.CenterScreen;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 6, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = "① 手机连接同一 Wi-Fi / 局域网\n② 使用相机或微信扫一扫，打开浏览器即可连接", AutoSize = true, Padding = new Padding(0, 0, 0, 12) });
            foreach (string url in urls.Where(value => !value.Contains("127.0.0.1")).OrderBy(value => value.Contains("192.168.") ? 0 : 1)) addresses.Items.Add(url);
            if (addresses.Items.Count == 0) throw new InvalidOperationException("未找到可用的局域网 IPv4 地址，请先连接 Wi-Fi 或网线。");
            layout.Controls.Add(addresses); layout.Controls.Add(qr); layout.Controls.Add(link);
            layout.Controls.Add(new Label { Text = "多网卡时请选择与手机同一网段的地址。\n若打不开，请允许 zClock 通过 Windows 防火墙的专用网络，\n并确认路由器未开启访客网络隔离。二维码含访问密钥，请勿公开。", AutoSize = true, MaximumSize = new Size(560, 0), Padding = new Padding(0, 10, 0, 8) });
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new Button { Text = "关闭", AutoSize = true, DialogResult = DialogResult.Cancel };
            var copy = new Button { Text = "复制地址", AutoSize = true };
            copy.Click += (s, e) => { try { Clipboard.SetText(link.Text); } catch (System.Runtime.InteropServices.ExternalException) { MessageBox.Show("剪贴板正忙，请选中地址后复制。"); } };
            buttons.Controls.Add(close); buttons.Controls.Add(copy); layout.Controls.Add(buttons);
            if (allowNetwork != null)
            {
                var firewall = new Button { Text = "允许局域网访问", AutoSize = true };
                firewall.Click += (s, e) => allowNetwork();
                buttons.Controls.Add(firewall);
            }
            Controls.Add(layout); CancelButton = close;
            addresses.SelectedIndexChanged += (s, e) => RenderCode();
            qr.Resize += (s, e) => RenderCode();
            addresses.SelectedIndex = 0;
            FormClosed += (s, e) => { if (qr.Image != null) qr.Image.Dispose(); };
        }

        internal static Bitmap CreateCode(string url, int availablePixels)
        {
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M))
            using (var renderer = new QRCode(data))
            {
                int moduleSize = Math.Max(2, Math.Min(12, availablePixels / data.ModuleMatrix.Count));
                return renderer.GetGraphic(moduleSize, Color.Black, Color.White, true);
            }
        }

        void RenderCode()
        {
            if (addresses.SelectedItem == null) return;
            link.Text = addresses.SelectedItem.ToString();
            Image previous = qr.Image;
            qr.Image = CreateCode(link.Text, Math.Max(180, Math.Min(qr.ClientSize.Width, qr.ClientSize.Height) - 20));
            if (previous != null) previous.Dispose();
        }
    }
}
