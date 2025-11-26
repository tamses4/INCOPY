namespace WinFormsApp9
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            btnStart = new Button();
            txtOutput = new TextBox();
            button1 = new Button();
            listView2 = new ListBox();
            listView1 = new ListBox();
            button2 = new Button();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Copperplate Gothic Bold", 28.2F, FontStyle.Underline, GraphicsUnit.Point, 0);
            label1.ForeColor = SystemColors.ButtonHighlight;
            label1.Location = new Point(168, 9);
            label1.Name = "label1";
            label1.Size = new Size(496, 53);
            label1.TabIndex = 0;
            label1.Text = "TRANSFER_MADE\r\n";
            label1.Click += label1_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(550, 4);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(156, 58);
            btnStart.TabIndex = 1;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Visible = false;
            // 
            // txtOutput
            // 
            txtOutput.Location = new Point(550, 68);
            txtOutput.Name = "txtOutput";
            txtOutput.Size = new Size(212, 27);
            txtOutput.TabIndex = 2;
            txtOutput.Visible = false;
            // 
            // button1
            // 
            button1.Location = new Point(12, 9);
            button1.Name = "button1";
            button1.Size = new Size(117, 53);
            button1.TabIndex = 5;
            button1.Text = "REFRESH";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // listView2
            // 
            listView2.FormattingEnabled = true;
            listView2.Location = new Point(12, 191);
            listView2.Name = "listView2";
            listView2.Size = new Size(472, 224);
            listView2.TabIndex = 6;
            // 
            // listView1
            // 
            listView1.FormattingEnabled = true;
            listView1.Location = new Point(550, 191);
            listView1.Name = "listView1";
            listView1.Size = new Size(219, 224);
            listView1.TabIndex = 7;
            // 
            // button2
            // 
            button2.BackColor = Color.Red;
            button2.Location = new Point(641, 443);
            button2.Name = "button2";
            button2.Size = new Size(128, 41);
            button2.TabIndex = 8;
            button2.Text = "Close";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = SystemColors.Control;
            label2.Location = new Point(15, 159);
            label2.Name = "label2";
            label2.Size = new Size(114, 20);
            label2.TabIndex = 9;
            label2.Text = "Fichier en cours:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = SystemColors.Control;
            label3.Location = new Point(550, 168);
            label3.Name = "label3";
            label3.Size = new Size(138, 20);
            label3.TabIndex = 10;
            label3.Text = "Adresses du reseau:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0, 0, 64);
            ClientSize = new Size(800, 518);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(button2);
            Controls.Add(listView1);
            Controls.Add(listView2);
            Controls.Add(button1);
            Controls.Add(txtOutput);
            Controls.Add(label1);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "IN-COPY";
            Load += Form1_Load_1;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button btnStart;
        private TextBox txtOutput;
        private Button button1;
        private ListBox listView2;
        private ListBox listView1;
        private Button button2;
        private Label label2;
        private Label label3;
    }
}
