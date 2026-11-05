// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace zClock
{
    public partial class InputForm : Form
    {
        public int Input { get; set; } // 用于存储输入的值

        const int MARGIN = 10;
        const int DPI_SCALE = 96; // DPI 的缩放比例
        [DllImport("user32.dll")]
        static extern int GetDpiForWindow(IntPtr hWnd);

        public InputForm()
        {
            InitializeComponent();
            this.Text = "请输入时间";

        }

        private void InputForm_Load(object sender, EventArgs e)
        {
            //窗口位置和大小初始化
            FormAssetsLocationInitiation();
        }


        //窗口位置和大小初始化
        private void FormAssetsLocationInitiation()
        {
            // 获取标题栏的高度
            int captionHeight = System.Windows.Forms.SystemInformation.CaptionHeight;
            // 获取窗体边框的宽度
            int frameBorderWidth = System.Windows.Forms.SystemInformation.FrameBorderSize.Width;
            int frameBorderHeight = System.Windows.Forms.SystemInformation.FrameBorderSize.Height;

            //int dpi = GetDpiForWindow(this.Handle);
            int dpi = 96;
            Console.WriteLine("DPI:" + dpi.ToString());
            double dpiscale = (double)dpi / 96;
            Console.WriteLine("dpiscale:"+dpiscale.ToString());
            int margin = (int)Math.Round(MARGIN * dpiscale);
            Console.WriteLine("margin:" + margin.ToString());
            label1.Location = new Point(margin, margin + (int)Math.Round(7 * dpiscale));
            int labelWidth = label1.Width;
            Console.WriteLine("labelWidth:" + labelWidth.ToString());
            textBox1.Location = new Point(margin + labelWidth + margin, margin);
            //textBox1.Width = 50;
            int textBoxHeight = (int)Math.Round(textBox1.Height * dpiscale);
            Console.WriteLine("textBoxHeight:" + textBoxHeight.ToString());
            int textBoxWidth = (int)Math.Round(textBox1.Width * dpiscale);
            Console.WriteLine("textBoxWidth:" + textBoxWidth.ToString());
            int buttonWidth = (int)Math.Round(button2.Width * dpiscale);
            Console.WriteLine("buttonWidth:" + buttonWidth.ToString());
            int buttonHeight = (int)Math.Round(button2.Height * dpiscale);
            Console.WriteLine("buttonHeight:" + buttonHeight.ToString());
            button2.Location = new Point(margin + labelWidth + margin + textBoxWidth - buttonWidth, margin + textBoxHeight + margin);
            button1.Location = new Point(button2.Location.X - margin - buttonWidth, button2.Location.Y);
            //this.Width = 2*frameBorderWidth+textBox1.Location.X + textBox1.Width + MARGIN;
            this.Width = 2*frameBorderWidth + textBox1.Location.X + textBox1.Width + 2*MARGIN;

            Console.WriteLine(this.Width.ToString());
            //this.Height = 2* frameBorderWidth+captionHeight + button2.Location.Y + button2.Height + MARGIN;
            this.Height = 2*frameBorderHeight + captionHeight + button2.Location.Y + button2.Height + 2*MARGIN;
            Console.WriteLine(this.Height.ToString());
        }

        // 将字符串转换为秒为单位的时间的函数
        public static int GetSeconds(string input)
        {
            int seconds;
            if (TryGetSeconds(input, out seconds)) return seconds;
            ShowErrorMessage(string.IsNullOrWhiteSpace(input) ? "空值" : input);
            return 0;
        }

        public static bool TryGetSeconds(string input, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            long total;
            if (input.Contains(":"))
            {
                string[] parts = input.Split(':');
                if (parts.Length != 2 && parts.Length != 3) return false;
                total = 0;
                foreach (string partText in parts)
                {
                    int part;
                    if (!int.TryParse(partText, out part) || part < 0) return false;
                    total = total * 60 + part;
                    if (total > int.MaxValue) return false;
                }
            }
            else
            {
                string[] suffixes = { "秒钟", "分钟", "小时", "s", "秒", "m", "分", "h", "时" };
                int[] factors = { 1, 60, 3600, 1, 1, 60, 60, 3600, 3600 };
                int factor = 1;
                for (int i = 0; i < suffixes.Length; i++)
                {
                    if (!input.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase)) continue;
                    input = input.Substring(0, input.Length - suffixes[i].Length);
                    factor = factors[i];
                    break;
                }
                int value;
                if (!int.TryParse(input, out value) || value <= 0) return false;
                total = (long)value * factor;
            }
            if (total <= 0 || total > int.MaxValue) return false;
            seconds = (int)total;
            return true;
        }

        // 显示错误信息的函数
        public static void ShowErrorMessage(string input)
        {
            System.Windows.Forms.MessageBox.Show("输入的字符串不是有效的时间格式，请输入以下格式之一的字符串：\n\n" +
                            "5400, 5400s, 5400秒, 5400秒钟, 90m, 90分, 90分钟, 90:00, 1:30:00, 1h, 1小时\n\n" +
                            "你输入的字符串是：" + input, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            int input= GetSeconds(textBox1.Text);
            if (input > 0)
            {
                Input = input;  // 获取输入框的值
                DialogResult = DialogResult.OK; // 设置对话框的结果为确定
            }
            else
            { 
                textBox1.Text=Properties.Settings.Default.time.ToString();
                textBox1.SelectAll();
            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel; // 设置对话框的结果为确定
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            textBox1.Text= Properties.Settings.Default.time.ToString();
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText; 
            textBox1.SelectAll();
        }


    }
}
