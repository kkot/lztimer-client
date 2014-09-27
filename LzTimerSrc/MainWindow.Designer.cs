namespace kkot.LzTimer
{
    partial class MainWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.label2 = new System.Windows.Forms.Label();
            this.funTimeLabel = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.intervalTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.historyButton = new System.Windows.Forms.Button();
            this.lastBreakLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.notifyIconAllday = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.ForeColor = System.Drawing.Color.LimeGreen;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(22, 24);
            this.label2.TabIndex = 1;
            this.label2.Text = "F";
            // 
            // funTimeLabel
            // 
            this.funTimeLabel.AutoSize = true;
            this.funTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.funTimeLabel.ForeColor = System.Drawing.Color.LimeGreen;
            this.funTimeLabel.Location = new System.Drawing.Point(76, 9);
            this.funTimeLabel.Name = "funTimeLabel";
            this.funTimeLabel.Size = new System.Drawing.Size(31, 24);
            this.funTimeLabel.TabIndex = 3;
            this.funTimeLabel.Text = "2h";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Text = "notifyIcon1";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // intervalTextBox
            // 
            this.intervalTextBox.Location = new System.Drawing.Point(76, 60);
            this.intervalTextBox.Name = "intervalTextBox";
            this.intervalTextBox.Size = new System.Drawing.Size(69, 20);
            this.intervalTextBox.TabIndex = 9;
            this.intervalTextBox.TextChanged += new System.EventHandler(this.intervalTextBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(151, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(23, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "min";
            // 
            // historyButton
            // 
            this.historyButton.Location = new System.Drawing.Point(12, 60);
            this.historyButton.Name = "historyButton";
            this.historyButton.Size = new System.Drawing.Size(58, 20);
            this.historyButton.TabIndex = 12;
            this.historyButton.Text = "history";
            this.historyButton.UseVisualStyleBackColor = true;
            this.historyButton.Click += new System.EventHandler(this.historyButton_Click);
            // 
            // lastBreakLabel
            // 
            this.lastBreakLabel.AutoSize = true;
            this.lastBreakLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lastBreakLabel.ForeColor = System.Drawing.Color.Gray;
            this.lastBreakLabel.Location = new System.Drawing.Point(76, 33);
            this.lastBreakLabel.Name = "lastBreakLabel";
            this.lastBreakLabel.Size = new System.Drawing.Size(31, 24);
            this.lastBreakLabel.TabIndex = 14;
            this.lastBreakLabel.Text = "4h";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label5.ForeColor = System.Drawing.Color.Gray;
            this.label5.Location = new System.Drawing.Point(12, 33);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(22, 24);
            this.label5.TabIndex = 13;
            this.label5.Text = "B";
            // 
            // notifyIconAllday
            // 
            this.notifyIconAllday.Text = "notifyIcon1";
            this.notifyIconAllday.Visible = true;
            this.notifyIconAllday.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(237, 92);
            this.Controls.Add(this.lastBreakLabel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.historyButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.intervalTextBox);
            this.Controls.Add(this.funTimeLabel);
            this.Controls.Add(this.label2);
            this.Name = "MainWindow";
            this.Text = "LzTimer";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.TestFormStatic_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label funTimeLabel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.TextBox intervalTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button historyButton;
        private System.Windows.Forms.Label lastBreakLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NotifyIcon notifyIconAllday;

    }
}