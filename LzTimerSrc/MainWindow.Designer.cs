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
            this.lastBreakLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.notifyIconAllday = new System.Windows.Forms.NotifyIcon(this.components);
            this.signInWithGoogle = new System.Windows.Forms.Button();
            this.connectionStatus = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.taskLabel = new System.Windows.Forms.Label();
            this.taskTextBox = new System.Windows.Forms.TextBox();
            this.serverLabel = new System.Windows.Forms.Label();
            this.serverTextBox = new System.Windows.Forms.TextBox();
            this.awayLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.intervalTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.ForeColor = System.Drawing.Color.LimeGreen;
            this.label2.Location = new System.Drawing.Point(8, 16);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 33);
            this.label2.TabIndex = 1;
            this.label2.Text = "Today";
            // 
            // todayTimeLabel
            // 
            this.todayTimeLabel.AutoSize = true;
            this.todayTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.todayTimeLabel.ForeColor = System.Drawing.Color.LimeGreen;
            this.todayTimeLabel.Location = new System.Drawing.Point(150, 16);
            this.todayTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.todayTimeLabel.Name = "todayTimeLabel";
            this.todayTimeLabel.Size = new System.Drawing.Size(47, 33);
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
            // lastBreakLabel
            // 
            this.lastBreakLabel.AutoSize = true;
            this.lastBreakLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lastBreakLabel.ForeColor = System.Drawing.Color.Gray;
            this.lastBreakLabel.Location = new System.Drawing.Point(150, 49);
            this.lastBreakLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lastBreakLabel.Name = "lastBreakLabel";
            this.lastBreakLabel.Size = new System.Drawing.Size(47, 33);
            this.lastBreakLabel.TabIndex = 14;
            this.lastBreakLabel.Text = "4h";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label5.ForeColor = System.Drawing.Color.Gray;
            this.label5.Location = new System.Drawing.Point(8, 49);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(151, 33);
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
            this.signInWithGoogle.Location = new System.Drawing.Point(14, 272);
            this.signInWithGoogle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.signInWithGoogle.Name = "signInWithGoogle";
            this.signInWithGoogle.Size = new System.Drawing.Size(169, 29);
            this.signInWithGoogle.TabIndex = 16;
            this.signInWithGoogle.Text = "Sign in with Google";
            this.signInWithGoogle.UseVisualStyleBackColor = true;
            this.signInWithGoogle.Click += new System.EventHandler(this.signInWithGoogle_Click);
            // 
            // connectionStatus
            // 
            this.connectionStatus.AutoSize = true;
            this.connectionStatus.Location = new System.Drawing.Point(10, 248);
            this.connectionStatus.Name = "connectionStatus";
            this.connectionStatus.Size = new System.Drawing.Size(113, 20);
            this.connectionStatus.TabIndex = 20;
            this.connectionStatus.Text = "Not connected";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.taskLabel);
            this.groupBox1.Controls.Add(this.taskTextBox);
            this.groupBox1.Controls.Add(this.serverLabel);
            this.groupBox1.Controls.Add(this.serverTextBox);
            this.groupBox1.Controls.Add(this.awayLabel);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.intervalTextBox);
            this.groupBox1.Location = new System.Drawing.Point(14, 98);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 133);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Settings";
            // 
            // taskLabel
            // 
            this.taskLabel.AutoSize = true;
            this.taskLabel.Location = new System.Drawing.Point(6, 98);
            this.taskLabel.Name = "taskLabel";
            this.taskLabel.Size = new System.Drawing.Size(47, 20);
            this.taskLabel.TabIndex = 29;
            this.taskLabel.Text = "Task:";
            // 
            // taskTextBox
            // 
            this.taskTextBox.Location = new System.Drawing.Point(76, 95);
            this.taskTextBox.Name = "taskTextBox";
            this.taskTextBox.Size = new System.Drawing.Size(227, 26);
            this.taskTextBox.TabIndex = 28;
            // 
            // serverLabel
            // 
            this.serverLabel.AutoSize = true;
            this.serverLabel.Location = new System.Drawing.Point(6, 67);
            this.serverLabel.Name = "serverLabel";
            this.serverLabel.Size = new System.Drawing.Size(59, 20);
            this.serverLabel.TabIndex = 27;
            this.serverLabel.Text = "Server:";
            // 
            // serverTextBox
            // 
            this.serverTextBox.Location = new System.Drawing.Point(77, 61);
            this.serverTextBox.Name = "serverTextBox";
            this.serverTextBox.Size = new System.Drawing.Size(227, 26);
            this.serverTextBox.TabIndex = 26;
            this.serverTextBox.TextChanged += new System.EventHandler(this.serverTextBox_TextChanged);
            // 
            // awayLabel
            // 
            this.awayLabel.AutoSize = true;
            this.awayLabel.Location = new System.Drawing.Point(6, 33);
            this.awayLabel.Name = "awayLabel";
            this.awayLabel.Size = new System.Drawing.Size(51, 20);
            this.awayLabel.TabIndex = 25;
            this.awayLabel.Text = "Away:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(269, 33);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 20);
            this.label3.TabIndex = 24;
            this.label3.Text = "min";
            // 
            // intervalTextBox
            // 
            this.intervalTextBox.Location = new System.Drawing.Point(77, 27);
            this.intervalTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.intervalTextBox.Name = "intervalTextBox";
            this.intervalTextBox.Size = new System.Drawing.Size(184, 26);
            this.intervalTextBox.TabIndex = 23;
            this.intervalTextBox.TextChanged += new System.EventHandler(this.intervalTextBox_TextChanged);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(369, 311);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.connectionStatus);
            this.Controls.Add(this.signInWithGoogle);
            this.Controls.Add(this.lastBreakLabel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.todayTimeLabel);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainWindow";
            this.Text = "LzTimer";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.TestFormStatic_Resize);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label todayTimeLabel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Label lastBreakLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NotifyIcon notifyIconAllday;
        private System.Windows.Forms.Button signInWithGoogle;
        private System.Windows.Forms.Label connectionStatus;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label taskLabel;
        private System.Windows.Forms.TextBox taskTextBox;
        private System.Windows.Forms.Label serverLabel;
        private System.Windows.Forms.TextBox serverTextBox;
        private System.Windows.Forms.Label awayLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox intervalTextBox;
    }
}