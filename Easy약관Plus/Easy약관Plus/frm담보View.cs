using AxHWPCONTROLLib;
using System;
using System.IO;
using System.Windows.Forms;

namespace Easy약관Plus
{
    public partial class frm담보View : DevExpress.XtraEditors.XtraForm
    {
        string 담보;
        byte[] pdfByte;
        string 형식;
        public frm담보View(string 담보, byte[] pdfByte, string 형식)
        {
            this.담보 = 담보;
            this.pdfByte = pdfByte;
            this.형식 = 형식;
             InitializeComponent();
        }

        private void frm담보View_Load(object sender, EventArgs e)
        {

            // pdf리본 생성방법 (홈페이지참조)
            // Create Ribbon -> Create All Bars

            if (형식 == "HWP") {
                담보pdfViewer.Visible = false;
                담보pdfViewer.Dock = DockStyle.None;
                axHwp담보.Visible = true;
                axHwp담보.Dock = DockStyle.Fill;

                axHwp담보.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                axHwp담보.Clear(1);
                한글화면초기화(axHwp담보, "개별");
                axHwp담보.SetTextFile(담보, "HWP", "");  //"insertfile"

            }  else if ( 형식 == "PDF")  {

                담보pdfViewer.Visible = true;
                담보pdfViewer.Dock = DockStyle.Fill;
                axHwp담보.Visible = false;
                axHwp담보.Dock = DockStyle.None;

                System.IO.MemoryStream ms01 = new MemoryStream();
                ms01.Write(pdfByte, 0, pdfByte.Length);
                ms01.Seek(0, SeekOrigin.Begin);
                담보pdfViewer.LoadDocument(ms01);
            }



            //Dim vp As ParameterSet
            //Set vp = HwpCtrl.CreateSet("ViewProperties")
            //vp.SetItem "ZoomType", hwpZoomCustom
            //vp.SetItem "ZoomRatio", 100
            //HwpCtrl.ViewProperties = vp


            //DHwpParameterSet vp;
            //vp = HwpCtrl.CreateSet("ViewProperties");
            //vp.SetItem("ZoomType", 0); //HwpZoomCustom
            //vp.SetItem("ZoomRatio", 100);
            //axHwp담보.ViewProperties = vp;



        }

        private void 한글화면초기화(AxHwpCtrl Ax01, string 종류)
        {
            HWPCONTROLLib.HwpAction hwpact;
            HWPCONTROLLib.HwpParameterSet 화면;
            HWPCONTROLLib.HwpParameterSet 메뉴;

            //화면크기 조절
            hwpact = (HWPCONTROLLib.HwpAction)Ax01.CreateAction("ViewZoomFitPage");  // 화면크기 조절 
            화면 = (HWPCONTROLLib.HwpParameterSet)hwpact.CreateSet();
            화면.SetItem("ZoomType", "AutoFitPage"); // 쪽맞춤
            hwpact.Execute(화면);

            //Run 명령어는 아래와 같이 실행 한다.
            //Ax01.Run("ViewOptionParaMark"); //문단부호

            //hwpact = (HWPCONTROLLib.HwpAction)Ax01.CreateAction("ViewOptionParaMark");
            //화면 = (HWPCONTROLLib.HwpParameterSet)hwpact.CreateSet();
            //hwpact.Execute(화면);


            if (종류 == "개별")
            {

                //화면 마진
                hwpact = (HWPCONTROLLib.HwpAction)Ax01.CreateAction("PageSetup");
                화면 = (HWPCONTROLLib.HwpParameterSet)hwpact.CreateSet();
                메뉴 = (HWPCONTROLLib.HwpParameterSet)화면.CreateItemSet("PageDef", "PageDef");

                메뉴.SetItem("ApplyTo", 3);   // 적용범위 : 문서전체
                메뉴.SetItem("LeftMargin", MiliToHwpUnit(13));
                메뉴.SetItem("RightMargin", MiliToHwpUnit(13));
                메뉴.SetItem("TopMargin", MiliToHwpUnit(6.5));
                메뉴.SetItem("BottomMargin", MiliToHwpUnit(6.5));
                메뉴.SetItem("HeaderLen", MiliToHwpUnit(6.5));
                메뉴.SetItem("FooterLen", MiliToHwpUnit(7.5));

                hwpact.Execute(화면);
                //다단설정
                hwpact = (HWPCONTROLLib.HwpAction)Ax01.CreateAction("MultiColumn");
                화면 = (HWPCONTROLLib.HwpParameterSet)hwpact.CreateSet();
                화면.SetItem("Count", 2);
                화면.SetItem("SameGap", MiliToHwpUnit(8));

                화면.SetItem("ApplyClass", 832);
                화면.SetItem("ApplyTo", 6);
                hwpact.Execute(화면);
            }


        }
        private double MiliToHwpUnit(double mm)
        {
            double 계산 = mm * 283.465;
            return 계산;
        }

    }
}