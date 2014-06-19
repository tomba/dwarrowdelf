namespace Client3D
{
	partial class DebugForm
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.camPosTextBox = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.chunkRecalcsTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.vertRendTextBox = new System.Windows.Forms.TextBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.ypCutTrackBar = new System.Windows.Forms.TrackBar();
			this.label7 = new System.Windows.Forms.Label();
			this.ynCutTrackBar = new System.Windows.Forms.TrackBar();
			this.label8 = new System.Windows.Forms.Label();
			this.xpCutTrackBar = new System.Windows.Forms.TrackBar();
			this.label6 = new System.Windows.Forms.Label();
			this.xnCutTrackBar = new System.Windows.Forms.TrackBar();
			this.label4 = new System.Windows.Forms.Label();
			this.viewCorner2TextBox = new System.Windows.Forms.TextBox();
			this.zCutTrackBar = new System.Windows.Forms.TrackBar();
			this.label5 = new System.Windows.Forms.Label();
			this.viewCorner1TextBox = new System.Windows.Forms.TextBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.checkBox5 = new System.Windows.Forms.CheckBox();
			this.checkBox4 = new System.Windows.Forms.CheckBox();
			this.checkBox3 = new System.Windows.Forms.CheckBox();
			this.checkBox2 = new System.Windows.Forms.CheckBox();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ypCutTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ynCutTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.xpCutTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.xnCutTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.zCutTrackBar)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.camPosTextBox);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(260, 54);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Camera";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Position";
			// 
			// camPosTextBox
			// 
			this.camPosTextBox.Location = new System.Drawing.Point(56, 19);
			this.camPosTextBox.Name = "camPosTextBox";
			this.camPosTextBox.ReadOnly = true;
			this.camPosTextBox.Size = new System.Drawing.Size(198, 20);
			this.camPosTextBox.TabIndex = 0;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.chunkRecalcsTextBox);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.vertRendTextBox);
			this.groupBox2.Location = new System.Drawing.Point(12, 72);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(260, 77);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Scene";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 48);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(80, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Chunk Recalcs";
			// 
			// chunkRecalcsTextBox
			// 
			this.chunkRecalcsTextBox.Location = new System.Drawing.Point(107, 45);
			this.chunkRecalcsTextBox.Name = "chunkRecalcsTextBox";
			this.chunkRecalcsTextBox.ReadOnly = true;
			this.chunkRecalcsTextBox.Size = new System.Drawing.Size(147, 20);
			this.chunkRecalcsTextBox.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 22);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(95, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Vertices Rendered";
			// 
			// vertRendTextBox
			// 
			this.vertRendTextBox.Location = new System.Drawing.Point(107, 19);
			this.vertRendTextBox.Name = "vertRendTextBox";
			this.vertRendTextBox.ReadOnly = true;
			this.vertRendTextBox.Size = new System.Drawing.Size(147, 20);
			this.vertRendTextBox.TabIndex = 0;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.ypCutTrackBar);
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Controls.Add(this.ynCutTrackBar);
			this.groupBox3.Controls.Add(this.label8);
			this.groupBox3.Controls.Add(this.xpCutTrackBar);
			this.groupBox3.Controls.Add(this.label6);
			this.groupBox3.Controls.Add(this.xnCutTrackBar);
			this.groupBox3.Controls.Add(this.label4);
			this.groupBox3.Controls.Add(this.viewCorner2TextBox);
			this.groupBox3.Controls.Add(this.zCutTrackBar);
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.viewCorner1TextBox);
			this.groupBox3.Location = new System.Drawing.Point(12, 155);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(260, 315);
			this.groupBox3.TabIndex = 4;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Map View Area";
			// 
			// ypCutTrackBar
			// 
			this.ypCutTrackBar.Location = new System.Drawing.Point(31, 253);
			this.ypCutTrackBar.Name = "ypCutTrackBar";
			this.ypCutTrackBar.Size = new System.Drawing.Size(176, 45);
			this.ypCutTrackBar.TabIndex = 14;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(11, 259);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(20, 13);
			this.label7.TabIndex = 13;
			this.label7.Text = "Y+";
			// 
			// ynCutTrackBar
			// 
			this.ynCutTrackBar.Location = new System.Drawing.Point(31, 202);
			this.ynCutTrackBar.Name = "ynCutTrackBar";
			this.ynCutTrackBar.Size = new System.Drawing.Size(176, 45);
			this.ynCutTrackBar.TabIndex = 11;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(11, 208);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(17, 13);
			this.label8.TabIndex = 10;
			this.label8.Text = "Y-";
			// 
			// xpCutTrackBar
			// 
			this.xpCutTrackBar.Location = new System.Drawing.Point(31, 147);
			this.xpCutTrackBar.Name = "xpCutTrackBar";
			this.xpCutTrackBar.Size = new System.Drawing.Size(176, 45);
			this.xpCutTrackBar.TabIndex = 8;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(11, 153);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(20, 13);
			this.label6.TabIndex = 7;
			this.label6.Text = "X+";
			// 
			// xnCutTrackBar
			// 
			this.xnCutTrackBar.Location = new System.Drawing.Point(31, 96);
			this.xnCutTrackBar.Name = "xnCutTrackBar";
			this.xnCutTrackBar.Size = new System.Drawing.Size(176, 45);
			this.xnCutTrackBar.TabIndex = 5;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(11, 102);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 13);
			this.label4.TabIndex = 4;
			this.label4.Text = "X-";
			// 
			// viewCorner2TextBox
			// 
			this.viewCorner2TextBox.Location = new System.Drawing.Point(130, 19);
			this.viewCorner2TextBox.Name = "viewCorner2TextBox";
			this.viewCorner2TextBox.ReadOnly = true;
			this.viewCorner2TextBox.Size = new System.Drawing.Size(118, 20);
			this.viewCorner2TextBox.TabIndex = 3;
			// 
			// zCutTrackBar
			// 
			this.zCutTrackBar.Location = new System.Drawing.Point(31, 45);
			this.zCutTrackBar.Name = "zCutTrackBar";
			this.zCutTrackBar.Size = new System.Drawing.Size(176, 45);
			this.zCutTrackBar.TabIndex = 2;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(11, 48);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(14, 13);
			this.label5.TabIndex = 1;
			this.label5.Text = "Z";
			// 
			// viewCorner1TextBox
			// 
			this.viewCorner1TextBox.Location = new System.Drawing.Point(6, 19);
			this.viewCorner1TextBox.Name = "viewCorner1TextBox";
			this.viewCorner1TextBox.ReadOnly = true;
			this.viewCorner1TextBox.Size = new System.Drawing.Size(118, 20);
			this.viewCorner1TextBox.TabIndex = 0;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.checkBox5);
			this.groupBox4.Controls.Add(this.checkBox4);
			this.groupBox4.Controls.Add(this.checkBox3);
			this.groupBox4.Controls.Add(this.checkBox2);
			this.groupBox4.Controls.Add(this.checkBox1);
			this.groupBox4.Location = new System.Drawing.Point(12, 477);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(260, 140);
			this.groupBox4.TabIndex = 5;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Options";
			// 
			// checkBox5
			// 
			this.checkBox5.AutoSize = true;
			this.checkBox5.Location = new System.Drawing.Point(6, 91);
			this.checkBox5.Name = "checkBox5";
			this.checkBox5.Size = new System.Drawing.Size(111, 17);
			this.checkBox5.TabIndex = 4;
			this.checkBox5.Text = "Disable Occlusion";
			this.checkBox5.UseVisualStyleBackColor = true;
			// 
			// checkBox4
			// 
			this.checkBox4.AutoSize = true;
			this.checkBox4.Location = new System.Drawing.Point(6, 67);
			this.checkBox4.Name = "checkBox4";
			this.checkBox4.Size = new System.Drawing.Size(87, 17);
			this.checkBox4.TabIndex = 3;
			this.checkBox4.Text = "Disable Light";
			this.checkBox4.UseVisualStyleBackColor = true;
			// 
			// checkBox3
			// 
			this.checkBox3.AutoSize = true;
			this.checkBox3.Location = new System.Drawing.Point(6, 43);
			this.checkBox3.Name = "checkBox3";
			this.checkBox3.Size = new System.Drawing.Size(98, 17);
			this.checkBox3.TabIndex = 2;
			this.checkBox3.Text = "Enable Borders";
			this.checkBox3.UseVisualStyleBackColor = true;
			// 
			// checkBox2
			// 
			this.checkBox2.AutoSize = true;
			this.checkBox2.Location = new System.Drawing.Point(107, 19);
			this.checkBox2.Name = "checkBox2";
			this.checkBox2.Size = new System.Drawing.Size(74, 17);
			this.checkBox2.TabIndex = 1;
			this.checkBox2.Text = "Wireframe";
			this.checkBox2.UseVisualStyleBackColor = true;
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(6, 20);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(95, 17);
			this.checkBox1.TabIndex = 0;
			this.checkBox1.Text = "Disable Culling";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// DebugForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 629);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "DebugForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "DebugForm";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ypCutTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ynCutTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.xpCutTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.xnCutTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.zCutTrackBar)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		internal System.Windows.Forms.TextBox camPosTextBox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label2;
		internal System.Windows.Forms.TextBox vertRendTextBox;
		private System.Windows.Forms.Label label3;
		internal System.Windows.Forms.TextBox chunkRecalcsTextBox;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label5;
		internal System.Windows.Forms.TextBox viewCorner1TextBox;
		internal System.Windows.Forms.TrackBar zCutTrackBar;
		internal System.Windows.Forms.TrackBar xpCutTrackBar;
		private System.Windows.Forms.Label label6;
		internal System.Windows.Forms.TrackBar xnCutTrackBar;
		private System.Windows.Forms.Label label4;
		internal System.Windows.Forms.TextBox viewCorner2TextBox;
		internal System.Windows.Forms.TrackBar ypCutTrackBar;
		private System.Windows.Forms.Label label7;
		internal System.Windows.Forms.TrackBar ynCutTrackBar;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.CheckBox checkBox2;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.CheckBox checkBox4;
		private System.Windows.Forms.CheckBox checkBox3;
		private System.Windows.Forms.CheckBox checkBox5;

	}
}