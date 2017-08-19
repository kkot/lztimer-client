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
            this.todayTimeLabel = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.intervalTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lastBreakLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.notifyIconAllday = new System.Windows.Forms.NotifyIcon(this.components);
            this.signInWithGoogle = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.ForeColor = System.Drawing.Color.LimeGreen;
            this.label2.Location = new System.Drawing.Point(16, 11);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 29);
            this.label2.TabIndex = 1;
            this.label2.Text = "Today";
            // 
            // todayTimeLabel
            // 
            this.todayTimeLabel.AutoSize = true;
            this.todayTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.todayTimeLabel.ForeColor = System.Drawing.Color.LimeGreen;
            this.todayTimeLabel.Location = new System.Drawing.Point(169, 11);
            this.todayTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.todayTimeLabel.Name = "todayTimeLabel";
            this.todayTimeLabel.Size = new System.Drawing.Size(39, 29);
            this.todayTimeLabel.TabIndex = 3;
            this.todayTimeLabel.Text = "2h";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 10000;
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
            this.intervalTextBox.Location = new System.Drawing.Point(101, 79);
            this.intervalTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.intervalTextBox.Name = "intervalTextBox";
            this.intervalTextBox.Size = new System.Drawing.Size(91, 22);
            this.intervalTextBox.TabIndex = 9;
            this.intervalTextBox.TextChanged += new System.EventHandler(this.intervalTextBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(201, 82);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 17);
            this.label3.TabIndex = 10;
            this.label3.Text = "min";
            // 
            // lastBreakLabel
            // 
            this.lastBreakLabel.AutoSize = true;
            this.lastBreakLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lastBreakLabel.ForeColor = System.Drawing.Color.Gray;
            this.lastBreakLabel.Location = new System.Drawing.Point(169, 41);
            this.lastBreakLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lastBreakLabel.Name = "lastBreakLabel";
            this.lastBreakLabel.Size = new System.Drawing.Size(39, 29);
            this.lastBreakLabel.TabIndex = 14;
            this.lastBreakLabel.Text = "4h";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label5.ForeColor = System.Drawing.Color.Gray;
            this.label5.Location = new System.Drawing.Point(16, 41);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 29);
            this.label5.TabIndex = 13;
            this.label5.Text = "Last break";
            // 
            // notifyIconAllday
            // 
            this.notifyIconAllday.Text = "notifyIcon1";
            this.notifyIconAllday.Visible = true;
            this.notifyIconAllday.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // signInWithGoogle
            // 
            this.signInWithGoogle.Location = new System.Drawing.Point(21, 118);
            this.signInWithGoogle.Name = "signInWithGoogle";
            this.signInWithGoogle.Size = new System.Drawing.Size(150, 23);
            this.signInWithGoogle.TabIndex = 16;
            this.signInWithGoogle.Text = "Sign in with Google";
            this.signInWithGoogle.UseVisualStyleBackColor = true;
            this.signInWithGoogle.Click += new System.EventHandler(this.signInWithGoogle_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(357, 153);
            this.Controls.Add(this.signInWithGoogle);
            this.Controls.Add(this.lastBreakLabel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.intervalTextBox);
            this.Controls.Add(this.todayTimeLabel);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
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
        private System.Windows.Forms.Label todayTimeLabel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.TextBox intervalTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lastBreakLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NotifyIcon notifyIconAllday;
        private System.Windows.Forms.Button signInWithGoogle;
    }
}