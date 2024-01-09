namespace Barrier
{
    partial class Barrier
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
            this.lblState = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbBarrier)).BeginInit();
            this.SuspendLayout();
            // 
            // pbBarrier
            // 
            this.pbBarrier.Image = global::Barrier.Properties.Resources.barrier_closed;
            this.pbBarrier.Location = new System.Drawing.Point(72, 12);
            this.pbBarrier.Name = "pbBarrier";
            this.pbBarrier.Size = new System.Drawing.Size(660, 308);
            this.pbBarrier.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbBarrier.TabIndex = 0;
            this.pbBarrier.TabStop = false;
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblState.Location = new System.Drawing.Point(288, 362);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(234, 31);
            this.lblState.TabIndex = 1;
            this.lblState.Text = "The gate is closed";
            // 
            // Barrier
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.pbBarrier);
            this.Name = "Barrier";
            this.Text = "Barrier";
            this.Shown += new System.EventHandler(this.Barrier_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbBarrier)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbBarrier;
        private System.Windows.Forms.Label lblState;
    }
}

