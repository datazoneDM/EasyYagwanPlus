namespace Easy약관Plus
{
    partial class frm담보관리
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
            this.grd담보리스트 = new DevExpress.XtraGrid.GridControl();
            this.view담보리스트 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.전체_담보약관 = new DevExpress.XtraTab.XtraTabControl();
            this.담보약관 = new DevExpress.XtraTab.XtraTabPage();
            this.전체약관 = new DevExpress.XtraTab.XtraTabPage();
            this.grd약관정보목록 = new DevExpress.XtraGrid.GridControl();
            this.view약관정보목록 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.groupControl2 = new DevExpress.XtraEditors.GroupControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.txt전체담보갯수 = new DevExpress.XtraEditors.TextEdit();
            this.txt현재변환담보갯수 = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.barHwp변환진행 = new DevExpress.XtraEditors.ProgressBarControl();
            this.grd담보 = new DevExpress.XtraGrid.GridControl();
            this.view담보 = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.grd담보리스트)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.view담보리스트)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.전체_담보약관)).BeginInit();
            this.전체_담보약관.SuspendLayout();
            this.담보약관.SuspendLayout();
            this.전체약관.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grd약관정보목록)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.view약관정보목록)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).BeginInit();
            this.groupControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt전체담보갯수.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt현재변환담보갯수.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.barHwp변환진행.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grd담보)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.view담보)).BeginInit();
            this.SuspendLayout();
            // 
            // grd담보리스트
            // 
            this.grd담보리스트.AllowDrop = true;
            this.grd담보리스트.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grd담보리스트.Location = new System.Drawing.Point(0, 0);
            this.grd담보리스트.LookAndFeel.SkinName = "McSkin";
            this.grd담보리스트.LookAndFeel.UseDefaultLookAndFeel = false;
            this.grd담보리스트.MainView = this.view담보리스트;
            this.grd담보리스트.Name = "grd담보리스트";
            this.grd담보리스트.Size = new System.Drawing.Size(1563, 927);
            this.grd담보리스트.TabIndex = 0;
            this.grd담보리스트.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.view담보리스트});
            this.grd담보리스트.DragDrop += new System.Windows.Forms.DragEventHandler(this.grd담보리스트_DragDrop);
            this.grd담보리스트.DragOver += new System.Windows.Forms.DragEventHandler(this.grd담보리스트_DragOver);
            this.grd담보리스트.DoubleClick += new System.EventHandler(this.grd담보리스트_DoubleClick);
            // 
            // view담보리스트
            // 
            this.view담보리스트.DetailHeight = 425;
            this.view담보리스트.GridControl = this.grd담보리스트;
            this.view담보리스트.Name = "view담보리스트";
            this.view담보리스트.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.view담보리스트_RowStyle);
            // 
            // groupControl1
            // 
            this.groupControl1.Appearance.Font = new System.Drawing.Font("나눔바른고딕", 8.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.groupControl1.Appearance.Options.UseFont = true;
            this.groupControl1.AppearanceCaption.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupControl1.AppearanceCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.groupControl1.AppearanceCaption.Options.UseFont = true;
            this.groupControl1.AppearanceCaption.Options.UseForeColor = true;
            this.groupControl1.Controls.Add(this.전체_담보약관);
            this.groupControl1.Controls.Add(this.groupControl2);
            this.groupControl1.Controls.Add(this.grd담보);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(1868, 1001);
            this.groupControl1.TabIndex = 1;
            this.groupControl1.Text = "담보파일과 담보코드리스트를 비교합니다.";
            // 
            // 전체_담보약관
            // 
            this.전체_담보약관.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.전체_담보약관.Appearance.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.전체_담보약관.Appearance.Options.UseFont = true;
            this.전체_담보약관.AppearancePage.Header.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.전체_담보약관.AppearancePage.Header.Options.UseFont = true;
            this.전체_담보약관.Location = new System.Drawing.Point(292, 29);
            this.전체_담보약관.Name = "전체_담보약관";
            this.전체_담보약관.SelectedTabPage = this.담보약관;
            this.전체_담보약관.Size = new System.Drawing.Size(1571, 966);
            this.전체_담보약관.TabIndex = 5;
            this.전체_담보약관.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.담보약관,
            this.전체약관});
            // 
            // 담보약관
            // 
            this.담보약관.Controls.Add(this.grd담보리스트);
            this.담보약관.Name = "담보약관";
            this.담보약관.Size = new System.Drawing.Size(1563, 927);
            this.담보약관.Text = "담보분리약관";
            // 
            // 전체약관
            // 
            this.전체약관.Controls.Add(this.grd약관정보목록);
            this.전체약관.Name = "전체약관";
            this.전체약관.Size = new System.Drawing.Size(1563, 927);
            this.전체약관.Text = "전체약관(통)";
            // 
            // grd약관정보목록
            // 
            this.grd약관정보목록.AllowDrop = true;
            this.grd약관정보목록.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grd약관정보목록.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grd약관정보목록.Location = new System.Drawing.Point(0, 0);
            this.grd약관정보목록.MainView = this.view약관정보목록;
            this.grd약관정보목록.Name = "grd약관정보목록";
            this.grd약관정보목록.Size = new System.Drawing.Size(1563, 927);
            this.grd약관정보목록.TabIndex = 272;
            this.grd약관정보목록.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.view약관정보목록});
            this.grd약관정보목록.DragDrop += new System.Windows.Forms.DragEventHandler(this.grd약관정보목록_DragDrop);
            this.grd약관정보목록.DragOver += new System.Windows.Forms.DragEventHandler(this.grd약관정보목록_DragOver);
            // 
            // view약관정보목록
            // 
            this.view약관정보목록.Appearance.Row.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.view약관정보목록.Appearance.Row.Options.UseFont = true;
            this.view약관정보목록.GridControl = this.grd약관정보목록;
            this.view약관정보목록.Name = "view약관정보목록";
            this.view약관정보목록.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.view약관정보목록_RowCellStyle);
            // 
            // groupControl2
            // 
            this.groupControl2.AppearanceCaption.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupControl2.AppearanceCaption.Options.UseFont = true;
            this.groupControl2.Controls.Add(this.labelControl2);
            this.groupControl2.Controls.Add(this.txt전체담보갯수);
            this.groupControl2.Controls.Add(this.txt현재변환담보갯수);
            this.groupControl2.Controls.Add(this.labelControl1);
            this.groupControl2.Controls.Add(this.barHwp변환진행);
            this.groupControl2.Location = new System.Drawing.Point(5, 29);
            this.groupControl2.Name = "groupControl2";
            this.groupControl2.Size = new System.Drawing.Size(285, 91);
            this.groupControl2.TabIndex = 4;
            this.groupControl2.Text = "Hwp변환진행";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("나눔바른고딕", 8.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(152, 39);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(5, 14);
            this.labelControl2.TabIndex = 4;
            this.labelControl2.Text = "/";
            // 
            // txt전체담보갯수
            // 
            this.txt전체담보갯수.EditValue = "0";
            this.txt전체담보갯수.Location = new System.Drawing.Point(160, 33);
            this.txt전체담보갯수.Name = "txt전체담보갯수";
            this.txt전체담보갯수.Properties.Appearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.txt전체담보갯수.Properties.Appearance.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt전체담보갯수.Properties.Appearance.Options.UseBorderColor = true;
            this.txt전체담보갯수.Properties.Appearance.Options.UseFont = true;
            this.txt전체담보갯수.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.txt전체담보갯수.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.txt전체담보갯수.Size = new System.Drawing.Size(55, 24);
            this.txt전체담보갯수.TabIndex = 3;
            // 
            // txt현재변환담보갯수
            // 
            this.txt현재변환담보갯수.EditValue = "0";
            this.txt현재변환담보갯수.Location = new System.Drawing.Point(93, 33);
            this.txt현재변환담보갯수.Name = "txt현재변환담보갯수";
            this.txt현재변환담보갯수.Properties.Appearance.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt현재변환담보갯수.Properties.Appearance.Options.UseFont = true;
            this.txt현재변환담보갯수.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.txt현재변환담보갯수.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.txt현재변환담보갯수.Size = new System.Drawing.Size(55, 24);
            this.txt현재변환담보갯수.TabIndex = 2;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(5, 36);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(84, 18);
            this.labelControl1.TabIndex = 2;
            this.labelControl1.Text = "변환상황카운트";
            // 
            // barHwp변환진행
            // 
            this.barHwp변환진행.EditValue = null;
            this.barHwp변환진행.Location = new System.Drawing.Point(5, 67);
            this.barHwp변환진행.Name = "barHwp변환진행";
            this.barHwp변환진행.Properties.Appearance.Font = new System.Drawing.Font("카이겐고딕 KR Regular", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barHwp변환진행.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.barHwp변환진행.Size = new System.Drawing.Size(210, 18);
            this.barHwp변환진행.TabIndex = 2;
            this.barHwp변환진행.TabStop = true;
            // 
            // grd담보
            // 
            this.grd담보.AllowDrop = true;
            this.grd담보.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.grd담보.Location = new System.Drawing.Point(5, 125);
            this.grd담보.LookAndFeel.SkinName = "Foggy";
            this.grd담보.LookAndFeel.UseDefaultLookAndFeel = false;
            this.grd담보.MainView = this.view담보;
            this.grd담보.Name = "grd담보";
            this.grd담보.Size = new System.Drawing.Size(285, 870);
            this.grd담보.TabIndex = 3;
            this.grd담보.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.view담보});
            this.grd담보.DragDrop += new System.Windows.Forms.DragEventHandler(this.grd담보_DragDrop);
            this.grd담보.DragOver += new System.Windows.Forms.DragEventHandler(this.grd담보_DragOver);
            // 
            // view담보
            // 
            this.view담보.DetailHeight = 425;
            this.view담보.GridControl = this.grd담보;
            this.view담보.Name = "view담보";
            this.view담보.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.view담보_RowStyle);
            // 
            // frm담보관리
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1868, 1001);
            this.Controls.Add(this.groupControl1);
            this.LookAndFeel.SkinName = "McSkin";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Name = "frm담보관리";
            this.Text = "담보관리";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frm담보관리_FormClosed);
            this.Load += new System.EventHandler(this.frm담보관리_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grd담보리스트)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.view담보리스트)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.전체_담보약관)).EndInit();
            this.전체_담보약관.ResumeLayout(false);
            this.담보약관.ResumeLayout(false);
            this.전체약관.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grd약관정보목록)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.view약관정보목록)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).EndInit();
            this.groupControl2.ResumeLayout(false);
            this.groupControl2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt전체담보갯수.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt현재변환담보갯수.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.barHwp변환진행.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grd담보)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.view담보)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl grd담보리스트;
        private DevExpress.XtraGrid.Views.Grid.GridView view담보리스트;
        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraGrid.GridControl grd담보;
        private DevExpress.XtraGrid.Views.Grid.GridView view담보;
        private DevExpress.XtraEditors.ProgressBarControl barHwp변환진행;
        private DevExpress.XtraEditors.TextEdit txt현재변환담보갯수;
        private DevExpress.XtraEditors.GroupControl groupControl2;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.TextEdit txt전체담보갯수;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraTab.XtraTabControl 전체_담보약관;
        private DevExpress.XtraTab.XtraTabPage 담보약관;
        private DevExpress.XtraTab.XtraTabPage 전체약관;
        private DevExpress.XtraGrid.GridControl grd약관정보목록;
        private DevExpress.XtraGrid.Views.Grid.GridView view약관정보목록;
    }
}