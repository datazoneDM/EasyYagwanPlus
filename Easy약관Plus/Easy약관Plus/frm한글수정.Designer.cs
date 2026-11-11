namespace Easy약관Plus
{
    partial class frm한글수정
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm한글수정));
            this.group한글정보 = new DevExpress.XtraEditors.GroupControl();
            this.grd초안한글정보 = new DevExpress.XtraGrid.GridControl();
            this.view초안한글정보 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.grd개정한글정보 = new DevExpress.XtraGrid.GridControl();
            this.view개정한글정보 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.group편집본 = new DevExpress.XtraEditors.GroupControl();
            this.AX개정 = new AxHWPCONTROLLib.AxHwpCtrl();
            this.AX편집 = new AxHWPCONTROLLib.AxHwpCtrl();
            ((System.ComponentModel.ISupportInitialize)(this.group한글정보)).BeginInit();
            this.group한글정보.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grd초안한글정보)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.view초안한글정보)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grd개정한글정보)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.view개정한글정보)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.group편집본)).BeginInit();
            this.group편집본.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AX개정)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AX편집)).BeginInit();
            this.SuspendLayout();
            // 
            // group한글정보
            // 
            this.group한글정보.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.group한글정보.AppearanceCaption.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.group한글정보.AppearanceCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.group한글정보.AppearanceCaption.Options.UseFont = true;
            this.group한글정보.AppearanceCaption.Options.UseForeColor = true;
            this.group한글정보.Controls.Add(this.grd초안한글정보);
            this.group한글정보.Controls.Add(this.grd개정한글정보);
            this.group한글정보.Location = new System.Drawing.Point(3, 2);
            this.group한글정보.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.group한글정보.Name = "group한글정보";
            this.group한글정보.Size = new System.Drawing.Size(1120, 810);
            this.group한글정보.TabIndex = 0;
            this.group한글정보.Text = "비교할 한글파일 정보";
            // 
            // grd초안한글정보
            // 
            this.grd초안한글정보.AllowDrop = true;
            this.grd초안한글정보.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grd초안한글정보.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.grd초안한글정보.Location = new System.Drawing.Point(560, 29);
            this.grd초안한글정보.MainView = this.view초안한글정보;
            this.grd초안한글정보.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.grd초안한글정보.Name = "grd초안한글정보";
            this.grd초안한글정보.Size = new System.Drawing.Size(550, 774);
            this.grd초안한글정보.TabIndex = 1;
            this.grd초안한글정보.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.view초안한글정보});
            this.grd초안한글정보.DragDrop += new System.Windows.Forms.DragEventHandler(this.grd초안한글정보_DragDrop);
            this.grd초안한글정보.DragOver += new System.Windows.Forms.DragEventHandler(this.grd초안한글정보_DragOver);
            // 
            // view초안한글정보
            // 
            this.view초안한글정보.GridControl = this.grd초안한글정보;
            this.view초안한글정보.Name = "view초안한글정보";
            this.view초안한글정보.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.view초안한글정보_RowStyle);
            // 
            // grd개정한글정보
            // 
            this.grd개정한글정보.AllowDrop = true;
            this.grd개정한글정보.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.grd개정한글정보.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.grd개정한글정보.Location = new System.Drawing.Point(5, 29);
            this.grd개정한글정보.MainView = this.view개정한글정보;
            this.grd개정한글정보.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.grd개정한글정보.Name = "grd개정한글정보";
            this.grd개정한글정보.Size = new System.Drawing.Size(550, 774);
            this.grd개정한글정보.TabIndex = 0;
            this.grd개정한글정보.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.view개정한글정보});
            this.grd개정한글정보.DragDrop += new System.Windows.Forms.DragEventHandler(this.grd개정한글정보_DragDrop);
            this.grd개정한글정보.DragOver += new System.Windows.Forms.DragEventHandler(this.grd개정한글정보_DragOver);
            // 
            // view개정한글정보
            // 
            this.view개정한글정보.GridControl = this.grd개정한글정보;
            this.view개정한글정보.Name = "view개정한글정보";
            this.view개정한글정보.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.view개정한글정보_RowStyle);
            this.view개정한글정보.Click += new System.EventHandler(this.view개정한글정보_Click);
            // 
            // group편집본
            // 
            this.group편집본.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.group편집본.AppearanceCaption.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.group편집본.AppearanceCaption.Options.UseFont = true;
            this.group편집본.Controls.Add(this.AX개정);
            this.group편집본.Controls.Add(this.AX편집);
            this.group편집본.Location = new System.Drawing.Point(1126, 2);
            this.group편집본.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.group편집본.Name = "group편집본";
            this.group편집본.Size = new System.Drawing.Size(696, 803);
            this.group편집본.TabIndex = 1;
            this.group편집본.Text = "(개정한글, 편집한글) 한글파일 올려 놓으세요";
            // 
            // AX개정
            // 
            this.AX개정.AllowDrop = true;
            this.AX개정.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.AX개정.Enabled = true;
            this.AX개정.Location = new System.Drawing.Point(5, 28);
            this.AX개정.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.AX개정.Name = "AX개정";
            this.AX개정.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("AX개정.OcxState")));
            this.AX개정.Size = new System.Drawing.Size(341, 769);
            this.AX개정.TabIndex = 1;
            // 
            // AX편집
            // 
            this.AX편집.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AX편집.Enabled = true;
            this.AX편집.Location = new System.Drawing.Point(348, 29);
            this.AX편집.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.AX편집.Name = "AX편집";
            this.AX편집.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("AX편집.OcxState")));
            this.AX편집.Size = new System.Drawing.Size(343, 768);
            this.AX편집.TabIndex = 0;
            // 
            // frm한글수정
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1826, 811);
            this.Controls.Add(this.group편집본);
            this.Controls.Add(this.group한글정보);
            this.LookAndFeel.SkinName = "McSkin";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "frm한글수정";
            this.Text = "frm한글수정";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frm한글수정_FormClosed);
            this.Load += new System.EventHandler(this.frm한글수정_Load);
            this.Resize += new System.EventHandler(this.frm한글수정_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.group한글정보)).EndInit();
            this.group한글정보.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grd초안한글정보)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.view초안한글정보)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grd개정한글정보)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.view개정한글정보)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.group편집본)).EndInit();
            this.group편집본.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.AX개정)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AX편집)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl group한글정보;
        private DevExpress.XtraGrid.GridControl grd개정한글정보;
        private DevExpress.XtraGrid.Views.Grid.GridView view개정한글정보;
        private DevExpress.XtraGrid.GridControl grd초안한글정보;
        private DevExpress.XtraGrid.Views.Grid.GridView view초안한글정보;
        private DevExpress.XtraEditors.GroupControl group편집본;
        private AxHWPCONTROLLib.AxHwpCtrl AX편집;
        private AxHWPCONTROLLib.AxHwpCtrl AX개정;
    }
}