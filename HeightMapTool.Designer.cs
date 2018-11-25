namespace HeightMapTool {
    partial class HeightMapTool {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer Code
        private void InitializeComponent() {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menu_LoadHeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_SaveHeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Reset = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();

            this.Text = "HeightMap Tool";
            this.components = new System.ComponentModel.Container();
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnRender);
            this.Controls.Add(this.menuStrip);
            this.menuStrip.ResumeLayout(false);
            this.ResumeLayout(false);



            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.menu_LoadHeightMap, this.menu_SaveHeightMap, this.menu_Reset });

            this.menu_LoadHeightMap.Text = "Load HeightMap";
            this.menu_LoadHeightMap.Click += new System.EventHandler(this.MenuItem_LoadHeightMap_Click);

            this.menu_SaveHeightMap.Text = "Save HeightMap";
            this.menu_SaveHeightMap.Click += new System.EventHandler(this.MenuItem_SaveHeightMap_Click);

            this.menu_Reset.Text = "Reset";
            this.menu_Reset.Click += new System.EventHandler(this.MenuItem_Reset_Click);
        }
        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menu_LoadHeightMap;
        private System.Windows.Forms.ToolStripMenuItem menu_SaveHeightMap;
        private System.Windows.Forms.ToolStripMenuItem menu_Reset;
    }
}