namespace SchnakyBuddy
{
    partial class Schnaky
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Schnaky));
            this.timerMouseCheck = new System.Windows.Forms.Timer(this.components);
            this.notifyIconSchnaky = new System.Windows.Forms.NotifyIcon(this.components);
            this.timerGrabWindow = new System.Windows.Forms.Timer(this.components);
            this.timerDragWindow = new System.Windows.Forms.Timer(this.components);
            this.timerMaxGrabTime = new System.Windows.Forms.Timer(this.components);
            this.timerScreenMelt = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // timerMouseCheck
            // 
            this.timerMouseCheck.Enabled = true;
            this.timerMouseCheck.Interval = 10;
            this.timerMouseCheck.Tick += new System.EventHandler(this.TimerMouseCheck_Tick);
            // 
            // notifyIconSchnaky
            // 
            this.notifyIconSchnaky.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIconSchnaky.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIconSchnaky.Icon")));
            this.notifyIconSchnaky.Text = "Schnaky";
            this.notifyIconSchnaky.Visible = true;
            this.notifyIconSchnaky.Click += new System.EventHandler(this.notifyIconSchnaky_Click);
            // 
            // timerGrabWindow
            // 
            this.timerGrabWindow.Interval = 10000;
            this.timerGrabWindow.Tick += new System.EventHandler(this.timerGrabWindow_Tick);
            // 
            // timerDragWindow
            // 
            this.timerDragWindow.Interval = 2000;
            this.timerDragWindow.Tick += new System.EventHandler(this.timerDragWindow_Tick);
            // 
            // timerMaxGrabTime
            // 
            this.timerMaxGrabTime.Interval = 7000;
            this.timerMaxGrabTime.Tick += new System.EventHandler(this.timerMaxGrabTime_Tick);
            // 
            // timerScreenMelt
            // 
            this.timerScreenMelt.Tick += new System.EventHandler(this.timerScreenMelt_Tick);
            // 
            // Schnaky
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(238, 238);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Schnaky";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Schnaky";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Black;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Schnaky_FormClosing);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Schnaky_Paint);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerMouseCheck;
        private System.Windows.Forms.NotifyIcon notifyIconSchnaky;
        private System.Windows.Forms.Timer timerGrabWindow;
        private System.Windows.Forms.Timer timerDragWindow;
        private System.Windows.Forms.Timer timerMaxGrabTime;
        private System.Windows.Forms.Timer timerScreenMelt;
    }
}