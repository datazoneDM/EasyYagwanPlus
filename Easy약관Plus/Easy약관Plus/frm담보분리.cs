using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraGrid.Views.Grid;
using AxHWPCONTROLLib;
using System.Collections.Generic;
using System.Linq;
using HWPCONTROLLib;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Easy약관Plus
{
    public partial class frm담보분리 : DevExpress.XtraEditors.XtraForm
    {
        public string[] 담보분리메인화면event = new string[10];
        public static event 담보분리메인화면_Handler 담보분리메인화면_SetMessage;
        public delegate void 담보분리메인화면_Handler(string[] 메인화면event);

        private string 담보인덱스파일 = "";
        private string[] 스타일내용;
        private Dictionary<string, string> 스타일값 = new Dictionary<string, string>();
        public frm담보분리()
        {
            InitializeComponent();
        }

        private string 한글파일경로;
        private string 비교할HWP경로;
        private string 기본양식폼;

        // 한글문서 기본정보 가져오기
        /*
        편집용지 편집용지;
        문단모양 문단모양;
        글자모양 글자모양;
        다단설정 다단설정;
        구역설정 구역설정;

        private void Hwp문서정보가져오기(AxHwpCtrl 원본axHwpCtrl)
        {
            // 기본값을 가져오기 위해 스타일중 바탕글(0)을 적용한다.
            원본axHwpCtrl.MovePos(3, 0, 0);
            HwpAction hwpact = (HwpAction)원본axHwpCtrl.CreateAction("Style"); //스타일 
            HwpParameterSet 화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            화면.SetItem("Apply",0);  // 문서의 맨뒤로 이동 후 스타일중 맨처음 있는 '바탕글' 적용 후 아래 기본값을 추출한다.
            hwpact.Execute(화면);

            편집용지 = new 편집용지();
            hwpact = (HwpAction)원본axHwpCtrl.CreateAction("PageSetup"); //편집용지
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            편집용지.위쪽 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("TopMargin"));
            편집용지.머리말 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("HeaderLen"));
            편집용지.왼쪽 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("LeftMargin"));
            편집용지.오른쪽 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("RightMargin"));
            편집용지.꼬리말 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("FooterLen"));
            편집용지.아래쪽 = Convert.ToDouble(((HwpParameterSet)화면.Item("PageDef")).Item("BottomMargin"));

            문단모양 = new 문단모양();
            hwpact = (HwpAction)원본axHwpCtrl.CreateAction("ParagraphShape"); //문단모양
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            문단모양.줄간격 = Convert.ToDouble(화면.Item("LineSpacing"));

            글자모양 = new 글자모양();
            hwpact = (HwpAction)원본axHwpCtrl.CreateAction("CharShape"); //글자모양
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            글자모양.기준크기 = Convert.ToDouble(화면.Item("Height"));
            글자모양.글꼴 = 화면.Item("FaceNameHangul").ToString();
            글자모양.장평 = Convert.ToInt32(화면.Item("RatioHangul"));

            다단설정 = new 다단설정();
            hwpact = (HwpAction)원본axHwpCtrl.CreateAction("MultiColumn"); //다단설정
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            다단설정.단개수 = Convert.ToInt32(화면.Item("Count"));
            다단설정.간격 = Convert.ToDouble(화면.Item("SameGap"));
            다단설정.적용범위1 = Convert.ToInt32(화면.Item("ApplyClass"));
            다단설정.적용범위2 = Convert.ToInt32(화면.Item("ApplyTo"));

            구역설정 = new 구역설정();
            hwpact = (HwpAction)원본axHwpCtrl.CreateAction("ModifySection"); //구역설정
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            구역설정.빈줄감추기 = Convert.ToInt32(화면.Item("HideEmptyLine"));  // 1 : 설정 , 0 : 해제

        }
        private void Hwp문서정보적용하기(AxHwpCtrl 대상axHwpCtrl)
        {
            HwpAction hwpact = (HwpAction)대상axHwpCtrl.CreateAction("PageSetup"); //편집용지 적용 
            HwpParameterSet 화면 = (HwpParameterSet)hwpact.CreateSet();
            HwpParameterSet 메뉴 = (HwpParameterSet)화면.CreateItemSet("PageDef", "PageDef");
            메뉴.SetItem("ApplyTo", 3);   // 적용범위 : 문서전체
            메뉴.SetItem("LeftMargin", 편집용지.왼쪽);
            메뉴.SetItem("RightMargin", 편집용지.오른쪽);
            메뉴.SetItem("TopMargin", 편집용지.위쪽);
            메뉴.SetItem("HeaderLen", 편집용지.머리말);
            메뉴.SetItem("BottomMargin", 편집용지.아래쪽);
            메뉴.SetItem("FooterLen", 편집용지.꼬리말);
            hwpact.Execute(화면);

            hwpact = (HwpAction)대상axHwpCtrl.CreateAction("ParagraphShape"); //문단모양 적용 
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            화면.SetItem("LineSpacing", 문단모양.줄간격);
            hwpact.Execute(화면);

            hwpact = (HwpAction)대상axHwpCtrl.CreateAction("CharShape"); //글자모양 적용 
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            화면.SetItem("Height", 글자모양.기준크기);

            화면.SetItem("FaceNameHangul", 글자모양.글꼴);
            화면.SetItem("FaceNameLatin", 글자모양.글꼴);
            화면.SetItem("FaceNameHanja", 글자모양.글꼴);
            화면.SetItem("FaceNameJapanese", 글자모양.글꼴);
            화면.SetItem("FaceNameOther", 글자모양.글꼴);
            화면.SetItem("FaceNameSymbol", 글자모양.글꼴);
            화면.SetItem("FaceNameUser", 글자모양.글꼴);

            화면.SetItem("RatioUser", 글자모양.장평);
            화면.SetItem("RatioSymbol", 글자모양.장평);
            화면.SetItem("RatioOther", 글자모양.장평);
            화면.SetItem("RatioJapanese", 글자모양.장평);
            화면.SetItem("RatioHanja", 글자모양.장평);
            화면.SetItem("RatioLatin", 글자모양.장평);
            화면.SetItem("RatioHangul", 글자모양.장평);
            hwpact.Execute(화면);
                       
            hwpact = (HwpAction)대상axHwpCtrl.CreateAction("MultiColumn");  //다단설정
            화면 = (HwpParameterSet)hwpact.CreateSet();
            hwpact.GetDefault(화면);
            화면.SetItem("Count", 다단설정.단개수);
            화면.SetItem("SameGap", 다단설정.간격);
            화면.SetItem("ApplyClass", 832);    //다단설정.적용범위1
            화면.SetItem("ApplyTo", 6); //다단설정.적용범위2
            hwpact.Execute(화면);

            hwpact = (HwpAction)대상axHwpCtrl.CreateAction("ModifySection");  //구역설정 - 맨아래 줄이 빈줄이면 꼬리말 구역까지 내려간다.
            화면 = (HwpParameterSet)hwpact.CreateSet();
            화면.SetItem("HideEmptyLine", 구역설정.빈줄감추기);
            화면.SetItem("ApplyClass", 24);   
            화면.SetItem("ApplyTo", 3); 
            hwpact.GetDefault(화면);
        }

        */
        private async void frm담보분리_Load(object sender, EventArgs e)
        {
            frmEasy약관.담보분리_SetMessage += new frmEasy약관.담보분리_Handler(event_담보분리_setMessage);

            // 1. UI 껍데기(그리드) 설정 - BeginUpdate가 적용되어 있어 0.01초 만에 끝납니다.
            grd담보분리_화면초기화();
            grd담보비교_화면초기화();
            grd담보분석_화면초기화();

            // 2. [핵심] 파일 읽기를 백그라운드 스레드로 던져서 폼이 뜨는 것을 막지 않게 합니다.
            await 스타일읽기Async();

            기다림.Visible = false;
        }

        private void Hwp화면보기설정(AxHwpCtrl 대상axHwpCtrl)
        {

            //화면크기 조절
            HwpAction hwpact = (HwpAction)대상axHwpCtrl.CreateAction("ViewZoomFitPage");  // 화면크기 조절 
            HwpParameterSet 화면 = (HwpParameterSet)hwpact.CreateSet();
            화면.SetItem("ZoomType", 1); // 쪽맞춤  "AutoFitPage"
            hwpact.Execute(화면);


            ////용지크기 조절
            //hwpact = (HwpAction)대상axHwpCtrl.CreateAction("PageSetup");
            //화면 = (HwpParameterSet)hwpact.CreateSet();

            //// 2. ★중요: 현재 페이지의 기본 설정을 먼저 가져옵니다 (기존 여백/크기 유지)
            //hwpact.GetDefault(화면);

            //// 3. PageDef 하위 객체를 가져와서 가로(Landscape) 방향으로 설정합니다.
            //HwpParameterSet pageDef = (HwpParameterSet)화면.Item("PageDef");
            //pageDef.SetItem("Landscape", 1);

            //// 4. 적용 범위 설정
            //화면.SetItem("ApplyClass", 24);
            //화면.SetItem("ApplyTo", 3);

            //// 5. 액션 실행
            //hwpact.Execute(화면);

            
        }
        private void frm담보분리_Shown(object sender, EventArgs e)
        {
            // 3. [핵심] 폼이 화면에 나타나자마자 한글 컨트롤 제어를 메세지 큐에 가볍게 던집니다.
            // 이렇게 하면 사용자는 폼이 즉시 열린 것처럼 느끼게 됩니다.
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    Hwp화면보기설정(ax전체미리보기);
                    Hwp화면보기설정(ax개별미리보기);
                }
                catch (Exception)
                {
                    // 한글 엔진 초기화 중 발생할 수 있는 COM 예외 안전하게 무시
                }
            }));
        }
        private async Task 스타일읽기Async()
        {
            try
            {
                스타일값 = new Dictionary<string, string>();
                string 파일경로 = string.Format("{0}\\스타일.txt", Application.StartupPath);

                if (!System.IO.File.Exists(파일경로))
                {
                    MessageBox.Show("스타일 파일이 존재하지 않습니다.", "파일 누락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // StreamReader와 ReadToEndAsync를 사용하여 UI 멈춤 없이 텍스트를 메모리에 싹 올려버립니다.
                using (var reader = new System.IO.StreamReader(파일경로, System.Text.Encoding.GetEncoding(949)))
                {
                    string 파일전체내용 = await reader.ReadToEndAsync();
                    스타일내용 = 파일전체내용.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    foreach (string 라인 in 스타일내용)
                    {
                        if (string.IsNullOrWhiteSpace(라인) || 라인.Contains("**"))
                            continue;

                        string[] 분리 = 라인.Split('|');
                        if (분리.Length < 2) continue; // 방어 코드

                        string[] 담보키분리 = 분리[0].Split(';');

                        foreach (string 키 in 담보키분리)
                        {
                            // Dictionary 중복 키 에러 방지 (ContainsKey)
                            if (!스타일값.ContainsKey(키))
                                스타일값.Add(키, 분리[1]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("스타일 파일에 오류가 발생 하였습니다.\n 경로 : c:\\Easy약관Plus\\program\\스타일.txt", "작업진행 불가(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Hwp문서정보가져오기
        private async void event_담보분리_setMessage(string[] 전달인수)
        {
            switch (전달인수[0])
            {
                case "Hwp분석":
                    await 스타일읽기Async();
                    기다림.Visible = true;

                    원본담보위치정보();
                    기다림.Visible = false;
                    break;
                case "인덱스생성":
                    한글파일경로 = 전달인수[1];
                    한글인덱스생성();
                    세션페이지생성();
                    break;
                case "인덱스저장":
                    한글인덱스저장();
                    break;
                case "한글파일경로전달":
                    한글파일경로 = 전달인수[1];
                    
                    ax전체미리보기열기(한글파일경로);

                    // 원본한글파일을 읽어 페이지,단,줄 setting
                    if (전달인수[1] == "유효성설정") {       
                        ax개별미리보기열기(한글파일경로);
                        한글원본기본설정();
                    }

                    break;
                case "담보쪼개기":
                    한글파일경로 = 전달인수[1];
                    담보쪼개기(-1,"전체저장");  // 처음부터 천체 분리를 하거나 특정 1개만 분리 할 수 있다.
                    break;
                case "담보합치기":
                    담보합치기();
                    break;
                case "비교할HWP경로":
                    비교할HWP경로 = 전달인수[1];
                    비교할HWP열기(비교할HWP경로);
                    break;
                case "한글유효성설정":
                    tab담보분리.SelectedTabPage = tab담보비교;
                    한글원본기본설정();
                    break;
                case "한글유효성검사":
                    한글유효성검사();
                    break;
                case "문단부호표시":
                    문단부호표시();
                    break;
            }
        }
        
        private void 세션페이지생성()
        {

            //1) 개별Hwp_원본 open
            ax개별미리보기.CreateControl();
            ax개별미리보기.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax개별미리보기.Clear(1);
            ax개별미리보기.Open(ax전체미리보기.Path);

            // 바탕글을 찾는다.?
            // 스타일이 적용된 항목을 복사하면 계속 이어진 특히 박스형태의 스타일
            ax전체미리보기.MovePos(2, 0, 0);
            HwpAction hwpact;
            HwpParameterSet textFind;
            hwpact = (HwpAction)ax전체미리보기.CreateAction("RepeatFind");  //FindAll 
            textFind = (HwpParameterSet)hwpact.CreateSet();
            textFind.SetItem("Direction", 2);              // 찾을 방향 : 0 = 아래쪽, 1 = 위쪽, 2 = 문서 전체
            textFind.SetItem("FindStyle", "바탕글");      // *** ※ 스타일 적용해서 제목을 찾음 - 중간에 제목이 2번이상 나올때가 존재함 ***
            textFind.SetItem("IgnoreMessage", 1);          // 메시지 무시 여부
            textFind.SetItem("FindType", 1);
            hwpact.Execute(textFind);

            int CtrlID = -1, 문단 = -1, 컬럼 = -1;
            ax전체미리보기.GetPos(ref CtrlID, ref 문단, ref 컬럼);
            ax전체미리보기.SelectText(문단, 0, 문단, 1);

            HwpAction 전체hwpact = (HwpAction)ax전체미리보기.CreateAction("SaveBlockAction"); // 블럭저장 
            HwpParameterSet 블럭저장 = (HwpParameterSet)전체hwpact.CreateSet();
            전체hwpact.GetDefault(블럭저장);

            string 한글저장 = string.Format("{0}\\Style\\기본틀.hwp", Application.StartupPath);
            블럭저장.SetItem("FileName", 한글저장);
            블럭저장.SetItem("Format", "HWP");
            블럭저장.SetItem("Attributes", 1);
            전체hwpact.Execute(블럭저장);

            ax개별미리보기.Clear(1);
            ax개별미리보기.Open(한글저장);

            ax개별미리보기.Run("SelectAll");
            ax개별미리보기.Run("Delete");

            HwpAction 개별hwpact = (HwpAction)ax개별미리보기.CreateAction("StyleEx"); // 스타일 덮어쓰기 
            HwpParameterSet 스타일수정 = (HwpParameterSet)개별hwpact.CreateSet();
            개별hwpact.GetDefault(스타일수정);
            개별hwpact.Execute(스타일수정);
            ax개별미리보기.Save();

            GridView gdview = view담보분리;
            int 세션카운트 = 0;
            for (int i = 0; i < gdview.RowCount; i++) {
                string 담보코드 = gdview.GetRowCellDisplayText(i, "담보코드").ToUpper();

                bool 세션타이틀찾기 = false;
                switch (담보코드) {
                    case "타이틀_HB":
                    case "타이틀_BB":
                    case "타이틀_TB":
                    case "타이틀_SB":
                    case "타이틀_IB":
                    case "기본_H":
                    case "기본_B":
                    case "기본_T":
                    case "기본_S":
                    case "기본_I": {
                        세션타이틀찾기 = true;
                        세션카운트++;
                        break;
                    }
                }

                // =======================
                //    세션타이틀 미존재
                // =======================

                // 세션타이틀이 없는경우 
                // 강제로 1개를 생성한다.
                // 담보쪼개기(int 시작, string 구분)  참조

                if (i == gdview.RowCount - 1 && 세션카운트 == 0) {
                    세션타이틀찾기 = true;
                    담보코드 = "타이틀_XB";
                }

                if (세션타이틀찾기) {
                    문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(i, "문단"));

                    //2) 개별Hwp_원본 삭제 - 스타일, 바탕쪽만 남음
                    ax개별미리보기.Clear(1);
                    ax개별미리보기.Open(한글저장);

                    //ax개별미리보기.Run("SelectAll");
                    //ax개별미리보기.Run("Delete");

                    //3) 개별Hwp_바탕쪽 삭제
                    개별hwpact = (HwpAction)ax개별미리보기.CreateAction("MasterPage"); // 바탕쪽 
                    HwpParameterSet 개별바탕쪽 = (HwpParameterSet)개별hwpact.CreateSet();
                    개별hwpact.GetDefault(개별바탕쪽);
                    개별바탕쪽.SetItem("Duplicate", 0);
                    개별바탕쪽.SetItem("Front", 0);
                    개별바탕쪽.SetItem("Type", 2);
                    개별hwpact.Execute(개별바탕쪽);

                    System.Threading.Thread.Sleep(1000);
                    ax개별미리보기.Run("SelectAll");
                    ax개별미리보기.Run("Delete");
                    System.Threading.Thread.Sleep(500);
                    ax개별미리보기.Run("CloseEx");

                    //ax개별미리보기.Run("DeleteSectionMasterPage");  // 메세지 창이 띄워진다. 
                    //ax개별미리보기.Run("MasterPageDeleteEx");
                    // ↓↓↓↓↓↓↓↓↓↓↓↓↓ [ 아래와 같이 수정 가능하면 팝업상자의 전달 변수를 받을 수 있다 ] ↓↓↓↓↓↓↓↓↓↓↓↓ 
                    //개별hwpact = (HwpAction)ax개별미리보기.CreateAction("DeleteSectionMasterPage");
                    //개별바탕쪽 = (HwpParameterSet)개별hwpact.CreateSet();
                    //개별hwpact.GetDefault(개별바탕쪽);


                    //--------------------------------------------------
                    ax전체미리보기.MovePos(1, 문단, 0);
                    HwpParameterSet 전체이동 = (HwpParameterSet)ax전체미리보기.ScrollPosInfo;
                    전체이동.SetItem("VertPos", 100);

                    //4) 전체Hwp_바탕쪽 복사
                    전체hwpact = (HwpAction)ax전체미리보기.CreateAction("MasterPage"); // 바탕쪽 
                    HwpParameterSet 전체바탕쪽 = (HwpParameterSet)전체hwpact.CreateSet();
                    전체hwpact.GetDefault(전체바탕쪽);
                    전체바탕쪽.SetItem("Duplicate", 0);
                    전체바탕쪽.SetItem("Front", 0);
                    전체바탕쪽.SetItem("Type", 2);
                    전체hwpact.Execute(전체바탕쪽);

                    System.Threading.Thread.Sleep(1000);
                    ax전체미리보기.Run("SelectAll");
                    ax전체미리보기.Run("Copy");
                    System.Threading.Thread.Sleep(500);
                    ax전체미리보기.Run("CloseEx");
                    //--------------------------------------------------


                    //5) 개별Hwp_바탕쪽 붙여넣기

                    개별hwpact = (HwpAction)ax개별미리보기.CreateAction("MasterPage"); // 바탕쪽 
                    개별바탕쪽 = (HwpParameterSet)개별hwpact.CreateSet();
                    개별hwpact.GetDefault(개별바탕쪽);
                    개별바탕쪽.SetItem("Duplicate", 0);
                    개별바탕쪽.SetItem("Front", 0);
                    개별바탕쪽.SetItem("Type", 2);
                    개별hwpact.Execute(개별바탕쪽);

                    System.Threading.Thread.Sleep(1000);

                    //개별hwpact = (HwpAction)ax개별미리보기.CreateAction("Paste");
                    //개별바탕쪽 = (HwpParameterSet)개별hwpact.CreateSet();
                    //개별hwpact.GetDefault(개별바탕쪽);

                    ax개별미리보기.Run("Paste");
                    System.Threading.Thread.Sleep(500);
                    ax개별미리보기.Run("CloseEx");

                    //6) 개별Hwp_세션별 기본틀 SaveAs
                    string 저장경로 = string.Format("{0}\\Style\\세션_{1}.hwp", Application.StartupPath, 담보코드);
                    ax개별미리보기.SaveAs(저장경로);

                }

            }

            ax전체미리보기.MovePos(2, 0, 0);
            HwpParameterSet 전체이동1 = (HwpParameterSet)ax전체미리보기.ScrollPosInfo;
            전체이동1.SetItem("VertPos", 100);


            MessageBox.Show("인덱스 생성 및 세션타이틀이 저장 되었습니다.", "인덱스생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void 문단부호표시()
        {
            ax전체미리보기.Run("ViewOptionParaMark"); //문단부호 (반대로 표시됨 - 현재문서에 문단부호가 있으면 off, 반대로 문단부호가 없으면 on
            ax개별미리보기.Run("ViewOptionParaMark");

            //ax전체미리보기.Run("ViewOptionCtrlMark"); //조판부호
            //ax개별미리보기.Run("ViewOptionCtrlMark");
        }

        private void 한글유효성검사()
        {
            GridView gdview = view담보비교;
            int grdrow = gdview.RowCount;
            string 비교1, 비교2;
            int 불일치확인 = 0;
            for( int i =0; i < grdrow; i++) {
                비교1 = gdview.GetRowCellDisplayText(i, "원본내용").ToString();
                비교2 = gdview.GetRowCellDisplayText(i, "비교내용").ToString();
                if(비교1 != 비교2){
                    gdview.SetRowCellValue(i, "비교", "불일치");
                    gdview.SetRowCellValue(i, "확인", false);
                    불일치확인++;
                } else {
                    gdview.SetRowCellValue(i, "확인", true);
                }
            }
            MessageBox.Show(string.Format("원본과 불일치하는 약관이 {0}개 존재합니다.",불일치확인), "선택약관불일치확인(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void 한글원본기본설정()
        {
            tab담보비교.Select();
            int 전체페이지 = ax전체미리보기.PageCount;
            grd담보비교_화면초기화();
            GridView gdview = view담보분리;
            System.Data.DataTable table = new DataTable();
            table = 담보비교datatable();

            //원본한글
            HwpAction hwpact;
            HwpParameterSet 이동;
            hwpact = (HwpAction)ax전체미리보기.CreateAction("Goto");  //이동
            이동 = (HwpParameterSet)hwpact.CreateSet();

            //비교한글
            HwpAction hwpact1;
            HwpParameterSet 이동1;
            hwpact1 = (HwpAction)ax개별미리보기.CreateAction("Goto");  //이동
            이동1 = (HwpParameterSet)hwpact.CreateSet();


            HwpParameterSet 전체이동 = (HwpParameterSet)ax전체미리보기.ScrollPosInfo;
            전체이동.SetItem("VertPos", 100);

            HwpParameterSet 개별이동 = (HwpParameterSet)ax개별미리보기.ScrollPosInfo;
            개별이동.SetItem("VertPos", 100);

            int CtrlID = -1, 문단 = -1, 컬럼 = -1;
            
            int 시작문단 = 0, 시작컬럼 = 0, 종료문단 = 0, 종료컬럼 = 0;
            for (int i = 1; i <= 전체페이지; i++) {

                //원본한글
                이동.SetItem("DialogResult",i);
                이동.SetItem("SetSelectionIndex", 1);
                hwpact.Execute(이동);
                
                ax전체미리보기.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                시작문단 = 문단;
                시작컬럼 = 컬럼;
                ax전체미리보기.MovePos(23, 0, 0); //현재위치한 줄의 끝으로 이동
                ax전체미리보기.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                종료문단 = 문단;
                종료컬럼 = 컬럼;

                bool 설정 = ax전체미리보기.SelectText(시작문단, 시작컬럼, 종료문단, 종료컬럼);
                ax전체미리보기.ScrollPosInfo = 전체이동;

                ax전체미리보기.Run("Copy");
                string 내용 = Clipboard.GetText();
                Clipboard.Clear();

                //비교한글
                이동1.SetItem("DialogResult", i);
                이동1.SetItem("SetSelectionIndex", 1);
                hwpact1.Execute(이동1);

                ax개별미리보기.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                시작문단 = 문단;
                시작컬럼 = 컬럼;
                ax개별미리보기.MovePos(23, 0, 0); //현재위치한 줄의 끝으로 이동
                ax개별미리보기.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                종료문단 = 문단;
                종료컬럼 = 컬럼;

                bool 설정1 = ax개별미리보기.SelectText(시작문단, 시작컬럼, 종료문단, 종료컬럼);
                ax개별미리보기.ScrollPosInfo = 개별이동;

                ax개별미리보기.Run("Copy");
                string 내용1 = Clipboard.GetText();
                Clipboard.Clear();

                table.Rows.Add(new object[] {
                    false,
                    i.ToString(),
                    "",  
                    내용,
                    내용1
                });
            }

            ax전체미리보기.Run("MoveTopLevelEnd");
            ax개별미리보기.Run("MoveTopLevelEnd");

            grd담보비교.DataSource = table;
            gdview.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
        }

        private void 비교할HWP열기(string _비교할HWP경로) 
        {
            ax개별미리보기.CreateControl();
            ax개별미리보기.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax개별미리보기.Clear(1);
            ax개별미리보기.Open(_비교할HWP경로);
            ax개별미리보기.MovePos(2, 0, 0);
        }
        private void ax전체미리보기열기(string 한글파일경로)
        {
            //전체Hwp파일(원본)
            ax전체미리보기.CreateControl();
            ax전체미리보기.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax전체미리보기.Clear(1);
            ax전체미리보기.Open(한글파일경로);
            ax전체미리보기.MovePos(2, 0, 0);

        }
        private void ax개별미리보기열기(string 한글파일경로)    
        {

            AxHwpCtrl ax개별 = new AxHwpCtrl();

            ax개별.CreateControl();
            ax개별.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax개별.Clear(1);

            ax개별.Open(한글파일경로);

            ax개별.Run("CloseEx");
            ax개별.Run("CloseEx");
            ax개별.Run("CloseEx");
            ax개별.Run("CloseEx");
            ax개별.Run("MoveTopLevelEnd");
            ax개별.MovePos(3, 0, 0);

            ax개별.Run("SelectAll");
            ax개별.Run("Delete");

            기본양식폼 = string.Format("{0}\\Style\\style_Form.hwp",Application.StartupPath);  // 기본양식폼을 미리 생성한다. - 나중에 미리보기, 약관나누기 할때 기본바탕폼으로 사용한다.
            ax개별.SaveAs(기본양식폼);
            ax개별.Dispose();

            ax개별미리보기.CreateControl();
            ax개별미리보기.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax개별미리보기.Clear(1);

            ax개별미리보기.Open(기본양식폼);

        }
        // 분리된 담보를 다시 합쳐 원본과 비교를 한다.
        private void 담보합치기()
        {
            ax개별미리보기.Run("SelectAll");
            ax개별미리보기.Run("Delete");
            ax개별미리보기.Run("BreakPara"); // 복사를 하면 기존 양식(스타일, 문단모양)이 없어진다. (왜일까?)
            ax개별미리보기.Run("DeleteBack");

            ax개별미리보기.MovePos(3, 0, 0);
            GridView gdview = view담보분리;
            string 상품코드 = gdview.GetRowCellDisplayText(0, "상품코드").ToString();
            string 저장폴더 = string.Format("{0}\\{1}\\", new System.IO.FileInfo(한글파일경로).DirectoryName, 상품코드);
            string 한글파일명 = string.Format("(합본){0}.hwp", 상품코드);
            string 저장파일명 = "";
            string 공통담보문구 = "";

            for (int i = 0; i < gdview.RowCount; i++) {

                if (gdview.GetRowCellDisplayText(i, "문단").ToString() == "")
                    continue;
                else if (gdview.GetRowCellDisplayText(i, "담보코드").ToString().ToUpper() == "EOF" &&
                         gdview.GetRowCellDisplayText(i, "담보명").ToString() != "")   // 맨마지막 담보중 담보코드가 EOF 이고 담보명이 공백이 아니면 담보쪼개기에 포함되지 않는다.
                    continue;

                공통담보문구 = "";
                //합치기 할때는 원본과 같아야 하므로 타이틀만 있는 담보파일을 사용한다.
                if (gdview.GetRowCellDisplayText(i, "공통담보").ToString() == "공통") {
                    공통담보문구 = "_공통담보";
                }
                저장파일명 = string.Format("{0}sn{1}_{2}{3}.hwp",
                                                            저장폴더,
                                                            gdview.GetRowCellDisplayText(i, "일련번호").ToString(),
                                                            gdview.GetRowCellDisplayText(i, "담보코드").ToString(),
                                                            공통담보문구);
                ax개별미리보기.MovePos(3, 0, 0);
                ax개별미리보기.Insert(저장파일명);
            }
            ax개별미리보기.SaveAs(저장폴더 + 한글파일명);
            ax전체미리보기.MovePos(3, 0, 0); // 1차적으로 잘 조합 되었는지 원본, 대상 파일을 비교하기 위해

            담보분리메인화면event = new string[10];
            담보분리메인화면event[0] = "비교대상한글파일";
            담보분리메인화면event[1] = string.Format("{0}{1}", 저장폴더, 한글파일명);
            담보분리메인화면_SetMessage.Invoke(담보분리메인화면event);

            tab담보분리.SelectedTabPage = tab담보비교;

            MessageBox.Show("담보파일 병합이 완료 되었습니다.", "파일병합(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 한글인덱스저장()
        {
            GridView gdview = view담보분리;
            if (string.IsNullOrEmpty(담보인덱스파일)) {
                MessageBox.Show("담보리스트파일이 존재하지 않습니다.", "담보리스트오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (gdview.RowCount <= 0)
                return;

            using (System.IO.FileStream fs = new System.IO.FileStream(담보인덱스파일, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                using (System.IO.StreamWriter wr = new System.IO.StreamWriter(fs,Encoding.GetEncoding(949))) {
                    string 출력 = "";
                    for (int i = 0; i < gdview.RowCount; i++) {
                        출력 = "";
                        출력 += gdview.GetRowCellDisplayText(i, "일련번호").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "상품코드").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "담보코드").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "담보명").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "CtrlID").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "문단").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "컬럼").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "쪽").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "단").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "줄").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "칸").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "정확도").ToString().Replace("%","") + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "공통담보").ToString();

                        wr.WriteLine(출력);
                        wr.Flush();
                    }

                    gdview = view담보분석;
                    if (gdview.RowCount <= 0)
                        return;

                    출력 = "*****담보분석*******";
                    wr.WriteLine(출력);
                    wr.Flush();

                    for (int i = 0; i < gdview.RowCount; i++) {

                        출력 = "";
                        출력 += gdview.GetRowCellDisplayText(i, "위치정보").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "스타일").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "담보명").ToString() + '\t';
                        출력 += gdview.GetRowCellDisplayText(i, "공통담보").ToString();

                        wr.WriteLine(출력);
                        wr.Flush();
                    }

                }
                fs.Dispose();
            }
            MessageBox.Show("담보인덱스파일을 저장했습니다.", "저장완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
       
        private void 개별담보검색(GridView gdview, int 인수, int 전체번호, string 공통담보, string 저장파일명, bool 저장여부, List<int> 담보분석_위치정보,  DataTable 담보분석, out string 메세지, string 세션명, bool 비교세션)
        {
            메세지 = "완료";
            int 시작문단 = 0, 시작컬럼 = 0, 종료문단 = 0, 종료컬럼 = 0;
            string 다음공통담보 = "";
            string[] 위치정보분리;


            if (공통담보.Contains("서브")) {
                위치정보분리 = gdview.GetRowCellDisplayText(인수, "위치정보").Split('-');
                시작문단 = Convert.ToInt32(위치정보분리[1]);
                시작컬럼 = Convert.ToInt32(위치정보분리[2]);
            } else {
                시작문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(인수, "문단"));
                시작컬럼 = Convert.ToInt32(gdview.GetRowCellDisplayText(인수, "컬럼"));
            }

            if (전체번호 == 인수 + 1) {
                ax전체미리보기.Run("MoveTopLevelEnd");  // Ctrl + PageDown : 페이지 맨끝
                int _tmpCtrlID = 0;
                ax전체미리보기.GetPos(ref _tmpCtrlID, ref 종료문단, ref 종료컬럼);
            } else {
                if (공통담보.Contains("서브")) {
                    위치정보분리 = gdview.GetRowCellDisplayText(인수 + 1, "위치정보").Split('-');
                    종료문단 = Convert.ToInt32(위치정보분리[1]);
                    종료컬럼 = Convert.ToInt32(위치정보분리[2]);
                } else {

                    종료문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(인수 + 1, "문단"));
                    종료컬럼 = Convert.ToInt32(gdview.GetRowCellDisplayText(인수 + 1, "컬럼"));

                    if ( gdview.Name == "view담보분리") {
                        다음공통담보 = gdview.GetRowCellDisplayText(인수 + 1, "공통담보").ToString();
                        // 다음담보가 '서브'이면 바로 위에 서브타이이틀이 존재한다. 
                        // 따라서 그 서브타이틀은 범위에서 제외한다.
                        if (다음공통담보 == "서브") {
                            int 다음문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(인수 + 1, "문단").ToString());
                            int 담보분석인수 = 담보분석_위치정보.IndexOf(다음문단);
                            for (int idx = 담보분석인수 - 1; idx >= 0; idx--) {

                                if (담보분석.Rows[idx]["공통담보"].ToString() != "서브") {
                                    int 현재인수 = idx;
                                    위치정보분리 = 담보분석.Rows[현재인수]["위치정보"].ToString().Split('-');
                                    종료문단 = Convert.ToInt32(위치정보분리[1]);
                                    종료컬럼 = Convert.ToInt32(위치정보분리[2]);
                                    break;
                                }
                            }
                        } 

                    }
                    
                }
            }


            // ======================================================== [ 블럭선택, 저장 ] ========================================================



            Clipboard.Clear();

            bool 설정 = ax전체미리보기.SelectText(시작문단, 시작컬럼, 종료문단, 종료컬럼);

            if (설정 == false) {
                gdview.SetRowCellValue(인수, "상품코드", "블록실패");
                메세지 = "블록실패";
                return;
            }
            




            ax전체미리보기.Run("Copy");
            ax전체미리보기.Run("Copy");
            ax전체미리보기.Run("Copy");
            ax전체미리보기.Run("Copy");


            // 서브타이틀을 출력하고 담보내용을 출력할때 이다. ( 즉 먼저 타이틀 출력하고 본 내용을 출력하는 단계로 이전 내용을 삭제 하면 않된다.)
            // 공통담보가 '서브' ===> '서브2번째'로  변경된다. 1번째는 기존처럼 출력하고 2번째는 삽입형태 이다.
            if (공통담보 == "서브2번째") {
                ax개별미리보기.MovePos(3, 0, 0);
            } else {

                // 세션이 변경 되어서 새롭게 불러 온다.
                if (비교세션) {
                    string 세션파일명 = string.Format("{0}\\Style\\세션_{1}.hwp", Application.StartupPath, 세션명);
                    ax개별미리보기.CreateControl();
                    ax개별미리보기.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                    ax개별미리보기.Clear(1);

                    ax개별미리보기.Open(세션파일명);
                   
                } 
                ax개별미리보기.Run("SelectAll");
                ax개별미리보기.Run("Delete");
                ax개별미리보기.Run("BreakPara");  // enter 복사를 하면 기존 양식(스타일, 문단모양)이 없어진다. (왜일까?)
                ax개별미리보기.Run("DeleteBack"); // back space
               
            }

            //============ [ 빈파일이 발생하여 약간의 인터발을 준다 ] =================
            // 문제발생시 삭제 (1) 아래 또 있음


            System.Threading.Thread.Sleep(5);


            ax개별미리보기.MovePos(3, 0, 0);
            ax개별미리보기.Run("Paste");
            Application.DoEvents();
            Clipboard.Clear();

            //저장파일명이 공백이면 미리보기, 아니면 개별담보 저장
            if (!string.IsNullOrWhiteSpace(저장파일명)){

                // 모바일(메리츠화재)경우 일부 편집에서 페이지 나눔이 들어가 있어 그걸 없애 준다.
                ax개별미리보기.MovePos(3, 0, 0);
                
                //상태바의 정보를 가져온다.
                int 총구역 = -1, 현재구역 = -1, 쪽 = -1, 단 = -1, 줄 = -1, 칸 = -1;
                short 수정 = -1;
                string ctrlname = "";
                ax개별미리보기.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);

                if (줄 == 1) {
                    ax개별미리보기.Run("DeleteBack"); // back space
                    ax개별미리보기.Run("BreakPara");  // enter
                }

                ax개별미리보기.SaveAs(저장파일명);
            }

            // ================ 공통담보 로직 ================

            int 다음클릭Row = 인수;
            
            if (공통담보 == "공통") {
                do {
                    다음클릭Row++;
                    다음공통담보 = gdview.GetRowCellDisplayText(다음클릭Row, "공통담보").ToString().Trim();
                } while (다음공통담보 != "");

                int 공통시작문단 = 0, 공통시작컬럼 = 0, 공통종료문단 = 0, 공통종료컬럼 = 0;

                공통시작문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(다음클릭Row, "문단")) + 1; //제목을 제외한 다음 문단 부터
                공통시작컬럼 = Convert.ToInt32(gdview.GetRowCellDisplayText(다음클릭Row, "컬럼"));

                if (gdview.RowCount == 다음클릭Row + 1) {
                    ax전체미리보기.Run("MoveTopLevelEnd");  // Ctrl + PageDown : 페이지 맨끝  
                    int _tmpCtrlID = 0;
                    ax전체미리보기.GetPos(ref _tmpCtrlID, ref 종료문단, ref 종료컬럼);
                } else {
                    공통종료문단 = Convert.ToInt32(gdview.GetRowCellDisplayText(다음클릭Row + 1, "문단"));
                    공통종료컬럼 = Convert.ToInt32(gdview.GetRowCellDisplayText(다음클릭Row + 1, "컬럼"));
                }

                설정 = ax전체미리보기.SelectText(공통시작문단, 공통시작컬럼, 공통종료문단, 공통종료컬럼);

                if (설정 == false) {
                    MessageBox.Show("블럭설정에 실패 했습니다.", "복사불가", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ax전체미리보기.Run("Copy");
                ax전체미리보기.Run("Copy");
                ax전체미리보기.Run("Copy");
                ax전체미리보기.Run("Copy");



                ax개별미리보기.MovePos(3, 0, 0);

                //============ [ 빈파일이 발생하여 약간의 인터발을 준다 ] =================
                // 문제발생시 삭제 (2) 위 또 있음


                System.Threading.Thread.Sleep(5);


                ax개별미리보기.Run("Paste");
                Clipboard.Clear();

                // 이게 실질적으로 사용하는 담보
                // 합치기 할때는 '공통담보'문구 있는 파일사용
                if (!string.IsNullOrWhiteSpace(저장파일명)) {
                    저장파일명 = 저장파일명.Replace("_공통담보", "");

                    // 모바일(메리츠화재)경우 일부 편집에서 페이지 나눔이 들어가 있어 그걸 없애 준다.
                    ax개별미리보기.MovePos(3, 0, 0);

                    //상태바의 정보를 가져온다.
                    int 총구역 = -1, 현재구역 = -1, 쪽 = -1, 단 = -1, 줄 = -1, 칸 = -1;
                    short 수정 = -1;
                    string ctrlname = "";
                    ax개별미리보기.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);

                    if (줄 == 1) {
                        ax개별미리보기.Run("DeleteBack"); // back space
                        ax개별미리보기.Run("BreakPara");  // enter
                    }
                    
                    ax개별미리보기.SaveAs(저장파일명);

                } else {
                    ax전체미리보기.MovePos(0, 시작문단, 시작컬럼);
                    ax전체미리보기.SelectText(시작문단, 시작컬럼, 종료문단, 종료컬럼);
                }
            }

        }


        private void 담보쪼개기(int 시작, string 구분)
        {
            // 구분 : 일부분보기, 일부분저장, 전체저장

            try {
                GridView gdview = view담보분리;
                GridView gdview02 = view담보분석;
                bool 저장여부 = false;
                if (구분 == "일부분저장" || 구분 == "전체저장") {
                    저장여부 = true;
                }
                
                if (string.IsNullOrWhiteSpace(한글파일경로) || gdview.RowCount <= 0)
                return;

                string 저장폴더 = "";
                string 상품코드 = gdview.GetRowCellDisplayText(0, "상품코드").ToString();

                if (저장여부) {
                    // 상품코드로 폴더생성
                    if (string.IsNullOrEmpty(상품코드)) {
                        MessageBox.Show("상품코드가 존재하지 않습니다. \n 첫번째 레코드에 상품코드를 넣어 주세요. ", "상품코드오류");
                        return;
                    }

                    저장폴더 = string.Format("{0}\\{1}\\", new System.IO.FileInfo(한글파일경로).DirectoryName, 상품코드);
                    if (!new System.IO.DirectoryInfo(저장폴더).Exists)
                        new System.IO.DirectoryInfo(저장폴더).Create();
                }
                
                string 담보코드 = "";
                string 담보명 = "";
                string 문단 = "";
                string 공통담보 = "";
                string 저장파일명 = "";
                string 공통담보문구 = ""; // 타이틀만 있는것 저장
                string 메세지 = "";

                int 시작번호 = -1, 종료번호 = -1;
            
                int Row담보분리 = gdview.RowCount;
                int Row담보분석 = gdview02.RowCount;
                
                int 전체번호 = 0;



                //일부분저장, 일부분보기
                if (구분.Contains("일부분")) {
                    시작번호 = 시작;
                    종료번호 = 시작번호 + 1;

                } else if (구분 == "전체저장") {
                    시작번호 = 0;
                    종료번호 = Row담보분리;

                } else {
                    MessageBox.Show(string.Format("구분자 오류 - {0}", 구분), "작업불가(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                // 서브타이틀
                DataTable 담보분석 = (DataTable)grd담보분석.DataSource;
                DataView dv2 = new DataView(담보분석);
                dv2.Sort = "위치정보 ASC";
                담보분석 = dv2.ToTable();

                int 담보분석인수 = -1;
                string[] 위치정보분리 = null;
                List<int> 담보분석_위치정보 = new List<int>();
                for (int i = 0; i < 담보분석.Rows.Count; i++) {
                    위치정보분리 = 담보분석.Rows[i]["위치정보"].ToString().Split('-');
                    담보분석_위치정보.Add(Convert.ToInt32(위치정보분리[1])); // 문단의 값을 저장
                }

                Dictionary<int, string> tmp세션 = new Dictionary<int, string>();
                List<int> tmp담보 = new List<int>();
                string 비교세션파일명 = "";
                bool 세션타이틀미존재 = false;

                for (int i = 0; i < Row담보분리; i++) {

                    담보코드 = gdview.GetRowCellDisplayText(i, "담보코드").ToString();
                    switch (담보코드) {
                        case "타이틀_HB":
                        case "타이틀_BB":
                        case "타이틀_TB":
                        case "타이틀_SB":
                        case "타이틀_IB":
                        case "기본_H":
                        case "기본_B":
                        case "기본_T":
                        case "기본_S":
                        case "기본_I":
                            문단 = gdview.GetRowCellDisplayText(i, "문단").ToString().Trim();
                            tmp세션.Add(Convert.ToInt32(문단), 담보코드);
                            tmp담보.Add(Convert.ToInt32(문단));
                            break;
                    }
                }

                if (tmp세션.Count == 0) {

                    // =======================
                    //    세션타이틀 미존재
                    // =======================
                    // 세션타이틀이 없는경우 
                    // 강제로 1개를 생성한다.
                    //세션페이지생성() 참조
                    tmp세션.Add(0, "타이틀_XB");
                    tmp담보.Add(0);
                    세션타이틀미존재 = true;
                }

                tmp담보.Sort();

                for (int i = 시작번호; i < 종료번호; i++) {

                    bool 비교세션 = false;

                    전체번호 = Row담보분리;
                    int 인수 = i;
                    저장파일명 = "";

                    담보코드 = gdview.GetRowCellDisplayText(i, "담보코드").ToString();
                    담보명 = gdview.GetRowCellDisplayText(i, "담보명").ToString().Trim();
                    공통담보 = gdview.GetRowCellDisplayText(i, "공통담보").ToString();
                    문단 = gdview.GetRowCellDisplayText(i, "문단").ToString().Trim();

                    if (문단 == "")
                        continue;
                    else if (담보코드.ToUpper() == "EOF" && 담보명 != "")   // 맨마지막 담보중 담보코드가 EOF 이고 담보명이 공백이 아니면 담보쪼개기에 포함되지 않는다.
                        continue;

                    // 세션파일 불러오기 - 인덱스가 저장되어 있는 파일을 해당 담보에 맞게 불러온다.
                    int s문단 = Convert.ToInt32(문단);
                    string 세션명 = "";
                    int 세션인수 = -1;

                    if (세션타이틀미존재 == true) {
                        세션인수 = 0;
                    } else {
                        for (int s = 0; s < tmp담보.Count; s++) {
                            if (s == tmp담보.Count - 1) {
                                if (s문단 >= tmp담보[s]) {
                                    세션인수 = s;
                                    break;
                                }
                            } else if (s문단 >= tmp담보[s] && s문단 < tmp담보[s + 1]) {
                                세션인수 = s;
                                break;
                            }
                        }
                        if ( 세션인수  == -1) {
                            MessageBox.Show("세션(인덱스)파일을 찾기 못했습니다.\n인덱스 생성을 실행 했는지 확인하세요 ", "진행불가(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    세션명 = tmp세션[tmp담보[세션인수]];

                    if (세션명 != 비교세션파일명) {
                        비교세션 = true;
                        비교세션파일명 = 세션명;
                    }


                    // 공통담보가 존재하면         : sn0001_12345_공통담보.hwp
                    // 공통담보가 존재하지 않으면  : sn0001_12345.hwp
                    // 반대로 생각해야 한다. 실질적으로 '공통담보'란 문구가 있는 파일은 합치기 할 때 사용하고,
                    // '공통담보'문구가 없는 파일을 실제 사용한다.(보통 파일은 공통담보가 없는게 대 다수 이므로 )
                    if (저장여부) {
                        공통담보문구 = "";
                        if (공통담보 == "공통") {
                            // 합치기때 사용, 실질적인 담보는 아래쪽에서 저장
                            공통담보문구 = "_공통담보";
                        } else if (공통담보 == "서브") {
                            공통담보문구 = "_서브담보";
                        }

                        저장파일명 = string.Format("{0}sn{1}_{2}{3}.hwp",
                                                                    저장폴더,
                                                                    gdview.GetRowCellDisplayText(i, "일련번호").ToString(),
                                                                    담보코드, 공통담보문구);
                    }
                    

                    if (공통담보 == "서브") {

                        담보분석인수 = 담보분석_위치정보.IndexOf(Convert.ToInt32(문단));
                        for (int idx = 담보분석인수 - 1; idx >= 0; idx--) {

                            if (담보분석.Rows[idx]["공통담보"].ToString() != "서브")  {
                                int 현재인수 = idx;
                                전체번호 = Row담보분석;

                                개별담보검색(gdview02, 현재인수, 전체번호, 공통담보, 저장파일명, 저장여부, 담보분석_위치정보, 담보분석, out 메세지, 세션명, 비교세션);
                                공통담보 = "서브2번째";
                                공통담보문구 = "";
                                break;
                            }
                        }
                    }

                    if (공통담보 == "서브2번째") {
                        // 2번을 저장하는데 첫번째는 공통담보문구를 넣고 두번째는 문구를 공백으로 하고 
                        저장파일명 = string.Format("{0}sn{1}_{2}{3}.hwp",
                                                                        저장폴더,
                                                                        gdview.GetRowCellDisplayText(인수, "일련번호").ToString(),
                                                                        담보코드, 공통담보문구);
                    }

                    //서브타이틀은 담보분석 Table를 이용해서 처리 한다.
                    if (공통담보 == "서브2번째")  {
                        gdview = view담보분석;
                        인수 = 담보분석인수;
                    }

                    개별담보검색(gdview, 인수, 전체번호, 공통담보, 저장파일명, 저장여부, 담보분석_위치정보, 담보분석, out 메세지, 세션명, 비교세션);
                    
                    if ( 공통담보 == "서브2번째") {
                        gdview = view담보분리;
                    }

                    if (메세지 != "완료")
                        continue;
                }

                if (저장여부) {
                    MessageBox.Show("담보분리가 완료 되었습니다.", "개별담보분리(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            } catch (Exception e) {
                MessageBox.Show(e.Message + "\n더블클릭에 Row에 놓고 하세요.", "조작오류(datazone천재소년)");
            }


        }


        private void 묶음제목붙여넣기(int _대표제목, int _클릭Row, AxHwpCtrl _ax03, AxHwpCtrl ax미리보기_ax저장, GridView _gdview, out bool _대표제목찾기유무)
        {
            _대표제목찾기유무 = false;
            int 대표제목 = _대표제목;
            int 현재idx = _클릭Row;
            int 대표idx;
            
            묶음제목찾기(_gdview, 대표제목, 현재idx, out 대표idx, out _대표제목찾기유무);
            if (_대표제목찾기유무)
            {
                int 시작문단 = Convert.ToInt32(_gdview.GetRowCellDisplayText(대표idx, "문단"));
                int 시작컬럼 = Convert.ToInt32(_gdview.GetRowCellDisplayText(대표idx, "컬럼"));
                int 종료문단 = Convert.ToInt32(_gdview.GetRowCellDisplayText(대표idx + 1, "문단"));
                int 종료컬럼 = Convert.ToInt32(_gdview.GetRowCellDisplayText(대표idx + 1, "컬럼"));

                bool 설정 = _ax03.SelectText(시작문단, 시작컬럼, 종료문단, 종료컬럼);

                if (설정 == false) {
                    MessageBox.Show("블럭설정에 실패 했습니다.", "복사불가", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _ax03.Dispose();
                    return;
                } else {
                    _ax03.Run("Copy");
                    Application.DoEvents();

                    ax미리보기_ax저장.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                    ax미리보기_ax저장.Run("SelectAll");
                    ax미리보기_ax저장.Run("Delete");

                    ax미리보기_ax저장.Run("BreakPara"); // 복사를 하면 기존 양식(스타일, 문단모양)이 없어진다. (왜일까?)
                    ax미리보기_ax저장.Run("DeleteBack");
                    ax미리보기_ax저장.Run("Paste");
                   
                }
            }
        }

        private void 묶음제목찾기(GridView _gdview, int _대표제목, int _현재idx, out int _대표idx, out bool _대표제목찾기유무)
        {
            int 이전idx = _현재idx - 1;
            string tmp일련번호 = _gdview.GetRowCellDisplayText(이전idx, "일련번호").ToString();
            string[] 일련번호분리 = tmp일련번호.Split('-');

            _대표idx = 이전idx;
            _대표제목찾기유무 = false;

            int 현재제목 = Convert.ToInt32(일련번호분리[0]);
            if (현재제목 == _대표제목 && 일련번호분리.Length == 1)  {  // 하단 인덱스가 없는 경우
                _대표idx = 이전idx;
                _대표제목찾기유무 = true; // 찾기성공
                return;
            }
            else if (현재제목 < _대표제목 || 이전idx < 0) {
                return;
            }
            else {
                _현재idx = 이전idx;
                묶음제목찾기(_gdview, _대표제목, _현재idx, out _대표idx, out _대표제목찾기유무);
            }
        }


        private void grd담보비교_화면초기화()
        {
            GridView gdview = view담보비교;

            // [최적화 1] 렌더링 일시 중지 및 안전망 구축
            gdview.BeginUpdate();
            try
            {
                // --- 1. 기본 스타일 및 동작 설정 ---
                gdview.GroupPanelText = "원본한글파일과 비교한글파일을 비교합니다.";
                gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.GroupPanel.ForeColor = Color.DarkBlue;
                gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김

                gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
                gdview.Appearance.FocusedRow.BackColor = Color.Orange;
                gdview.Appearance.SelectedRow.BackColor = Color.BurlyWood;

                gdview.OptionsSelection.MultiSelect = true;          /** 멀티라인 선택가능 **/
                gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;   /** 멀티라인 선택시 쉬프트 가능 **/

                gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                gdview.OptionsBehavior.Editable = false;

                // --- 2. 데이터 바인딩 (쓸데없는 new DataTable() 제거) ---
                grd담보비교.DataSource = 담보비교datatable();

                // --- 3. 컬럼 비율 설정 ---
                int grdwidth = grd담보비교.Width;
                var columnWidths = new (string Name, double Ratio)[]
                {
                    ("확인", 0.05), ("쪽", 0.08), ("비교", 0.12), ("원본내용", 0.37), ("비교내용", 0.37)
                };

                foreach (var colInfo in columnWidths)
                {
                    var col = gdview.Columns[colInfo.Name];
                    if (col != null) col.Width = Convert.ToInt32(grdwidth * colInfo.Ratio);
                }

                // --- 4. 에디터 및 정렬 설정 ---
                if (gdview.Columns["비교"] != null)
                    gdview.Columns["비교"].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

                if (gdview.Columns["확인"] != null)
                {
                    var 체크박스 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
                    gdview.Columns["확인"].ColumnEdit = 체크박스;
                }
            }
            finally
            {
                // [최적화 2] 에러 시 그리드 먹통 방지
                gdview.EndUpdate();
            }
        }

        private void grd담보분석_화면초기화()
        {
            GridView gdview = view담보분석;

            gdview.BeginUpdate();
            try
            {
                // --- 1. 기본 스타일 및 동작 설정 ---
                gdview.GroupPanelText = "원본파일을 분석";
                gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.GroupPanel.ForeColor = Color.DarkBlue;
                gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김

                gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
                gdview.Appearance.FocusedRow.BackColor = Color.Orange;
                gdview.Appearance.SelectedRow.BackColor = Color.BurlyWood;

                gdview.OptionsSelection.MultiSelect = true;          /** 멀티라인 선택가능 **/
                gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;   /** 멀티라인 선택시 쉬프트 가능 **/

                gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                gdview.OptionsBehavior.Editable = false;

                // --- 2. 데이터 바인딩 ---
                grd담보분석.DataSource = 담보분석datatable();

                // --- 3. 컬럼 비율 설정 ---
                int grdwidth = grd담보분석.Width;
                var columnWidths = new (string Name, double Ratio)[]
                {
                    ("위치정보", 0.2), ("스타일", 0.1), ("담보명", 0.7)
                };

                foreach (var colInfo in columnWidths)
                {
                    var col = gdview.Columns[colInfo.Name];
                    if (col != null) col.Width = Convert.ToInt32(grdwidth * colInfo.Ratio);
                }

                // --- 4. 숨김 처리 ---
                if (gdview.Columns["공통담보"] != null)
                    gdview.Columns["공통담보"].Visible = false;
            }
            finally
            {
                gdview.EndUpdate();
            }
        }

        private void grd담보분리_화면초기화()
        {
            GridView gdview = view담보분리;

            // [최적화 1] 화면 깜빡임 방지 및 렌더링 최적화
            gdview.BeginUpdate();
            try
            {
                // --- 1. 기본 스타일 및 동작 설정 ---
                gdview.GroupPanelText = "②한글Index정의파일과 한글파일(Hwp)을 올려주세요 (클릭 후 더블클릭 하면 해당 담보가 자동 저장합니다.)";
                gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.GroupPanel.ForeColor = Color.DarkBlue;
                gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김

                gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
                gdview.Appearance.FocusedRow.BackColor = Color.Orange;
                gdview.Appearance.SelectedRow.BackColor = Color.BurlyWood;

                gdview.OptionsSelection.MultiSelect = true;          // 멀티라인 선택가능
                gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect; // 쉬프트 선택 가능

                gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                gdview.OptionsBehavior.Editable = false;

                // --- 2. 데이터 바인딩 ---
                // 불필요한 객체 생성(new DataTable) 제거
                grd담보분리.DataSource = 담보분리datatable();

                // --- 3. 컬럼 비율 설정 (튜플 배열) ---
                int grdwidth = grd담보분리.Width;
                var columnWidths = new (string Name, double Ratio)[]
                {
                    ("확인", 0.05), ("일련번호", 0.07), ("상품코드", 0.07), ("담보코드", 0.14),
                    ("담보명", 0.44), ("문단", 0.06), ("컬럼", 0.06), ("쪽", 0.06),
                    ("단", 0.06), ("줄", 0.06), ("칸", 0.06), ("정확도", 0.10), ("보기", 0.06)
                };

                foreach (var colInfo in columnWidths)
                {
                    var col = gdview.Columns[colInfo.Name];
                    if (col != null) col.Width = Convert.ToInt32(grdwidth * colInfo.Ratio);
                }

                // --- 4. 숨김 처리 ---
                if (gdview.Columns["CtrlID"] != null) gdview.Columns["CtrlID"].Visible = false;
                if (gdview.Columns["공통담보"] != null) gdview.Columns["공통담보"].Visible = false;

                // --- 5. 정렬 (HAlignment) 일괄 설정 ---
                string[] centerCols = { "상품코드", "담보코드" };
                string[] farCols = { "문단", "컬럼", "쪽", "단", "줄", "칸" };

                foreach (string c in centerCols)
                {
                    if (gdview.Columns[c] != null)
                        gdview.Columns[c].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                }

                foreach (string c in farCols)
                {
                    if (gdview.Columns[c] != null)
                        gdview.Columns[c].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                }

                // --- 6. RepositoryItem (에디터) 설정 ---
                if (gdview.Columns["확인"] != null)
                {
                    var 체크박스 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
                    gdview.Columns["확인"].ColumnEdit = 체크박스;
                }

                if (gdview.Columns["정확도"] != null)
                {
                    // [최적화 2] 객체 초기화자(Object Initializer)를 사용하여 코드를 한 덩어리로 압축
                    var 정확도상태바 = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        ShowTitle = true,
                        PercentView = true,
                        Step = 1
                    };
                    gdview.Columns["정확도"].ColumnEdit = 정확도상태바;
                }
            }
            finally
            {
                // [최적화 1] 에러 발생 시에도 그리드 멈춤 완벽 방지
                gdview.EndUpdate();
            }
        }

        private DataTable 담보분리datatable()
        {
            System.Data.DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("확인", typeof(bool)));
            table.Columns.Add(new DataColumn("일련번호", typeof(string)));
            table.Columns.Add(new DataColumn("상품코드", typeof(string)));
            table.Columns.Add(new DataColumn("담보코드", typeof(string)));
            table.Columns.Add(new DataColumn("담보명", typeof(string)));
            
            table.Columns.Add(new DataColumn("CtrlID", typeof(string)));
            table.Columns.Add(new DataColumn("문단", typeof(string)));
            table.Columns.Add(new DataColumn("컬럼", typeof(string)));

            table.Columns.Add(new DataColumn("쪽", typeof(string)));
            table.Columns.Add(new DataColumn("단", typeof(string)));
            table.Columns.Add(new DataColumn("줄", typeof(string)));
            table.Columns.Add(new DataColumn("칸", typeof(string)));
            table.Columns.Add(new DataColumn("정확도", typeof(string)));
            table.Columns.Add(new DataColumn("공통담보", typeof(string)));
            table.Columns.Add(new DataColumn("보기", typeof(string)));
            


            return table;
        }

        private DataTable 담보분석datatable()
        {
            System.Data.DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("위치정보", typeof(string)));
            table.Columns.Add(new DataColumn("스타일", typeof(string)));
            table.Columns.Add(new DataColumn("담보명", typeof(string)));
            table.Columns.Add(new DataColumn("공통담보", typeof(string)));

            return table;
        }

        private DataTable 담보비교datatable()
        {
            System.Data.DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("확인", typeof(bool)));
            table.Columns.Add(new DataColumn("쪽", typeof(string)));
            table.Columns.Add(new DataColumn("비교", typeof(string)));
            table.Columns.Add(new DataColumn("원본내용", typeof(string)));
            table.Columns.Add(new DataColumn("비교내용", typeof(string)));
            return table;
        }


        private void grd담보분리_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }
                       
        private void grd담보분리_DragDrop(object sender, DragEventArgs e)
        {
            GridView gdview = view담보분리;
            GridView gdview02 = view담보분석;

            grd담보분리_화면초기화();
            grd담보분석_화면초기화();

            string[] 담보분리스트 = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            담보인덱스파일 = 담보분리스트[0];
            string[] data = System.IO.File.ReadAllLines(담보분리스트[0], System.Text.Encoding.GetEncoding(949));
            if (data.Length <= 0 || new System.IO.FileInfo(담보분리스트[0]).Extension.ToUpper() == ".HWP") {
                MessageBox.Show("약관담보 리스트파일을 올리세요.", "리스트오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DataTable table = new DataTable();
            table = 담보분리datatable();

            DataTable table02 = new DataTable();
            table02 = 담보분석datatable();

            int 인덱스찾기오류 = 0;
            bool 담보분석여부 = false;
            for (int i = 0; i < data.Length; i++) {
                
                if (data[i].Trim() == "*****담보분석*******") {
                    담보분석여부 = true;
                    continue;
                }

                string[] 분리 = data[i].Split('\t');

                if (담보분석여부)
                {
                    table02.Rows.Add(new object[]
                    {
                        분리[0],
                        분리[1],
                        분리[2],
                        분리[3]
                    });
                } else {

                    if ( 분리.Length != 4 && 분리.Length != 13) {
                        MessageBox.Show("약관담보 리스트파일 규칙에 위배 됩니다.\n\n 규칙) 일련번호, 상품코드, 담보코드, 담보명", "리스트오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string[] 일련번호분리 = 분리[0].Split('-');
                    일련번호분리[0] = 일련번호분리[0].PadLeft(4, '0');
                    string 일번자리맞춤 = string.Join("-", 일련번호분리); ;

                    string tmpCtrlID = "", tmp문단 = "", tmp컬럼 = "", tmp쪽 = "", tmp단 = "", tmp줄 = "", tmp칸 = "", tmp정확도 = "", tmp공통담보 = "";
                    if (분리.Length > 5) {
                        tmpCtrlID = 분리[4];
                        tmp문단 = 분리[5];
                        tmp컬럼 = 분리[6];
                        tmp쪽 = 분리[7];
                        tmp단 = 분리[8];
                        tmp줄 = 분리[9];
                        tmp칸 = 분리[10];
                        tmp정확도 = 분리[11];
                        tmp공통담보 = 분리[12];
                    }
                    table.Rows.Add(new object[] {
                        false,                                       // 확인     
                        일번자리맞춤,                                // 일련번호  
                        분리[1].ToString().Trim(),                   // 상품코드
                        분리[2].ToString().Trim(),                   // 담보코드
                        분리[3].ToString().Trim(),                   // 담보명

                        // 여기까지는 기본이고 아래는 담보인덱스를 찾고 저장한 데이타

                        tmpCtrlID,                                   // CtrlID
                        tmp문단,                                     // 문단
                        tmp컬럼,                                     // 컬럼

                        tmp쪽,                                       // 쪽
                        tmp단,                                       // 단
                        tmp줄,                                       // 줄
                        tmp칸,                                       // 칸
                        tmp정확도,                                   // 정확도
                        tmp공통담보                                  // 공통담보
                    });

                    if (tmp문단 == "0" && i != 0)
                        인덱스찾기오류++;

                }

                Application.DoEvents();
            }

            if (인덱스찾기오류 > 0) {
                담보분리메인화면event = new string[10];
                담보분리메인화면event[0] = "담보찾기오류";
                담보분리메인화면event[1] = string.Format("담보찾기오류 : {0}건", 인덱스찾기오류);
                담보분리메인화면_SetMessage.Invoke(담보분리메인화면event);

            }
            grd담보분리.DataSource = table;
            grd담보분석.DataSource = table02;

            gdview.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
            gdview02.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
        }

        private void view담보분리_CustomRowCellEdit(object sender, CustomRowCellEditEventArgs e)
        {
            if (e.Column.FieldName == "보기")
            {
                DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit buttonEdit = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();

                if (view담보분리.GetRowCellValue(e.RowHandle, "담보코드").ToString().ToUpper() == "EOF" &&
                    view담보분리.GetRowCellValue(e.RowHandle, "담보명").ToString() != "") {                    
                    return;
                }
                
                buttonEdit.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                buttonEdit.Buttons[0].ImageOptions.Image = Image.FromFile(string.Format("{0}\\img\\미리보기.png",Application.StartupPath));
                buttonEdit.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
                e.RepositoryItem = buttonEdit;
            }
            
        }

        private void frm담보분리_FormClosed(object sender, FormClosedEventArgs e)
        {
            frmEasy약관.담보분리_SetMessage -= new frmEasy약관.담보분리_Handler(event_담보분리_setMessage);

        }

        private void view담보분리_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle >= 0 &&
                   view담보분리.GetRowCellValue(e.RowHandle, "담보코드").ToString().ToUpper() == "EOF" &&
                   view담보분리.GetRowCellValue(e.RowHandle, "담보명").ToString() != "") {
                e.Appearance.ForeColor = Color.DimGray;
            }

            if ((e.RowHandle >= 0) && 
                ((view담보분리.GetRowCellValue(e.RowHandle, "문단").ToString() == "0" && e.RowHandle != 0) ||
                 view담보분리.GetRowCellValue(e.RowHandle, "상품코드").ToString() == "블록실패")) {
                e.Appearance.BackColor = Color.Red;
            }
            if (e.RowHandle >= 0 && view담보분리.GetRowCellValue(e.RowHandle, "공통담보").ToString() == "서브") {
                e.Appearance.BackColor = Color.DarkBlue;
            } else if (e.RowHandle >= 0 &&
                view담보분리.GetRowCellValue(e.RowHandle, "정확도").ToString().ToUpper() != "0" &&
                view담보분리.GetRowCellValue(e.RowHandle, "정확도").ToString().ToUpper() != "100" && 
                view담보분리.GetRowCellValue(e.RowHandle, "정확도").ToString().ToUpper() != "") {
                e.Appearance.BackColor = Color.CornflowerBlue;

                담보분리메인화면event = new string[10];
                담보분리메인화면event[0] = "정확도100미만";
                담보분리메인화면_SetMessage.Invoke(담보분리메인화면event);
            } else if (e.RowHandle >= 0 && view담보분리.GetRowCellValue(e.RowHandle, "공통담보").ToString() == "공통") {
                e.Appearance.BackColor = Color.Green;
            } 
        }

        private void frm담보분리_Resize(object sender, EventArgs e)
        {
            //group약관분석
            //group담보미리보기

            int 표시영역 = this.Size.Width;
            int 왼쪽영역 = Convert.ToInt32((double)표시영역 * 0.55);
            int 오른쪽영역 = Convert.ToInt32((double)표시영역 * 0.45) - 8;
            group약관분석.Width = 왼쪽영역;
            group담보미리보기.Width = 오른쪽영역;

            group담보미리보기.Location = new Point(왼쪽영역 + 6, 2);

            int 왼쪽_1 = Convert.ToInt32((double)왼쪽영역 * 0.7);
            int 왼쪽_2 = Convert.ToInt32((double)왼쪽영역 * 0.3) - 6;
            tab담보분리.Width = 왼쪽_1;
            grd담보분석.Width = 왼쪽_2;
            grd담보분석.Location = new Point(왼쪽_1 + 4, 62);

            int hwp표시영역 = group담보미리보기.Size.Width;
            int 개당영역 = hwp표시영역 / 2;
            Hwp영역1.Width = 개당영역 - 2 ;
            Hwp영역2.Width = 개당영역 - 2;
            Hwp영역2.Location = new Point(개당영역 + 2, 28);


            int 메세지영역 = grd담보분석.Size.Width;
            lab메세지1.Width = Convert.ToInt32((double)메세지영역 * 0.38);
            lab메세지2.Width = Convert.ToInt32((double)메세지영역 * 0.62);
            lab메세지띠.Width = 메세지영역;

            lab메세지1.Location =  new Point(왼쪽_1 + 4, 31);
            lab메세지2.Location =  new Point(왼쪽_1 + 4 + lab메세지1.Width, 31);
            lab메세지띠.Location =  new Point(왼쪽_1 + 4, 52);

            int 기다림X = grd담보분석.Location.X + (grd담보분석.Size.Width - 기다림.Size.Width ) / 2;
            int 기다림Y = grd담보분석.Location.Y + (grd담보분석.Size.Height - 기다림.Size.Height ) / 2;
            기다림.Location = new Point(기다림X, 기다림Y);

        }

        private void view담보비교_RowStyle(object sender, RowStyleEventArgs e)
        {
            GridView gdview = view담보비교;
            if (e.RowHandle < 0) { return; }

            if (gdview.GetRowCellDisplayText(e.RowHandle, "비교").ToString() == "불일치" )
                e.Appearance.BackColor = Color.Red;
        }
        private void view담보분석_RowStyle(object sender, RowStyleEventArgs e)
        {
            int 현재인수 = e.RowHandle;
            if (현재인수 < 0) { return; }

            if (view담보분석.GetRowCellDisplayText(현재인수, "공통담보").ToString() == "공통") {
                e.Appearance.BackColor = Color.Green;
            } else if (view담보분석.GetRowCellDisplayText(현재인수, "공통담보").ToString() == "서브") {
                e.Appearance.BackColor = Color.DarkBlue;
            }

        }

        private void 원본담보위치정보()
        {
            if (string.IsNullOrWhiteSpace(한글파일경로))  {
                MessageBox.Show("한글파일을 올려 주세요.", "한글경로오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string 파일경로 = 한글파일경로;
            AxHwpCtrl ax03 = new AxHwpCtrl();
            ax03.CreateControl();
            ax03.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            ax03.Clear();
            ax03.Open(파일경로);


            HwpAction hwpact;
            HwpParameterSet textFind;
            hwpact = (HwpAction)ax03.CreateAction("RepeatFind");  //FindAll 
            textFind = (HwpParameterSet)hwpact.CreateSet();

            DataTable table = new DataTable();
            table = 담보분석datatable();

            List<string> 스타일 = new List<string>(); 
            foreach (string value in 스타일값.Values) {
                스타일.Add(value);  //"제목23", "제목띠(Gray)", "제목선"
            }
            스타일 = 스타일.Distinct().ToList(); // 스타일중복제거


            for ( int i = 0; i < 스타일.Count; i++) {
                Clipboard.Clear();
                Clipboard.Clear();
                

                List<string> tmp위치정보 = new List<string>();
                int 찾기중복 = 0;
                int 비교문단 = -1;
                ax03.MovePos(2, 0, 0); // 문서의 시작으로 이동
                bool 처음 = true;

                Clipboard.Clear();

                do {
                    if (처음) {

                        Clipboard.Clear();

                        textFind.SetItem("Direction", 2);              // 찾을 방향 : 0 = 아래쪽, 1 = 위쪽, 2 = 문서 전체
                        textFind.SetItem("FindStyle", 스타일[i]);      // *** ※ 스타일 적용해서 제목을 찾음 - 중간에 제목이 2번이상 나올때가 존재함 ***
                        textFind.SetItem("IgnoreMessage", 1);          // 메시지 무시 여부
                        textFind.SetItem("FindType", 1);               // 다시 찾기를 할 때 마지막으로 실행한 [찾기 TRUE]를 할 것인가
                        처음 = false;
                    }
                    hwpact.Execute(textFind);

                    int CtrlID = -1, 문단 = -1, 컬럼 = -1;
                    int 총구역 = -1, 현재구역 = -1;
                    int 쪽 = -1, 단 = -1, 줄 = -1, 칸 = -1;
                    short 수정 = -1;
                    string ctrlname = "";

                    ax03.Run("Copy");
                    string 내용 = Clipboard.GetText();
                    Clipboard.Clear();

                    // ※ 찾기는 블럭형태로 문자를 찾아 준다. 즉 현재 커서위치는 문자열 맨 뒤에 있다. 다음에 담보 자르기를 하기 위해는 커서가 문자열 맨 앞으로 가야 한다.
                    // 1) 내용을 복사한다.
                    // 2) 현재위치를 저장한다.
                    // 3) 표,문자열 맨 앞으로 커서를 옮긴다. (즉 복사의 시작점)
                    // 4) 위치정보를 다시 얻어 온다.
                    // 5) 위치정보를 저장한다.
                    // 6) 다시 위치정보를 문자열 맨뒤로 옮긴다. - 이렇게 하지 않으면 찾기가 계속 같은내용으로 반복된다.


                    int tmpCtrlID = -1;
                    int tmp문단 = -1;
                    int tmp컬럼 = -1;

                    // 캐럿의 위치를 얻어온다.
                    ax03.GetPos(ref CtrlID, ref 문단, ref 컬럼);


                    //CtrlID > 0 : 객체로 존재 (즉 표,박스등..)
                    bool 객체여부 = false;
                    if (CtrlID > 0) {
                        ax03.MovePos(22, 0, 0);    //현재 위치한 줄의 시작
                        ax03.Run("CloseEx");       //Table밖으로 캐럿 이동(Shift + Esc) , _CtrlID : 0을 셋팅된다.
                        ax03.Run("CloseEx");
                        ax03.Run("CloseEx");
                        ax03.Run("CloseEx");
                        객체여부 = true;
                    } else {
                        tmpCtrlID = CtrlID; tmp문단 = 문단; tmp컬럼 = 컬럼;
                        ax03.MovePos(22, 0, 0);      //현재 위치한 줄의 시작
                        //ax03.MovePos(21, 0, 0);      //한 줄 위로 이동.
                    }
                    ax03.GetPos(ref CtrlID, ref 문단, ref 컬럼);

                    //상태바의 정보를 가져온다.
                    ax03.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);

                    string 오류검증 = string.Format("{0}{1}{2}{3}", 쪽, 단, 줄, 칸);
                    if (오류검증 == "1111") {
                        // 검증실패
                    }
                    string 위치정보 = string.Format("{0:0000}-{1:00000}-{2:0000}-{3:0000}-{4:0000}-{5:0000}-{6:0000}",0 ,문단,컬럼,쪽,단,줄,칸); //CtrlID

                    if (객체여부) {
                        ax03.MovePos(23, 0, 0);      //현재 위치한 줄의 끝으로 이동.
                    } else {
                        ax03.MovePos(tmpCtrlID, tmp문단, tmp컬럼);
                    }

                    if (tmp위치정보.IndexOf(위치정보) == -1 && 문단 > 비교문단 ) {
                        tmp위치정보.Add(위치정보); // 현재담보내용의 위치를 저장한다.
                        table.Rows.Add(new object[]
                        {
                            위치정보,                  // 위치정보
                            스타일[i],                 // 찾는 스타일
                            내용,                      // 찾은 담보내용
                            ""                         // 공통담보
                        });

                        비교문단 = 문단;
                    } else {
                        찾기중복++;
                        //중복됨 2번이면 종료한다.
                    }
                    Application.DoEvents();
                } while (찾기중복 <= 2);

            }

            //table.DefaultView.Sort = "위치정보 ASC"; // Add로 데이터를 넣고 정렬하면 되는데 DataSource로 하면 정렬이 되질 않는다.

            DataView dv = new DataView(table);
            dv.Sort = "위치정보 ASC";
            table = dv.ToTable();

            string 서브타이틀 = "";
            if (스타일값.ContainsKey("묶음담보"))  {
                서브타이틀 = 스타일값["묶음담보"];
            }
            for (int i = 0; i < table.Rows.Count; i++)  {
                int 현재인수 = i;

                // ex) 12. 타이틀 ==> 12-1 타이틀, 12-2 타이틀이 있을경우 12. 타이틀, 12-1. 타이틀 하나, 12. 타이틀 12-2. 타이틀 인 경우 (DB에 경우)
                if (string.IsNullOrWhiteSpace(서브타이틀) == false && table.Rows[i]["스타일"].ToString() == 서브타이틀) {
                    table.Rows[i]["공통담보"] = "서브";
                    if (table.Rows[i - 1]["공통담보"].ToString() != "서브") {
                        table.Rows[i - 1]["공통담보"] = "묶음타이틀";
                    }

                } else if ( (현재인수 != table.Rows.Count - 1) &&
                    (table.Rows[i]["스타일"].ToString() == table.Rows[i+1]["스타일"].ToString()) )  {
                    string[] 현재위치정보분리 = table.Rows[i]["위치정보"].ToString().Split('-');
                    string[] 위치정보분리 = table.Rows[i + 1]["위치정보"].ToString().Split('-');

                    // 타이틀과 타이틀 문단 간격이 1이면 담보내용을 서로 같이 쓴다(?)
                    if (Math.Abs(Convert.ToInt32(현재위치정보분리[1]) - Convert.ToInt32(위치정보분리[1])) == 1) {
                        table.Rows[i]["공통담보"] = "공통";
                    }
                }
            }

            grd담보분석.DataSource = table;
            view담보분석.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
            ax03.Dispose();
        }

       
        private void 한글인덱스생성()
        {

            DataTable 담보분리 = (DataTable)grd담보분리.DataSource;
            DataTable 담보분석 = (DataTable)grd담보분석.DataSource;

            DataView dv1 = new DataView(담보분리);
            dv1.Sort = "일련번호 ASC";
            담보분리 = dv1.ToTable();

            DataView dv2 = new DataView(담보분석);
            dv2.Sort = "위치정보 ASC";
            담보분석 = dv2.ToTable();

            var rowToUpdate = 담보분리.AsEnumerable().Where(r => r.Field<string>("상품코드") != "");
            foreach(var row in rowToUpdate) {
                row["CtrlID"] = "";
                row["문단"] = "";
                row["컬럼"] = "";
                row["쪽"] = "";
                row["단"] = "";
                row["줄"] = "";
                row["칸"] = "";
                row["정확도"] = "";
                row["공통담보"] = "";
            }
            grd담보분리.DataSource = 담보분리;

            
            //담보분석.DefaultView.Sort = "위치정보 ASC";

            /*
            var 검색결과 = from tb1 in 담보분리.AsEnumerable()
                       join tb2 in 담보분석.AsEnumerable()
                       on tb1.Field<string>("담보명") equals tb2.Field<string>("담보명") into dataKey
                       from tbResult in dataKey.DefaultIfEmpty()
                       select new
                       {
                           일련번호 = tb1.Field<string>("일련번호"),
                           담보명 = tb1.Field<string>("담보명"),
                           위치정보 = (tbResult == null ? "" : tbResult.Field<string>("위치정보"))
                           
                       };
            foreach(var 결과값 in 검색결과)
            {
                MessageBox.Show(결과값.담보명, 결과값.위치정보);
            } */


            //담보분석.Columns.Add("확인", typeof(string));

            string[] 위치정보분리 = null;
            ArrayList 담보분석_위치정보 = new ArrayList();
            for ( int i = 0; i < 담보분석.Rows.Count; i++) {
                위치정보분리 = 담보분석.Rows[i]["위치정보"].ToString().Split('-');
                담보분석_위치정보.Add(위치정보분리[1]); // 문단의 값을 저장
            }


            int 담보분리row = 담보분리.Rows.Count;
            List<int> 불일치 = new List<int>();
            for (int i = 0; i < 담보분리row; i++) {

                string 담보명 = 담보분리.Rows[i]["담보명"].ToString().Replace(" ","");
                string 담보코드 = 담보분리.Rows[i]["담보코드"].ToString();
                int 담보길이 = 담보명.Length;

                string 스타일 = 스타일값["담보제목"];
                foreach (string Key in 스타일값.Keys) {
                    if (담보코드.Contains(Key)) {
                        스타일 = 스타일값[Key];
                        break;
                    }
                }

                string 비교담보명 = "";
                string 비교스타일 = "";

                int 담보분석row = 담보분석.Rows.Count;
                bool 담보찾기여부 = false;
                string 묶음타이틀 = "";
                
                for ( int j = 0; j < 담보분석row; j++) {

                    비교담보명 = 담보분석.Rows[j]["담보명"].ToString().Replace(" ", "");
                    if (담보길이 > 비교담보명.Length)
                        continue;

                    비교담보명 = 비교담보명.Substring(0,담보길이);   // 실제 약관책자에 있는명과 목차에 있는명이 서로 상이한 경우가 많아 목차를 기준으로 찾는다.
                    비교스타일 = 담보분석.Rows[j]["스타일"].ToString();
                    묶음타이틀 = 담보분석.Rows[j]["공통담보"].ToString();

                    if ( 담보명 == 비교담보명 && 스타일 == 비교스타일 && 묶음타이틀 != "묶음타이틀") {
                        위치정보분리 = 담보분석.Rows[j]["위치정보"].ToString().Split('-');
                        담보분리.Rows[i]["CtrlID"] = 위치정보분리[0];
                        담보분리.Rows[i]["문단"] = 위치정보분리[1];
                        담보분리.Rows[i]["컬럼"] = 위치정보분리[2];
                        담보분리.Rows[i]["쪽"] = 위치정보분리[3];
                        담보분리.Rows[i]["단"] = 위치정보분리[4];
                        담보분리.Rows[i]["줄"] = 위치정보분리[5];
                        담보분리.Rows[i]["칸"] = 위치정보분리[6];
                        담보분리.Rows[i]["정확도"] = "100";
                        담보분리.Rows[i]["공통담보"] = 담보분석.Rows[j]["공통담보"].ToString();

                        담보찾기여부 = true;
                        break;
                    }
                }

                //정확하게 일치하는 담보타이틀이 없는 경우 포함%로 표시한다.
                if (담보찾기여부 == false) {
                    불일치.Add(i);
                }
            }

            // 불일치 한 담보를 찾기위한 로직 - 앞,뒤값을 가져와 근사값으로 찾는다.


            try
            {

                foreach (int 인수 in 불일치) {
                    string 불일치담보명 = 담보분리.Rows[인수]["담보명"].ToString().Replace(" ", "");

                    // 불일치 한 경우가 맨처음, 맨마지막인 경우
                    // 전,후를 기준으로 속해 있는 모든 문단을 가져와서 가장 근사값을 가져온다. -실질적으로 1개 이나 간혹 담보타일틀에 설명이이 첨부되어 있는 사항이 있음

                    string tmp문단 = "";
                    int 다음인수 = 인수;
                    int 이전인수 = 인수;
                    int 담보분석row = 담보분석.Rows.Count;

                    bool 시작Row포함 = false;
                    bool 종료Row포함 = false;
                    do {
                        이전인수--;
                        if (이전인수 < 0) {
                            break;
                        }
                        tmp문단 = 담보분리.Rows[이전인수]["문단"].ToString().Trim();
                    } while (string.IsNullOrWhiteSpace(tmp문단));

                    // 마지막 담보도 100%가 아닌경우 담보분석의 값을 넣어 준다.(주의요망)

                    if (string.IsNullOrWhiteSpace(tmp문단)) {
                        위치정보분리 = 담보분석.Rows[0]["위치정보"].ToString().Split('-');
                        이전인수 = 0;
                        시작Row포함 = true;
                    }
                    
                    tmp문단 = "";

                    do {
                        다음인수++;
                        if (다음인수 > 담보분리row - 1) {
                            break;
                        }

                        tmp문단 = 담보분리.Rows[다음인수]["문단"].ToString().Trim();
                    } while (string.IsNullOrWhiteSpace(tmp문단));

                    // 마지막 담보도 100%가 아닌경우 담보분석의 값을 넣어 준다.(주의요망)
                    if (string.IsNullOrWhiteSpace(tmp문단)) {
                        위치정보분리 = 담보분석.Rows[담보분석row - 1]["위치정보"].ToString().Split('-');
                        다음인수 = 담보분리row - 1;
                        종료Row포함 = true;
                    }
                    
                    string tmp스타일값 = 스타일값["담보제목"];
                    foreach (KeyValuePair<string, string> item in 스타일값) {
                        if (담보분리.Rows[인수]["담보코드"].ToString().Contains(item.Key)) {
                            tmp스타일값 = item.Value;
                            break;
                        }
                    }

                    //if (인수 == 25)
                    //    Debugger.Break();


                    int 담보분석_문단인수_앞 = (시작Row포함 == true) ? 0 : 담보분석_위치정보.IndexOf(담보분리.Rows[이전인수]["문단"].ToString());
                    int 담보분석_문단인수_뒤 = (종료Row포함 == true) ? 담보분석_위치정보.Count - 1 : 담보분석_위치정보.IndexOf(담보분리.Rows[다음인수]["문단"].ToString());
                    bool 간격비교 = 다음인수 - 이전인수 == 담보분석_문단인수_뒤 - 담보분석_문단인수_앞 ? true : false;

                    int 해당위치 = -1;
                    double 최대근사값 = 0;
                    double 일치문자 = 0;
                    double 길이 = 불일치담보명.Length;
                    if (간격비교) {

                        if (시작Row포함) {
                            해당위치 = 담보분석_문단인수_앞;
                        } else {
                            해당위치 = 담보분석_문단인수_앞 + 1;
                        }
                        foreach (char 문자 in 불일치담보명) {
                            if (담보분석.Rows[해당위치]["담보명"].ToString().Contains(문자)) {
                                최대근사값++;
                            }
                        }
                    } else {

                        for (int p = 담보분석_문단인수_앞; p <= 담보분석_문단인수_뒤; p++)  {
                            if ((시작Row포함 == false && p == 담보분석_문단인수_앞) || 
                                (종료Row포함 == false && p == 담보분석_문단인수_뒤) ||
                                담보분석.Rows[p]["공통담보"].ToString() == "묶음타이틀" ) {
                                continue;
                            }

                            일치문자 = 0;
                            foreach (char 문자 in 불일치담보명) {
                                if (담보분석.Rows[p]["담보명"].ToString().Contains(문자)) {
                                    일치문자++;
                                }
                            }

                            if (일치문자 > 최대근사값) {
                                최대근사값 = 일치문자;
                                해당위치 = p;
                            }
                        }
                    }

                    위치정보분리 = 담보분석.Rows[해당위치]["위치정보"].ToString().Split('-');
                    int 정확도 = 0;
                    정확도 = (int)(최대근사값 / 길이 * 100);
                    담보분리.Rows[인수]["CtrlID"] = 위치정보분리[0];
                    담보분리.Rows[인수]["문단"] = 위치정보분리[1];
                    담보분리.Rows[인수]["컬럼"] = 위치정보분리[2];
                    담보분리.Rows[인수]["쪽"] = 위치정보분리[3];
                    담보분리.Rows[인수]["단"] = 위치정보분리[4];
                    담보분리.Rows[인수]["줄"] = 위치정보분리[5];
                    담보분리.Rows[인수]["칸"] = 위치정보분리[6];
                    담보분리.Rows[인수]["정확도"] = 정확도.ToString();
                    담보분리.Rows[인수]["공통담보"] = 담보분석.Rows[해당위치]["공통담보"].ToString();
                }

            } catch (Exception) {
                    grd담보분리.DataSource = 담보분리;
                    return;
            }

            grd담보분리.DataSource = 담보분리;
        }

        private void view담보분리_DoubleClick(object sender, EventArgs e)
        {
            GridView gdview = view담보분리;
            if (gdview.RowCount == 0) return;
            
            int 클릭Row = gdview.FocusedRowHandle;
            // 자동저장
            담보쪼개기(클릭Row, "일부분저장");
        }

        private void view담보분리_Click(object sender, EventArgs e)
        {
            GridView gdview = view담보분리;

            string 클릭필드 = gdview.FocusedColumn.FieldName;
            int 클릭Row = gdview.FocusedRowHandle; // 타이틀을 클릭해도 0값이 반환된다.

            if (gdview.RowCount == 0 || 
                string.IsNullOrWhiteSpace(한글파일경로) ||
                클릭필드 != "보기" || 
                gdview.GetRowCellDisplayText(클릭Row, "문단").ToString() == ""  ) {
                return;
            }
            string 구분 = "일부분보기";
            담보쪼개기(클릭Row, 구분);

        }

        
    }


    /*
    public class 편집용지
    {
        public double 위쪽 { get; set; }
        public double 머리말 { get; set; }
        public double 왼쪽 { get; set; }
        public double 오른쪽 { get; set; }
        public double 꼬리말 { get; set; }
        public double 아래쪽 { get; set; }
    }
    public class 문단모양
    {
        public double 줄간격 { get; set; }
    }
    public class 글자모양
    {
        public double 기준크기 { get; set; }
        public string 글꼴 { get; set; }
        public int 장평 { get; set; }
    }
    public class 다단설정
    {
        public int 단개수 { get; set; }
        public double 간격 { get; set; }
        public int 적용범위1 { get; set; }
        public int 적용범위2 { get; set; }

    }
    public class 구역설정
    {
        public int 빈줄감추기 { get; set; }
    }

    */
}