// SPDX-License-Identifier: GPL-3.0-only
namespace zClock
{
    partial class zClock
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(zClock));
            this.clock_label = new System.Windows.Forms.Label();
            this.timer_checkFullScreen = new System.Windows.Forms.Timer(this.components);
            this.clock_Timer = new System.Windows.Forms.Timer(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ResetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // clock_label
            // 
            this.clock_label.AutoSize = true;
            this.clock_label.BackColor = System.Drawing.Color.Transparent;
            this.clock_label.Font = new System.Drawing.Font("Consolas", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clock_label.Location = new System.Drawing.Point(11, 9);
            this.clock_label.Name = "clock_label";
            this.clock_label.Size = new System.Drawing.Size(172, 61);
            this.clock_label.TabIndex = 0;
            this.clock_label.Text = "20:00";
            this.clock_label.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ThisMouseDown);
            this.clock_label.MouseEnter += new System.EventHandler(this.clock_label_MouseEnter);
            this.clock_label.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ThisMouseMove);
            this.clock_label.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ThisMouseUp);
            // 
            // timer_checkFullScreen
            // 
            this.timer_checkFullScreen.Enabled = true;
            this.timer_checkFullScreen.Interval = 1000;
            this.timer_checkFullScreen.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // clock_Timer
            // 
            this.clock_Timer.Interval = 1000;
            this.clock_Timer.Tick += new System.EventHandler(this.clock_Timer_Tick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ResetToolStripMenuItem,
            this.SettingsToolStripMenuItem,
            this.HideToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(189, 124);
            // 
            // ResetToolStripMenuItem
            // 
            this.ResetToolStripMenuItem.Name = "ResetToolStripMenuItem";
            this.ResetToolStripMenuItem.Size = new System.Drawing.Size(188, 30);
            this.ResetToolStripMenuItem.Text = "重置时间";
            this.ResetToolStripMenuItem.Click += new System.EventHandler(this.ResetToolStripMenuItem_Click);
            // 
            // SettingsToolStripMenuItem
            // 
            this.SettingsToolStripMenuItem.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.SettingsToolStripMenuItem.Name = "SettingsToolStripMenuItem";
            this.SettingsToolStripMenuItem.Size = new System.Drawing.Size(188, 30);
            this.SettingsToolStripMenuItem.Text = "时间设置";
            this.SettingsToolStripMenuItem.Click += new System.EventHandler(this.时间设置ToolStripMenuItem_Click);
            // 
            // HideToolStripMenuItem
            // 
            this.HideToolStripMenuItem.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.HideToolStripMenuItem.CheckOnClick = true;
            this.HideToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.HideToolStripMenuItem.Name = "HideToolStripMenuItem";
            this.HideToolStripMenuItem.Size = new System.Drawing.Size(188, 30);
            this.HideToolStripMenuItem.Text = "未放映时隐藏";
            this.HideToolStripMenuItem.Click += new System.EventHandler(this.hideToolStripMenuItem_CheckedChanged);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(188, 30);
            this.ExitToolStripMenuItem.Text = "退出";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "zClock计时器";
            this.notifyIcon1.Visible = true;
            // 
            // zClock
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.DodgerBlue;
            this.ClientSize = new System.Drawing.Size(183, 89);
            this.Controls.Add(this.clock_label);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "zClock";
            this.Opacity = 0.5D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "zClock";
            this.TopMost = true;
            this.Deactivate += new System.EventHandler(this.Form1_Deactivate);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ThisMouseDown);
            this.MouseEnter += new System.EventHandler(this.Form1_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.Form1_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ThisMouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ThisMouseUp);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label clock_label;
        private System.Windows.Forms.Timer timer_checkFullScreen;
        private System.Windows.Forms.Timer clock_Timer;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem SettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ToolStripMenuItem ResetToolStripMenuItem;
    }
}

