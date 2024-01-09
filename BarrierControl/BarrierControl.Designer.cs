namespace BarrierControl
{
    partial class BarrierControl
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
            this.pbBarrier = new System.Windows.Forms.PictureBox();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbBarrier)).BeginInit();
            this.SuspendLayout();
            // 
            // pbBarrier
            // 
            this.pbBarrier.Image = global::BarrierControl.Properties.Resources.barrier_closed;
            this.pbBarrier.Location = new System.Drawing.Point(58, 27);
            this.pbBarrier.Name = "pbBarrier";
            this.pbBarrier.Size = new System.Drawing.Size(841, 348);
            this.pbBarrier.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbBarrier.TabIndex = 1;
            this.pbBarrier.TabStop = false;
            // 
            // btnOpen
            // 
            this.btnOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpen.Location = new System.Drawing.Point(254, 412);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(179, 50);
            this.btnOpen.TabIndex = 3;
            this.btnOpen.Text = "Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(524, 412);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(179, 50);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // BarrierControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(954, 500);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.pbBarrier);
            this.Name = "BarrierControl";
            this.Text = "BarrierControl";
            this.Shown += new System.EventHandler(this.BarrierControl_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbBarrier)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox pbBarrier;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
    }
}

