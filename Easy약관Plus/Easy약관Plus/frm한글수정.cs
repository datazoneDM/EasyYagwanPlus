using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraGrid.Views.Grid;
using AxHWPCONTROLLib;
using HWPCONTROLLib;

namespace Easy약관Plus
{
    public partial class frm한글수정 : DevExpress.XtraEditors.XtraForm
    {

        public string[] 한글수정메인화면event = new string[10];
        public static event 한글수정메인화면_Handler 한글수정메인화면_SetMessage;
        public delegate void 한글수정메인화면_Handler(string[] 메인화면event);

        static string 개정한글파일경로 = "";
        static string 초안한글파일경로 = "";
        static string 개정TXT파일경로 = "";
        static string 초안TXT파일경로 = "";

        private string 확대여부 = "";

        public frm한글수정()
        {
            InitializeComponent();
        }


        private void frm한글수정_Load(object sender, EventArgs e)
        {
            frmEasy약관.한글수정_SetMessage += new frmEasy약관.한글수정_Handler(event_한글수정_setMessage);   //<== 메인화면에서 메세지 받음

            grd개정한글초기화(grd개정한글정보, view개정한글정보);
            grd개정한글초기화(grd초안한글정보, view초안한글정보);
        }

        private void event_한글수정_setMessage(string[] 전달인수)
        {
            switch (전달인수[0])
            {
                case "편집HWP경로":
                    편집Hwp파일읽기(전달인수[1], 전달인수[2]);
                    break;
                case "한글정보저장":
                    개정한글파일경로 = 전달인수[1];
                    초안한글파일경로 = 전달인수[2];
                    한글정보저장하기(개정한글파일경로, view개정한글정보);
                    한글정보저장하기(초안한글파일경로, view초안한글정보);
                    MessageBox.Show("한글수정 정보가 저장 되었습니다.", "저장완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "한글정보비교":
                    한글정보비교하기();
                    break;
                case "한글화면크기":
                    확대여부 = 전달인수[1];
                    frm한글수정_Resize(null, null);
                    break;
                case "한글수정":
                    string 편집hwp경로 = 전달인수[1];
                    한글수정표시하기(편집hwp경로);
                    break;
                case "스타일찾기":
                    string 스타일구분 = 전달인수[1];
                    string 스타일명 = 전달인수[2];
                    string 한글경로 = 전달인수[3];
                    스타일찾기(스타일구분, 스타일명, 한글경로);
                    break;

            }
        }

        private void 스타일찾기(string 스타일구분, string 스타일명, string 한글경로)
        {
            // 개정스타일 : grd개정한글정보, AX개정
            // 편집스타일 : grd초안한글정보, AX편집
            
            AxHwpCtrl Ax한글 = new AxHwpCtrl();
            GridView gdView = new GridView();
            DevExpress.XtraGrid.GridControl grid = new DevExpress.XtraGrid.GridControl();

            if (스타일구분 == "개정스타일") {
                //Ax한글 = AX개정;
                grid = grd개정한글정보;
                gdView = view개정한글정보;
            } else if (스타일구분 == "편집스타일") {
                //Ax한글 = AX편집;
                grid = grd초안한글정보;
                gdView = view초안한글정보;
            } else {
                return;
            }

            if (gdView.RowCount <= 0) {
                return;
            }

            Ax한글.CreateControl();
            Ax한글.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            Ax한글.Clear(1);
            Ax한글.Open(한글경로);
            Ax한글.MovePos(2, 0, 0); // 문서의 시작으로 이동

            HwpAction hwpact;
            HwpParameterSet textFind;
            
            List<int> tmp문단 = new List<int>();
            int 찾기중복 = 0;

            do {
                hwpact = (HwpAction)Ax한글.CreateAction("RepeatFind");  
                textFind = (HwpParameterSet)hwpact.CreateSet();
                textFind.SetItem("Direction", 0);              // 찾을 방향 : 0 = 아래쪽, 1 = 위쪽, 2 = 문서 전체
                textFind.SetItem("FindStyle", 스타일명);       // *** ※ 스타일 적용해서 제목을 찾음 - 중간에 제목이 2번이상 나올때가 존재함 ***
                textFind.SetItem("IgnoreMessage", 1);          // 메시지 무시 여부
                textFind.SetItem("FindType", 1);               // 다시 찾기를 할 때 마지막으로 실행한 [찾기 TRUE]를 할 것인가
                hwpact.Execute(textFind);

                int CtrlID = -1, 문단 = -1, 컬럼 = -1;
                // 캐럿의 위치를 얻어온다.
                Ax한글.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                
                if (tmp문단.IndexOf(문단) == -1) {
                    tmp문단.Add(문단);
                } else {
                    찾기중복++;
                }

                Ax한글.MovePos(1, 문단, 0);
                Ax한글.MovePos(7, 0, 0);

            } while (찾기중복 <= 2);

            Ax한글.Dispose();
            tmp문단.Sort();

            DataTable 개정초안한글 = (DataTable)grid.DataSource;
            DataView dv1 = new DataView(개정초안한글);
            dv1.Sort = "위치정보 ASC";
            개정초안한글 = dv1.ToTable();


            for ( int i = 0; i < dv1.Count; i++) {
                int 개정문단 = Convert.ToInt32(개정초안한글.Rows[i]["문단"]);
                if (tmp문단.IndexOf(개정문단) != -1) {
                    개정초안한글.Rows[i]["스타일"] = "해당";
                }
            }

            grid.DataSource = 개정초안한글;
            MessageBox.Show("스타일찾기가 완료 되었습니다.", "스타일찾기(datazone천재소년)");


        }
        private void 한글수정표시하기(string 편집hwp경로)
        {
            GridView gridView = view개정한글정보;
            int 개정한글전체cnt = view개정한글정보.RowCount - 1;

            편집hwp경로 = 편집hwp경로.Replace(".hwp", "_수정.hwp");

            HwpAction hwpact;
            HwpParameterSet inText;
            hwpact = (HwpAction)AX편집.CreateAction("InsertText");  // 텍스트 추가
            inText = (HwpParameterSet)hwpact.CreateSet();

            int 중간저장 = -1;
            for (int i = 개정한글전체cnt; i >= 0; i--) {

                bool 선택여부 = (bool)gridView.GetRowCellValue(i, "일치");

                if (선택여부) {
                    continue;
                }

                //string[] 수정위치정보분리 = gridView.GetRowCellValue(i, "위치정보").ToString().Split('-');
                int 시작문단 = Convert.ToInt32(gridView.GetRowCellValue(i, "문단").ToString());   //Convert.ToInt32(수정위치정보분리[1]);

                int CtrlID = -1, 문단 = -1, 컬럼 = -1;
                AX개정.MovePos(1, 시작문단, 0); // 현재 리스트의 특정 위치.(para pos로 위치 지정)
                AX개정.MovePos(7, 0, 0);        // 현재 위치한 문단의 끝으로 이동
                AX개정.GetPos(ref CtrlID, ref 문단, ref 컬럼);

                Clipboard.Clear();
                bool 설정1 = AX개정.SelectText(시작문단, 0, 시작문단, 컬럼);
                AX개정.Run("Copy");
                string 내용1 = Clipboard.GetText();

                int 바로위Idx = -1;
                for (int j = i - 1; j >= 0; j--) {
                    bool 선택여부2 = (bool)gridView.GetRowCellValue(j, "일치");
                    if (선택여부2) {
                        바로위Idx = j;
                        break;
                    }
                }

                string 수정일련번호 = string.Format("*********수정[{0:0000}]*********", gridView.GetRowCellValue(i, "일련번호").ToString());

                string[] 일치위치정보분리 = gridView.GetRowCellValue(바로위Idx, "일치위치정보").ToString().Split('-');
                int 이동문단 = Convert.ToInt32(일치위치정보분리[1]);

                AX편집.MovePos(1, 이동문단, 0); // 현재 리스트의 특정 위치.(para pos로 위치 지정)
                AX편집.MovePos(7, 0, 0);        // 현재 위치한 문단의 끝으로 이동
                AX편집.Run("BreakPara");        // Enter(빈줄삽입)
                AX편집.Run("BreakPara");
                inText.SetItem("Text", 수정일련번호);
                hwpact.Execute(inText);
                AX편집.Run("BreakPara");
                AX편집.Run("BreakPara");

                AX편집.Run("Paste");
                AX편집.Run("BreakPara");
                AX편집.Run("BreakPara");

                Clipboard.Clear();
                중간저장++;
                if (중간저장 >= 20)
                {
                    AX편집.SaveAs(편집hwp경로);
                    중간저장 = 0;
                }

            }
            MessageBox.Show("수정을 완료 했습니다.\n편집작업을 하세요.", "수정완료(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 한글정보비교하기()
        {
            GridView grdview01 = view개정한글정보;
            GridView grdview02 = view초안한글정보;

            int 개정전체cnt = grdview01.RowCount;
            int 초안전체cnt = grdview02.RowCount;

            if (!(개정전체cnt > 0) && !(초안전체cnt > 0)) {
                MessageBox.Show("개정한글리스트 또는 초안한글리스트가 존재하지 않습니다.", "리스트오류(천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable 초안한글 = (DataTable)grd초안한글정보.DataSource;
            DataView dv02 = new DataView(초안한글);
            dv02.Sort = "위치정보 ASC";
            초안한글 = dv02.ToTable();

            // 반드시 먼저 개정한글에 담보명에 스타일이 적용 되어 있어야 한다.
            // 스타일적용 + 문구 일치 = 절대위치로 범위를 정하는 역활을 한다.

            List<int> tmp개정스타일 = new List<int>();
            int 이전일치일련번호 = 0;
            
            int 현재인덱스 = -1;
            for (int i = 0; i < 개정전체cnt; i++) {
                //if ( i == 2768)
                //{
                //    System.Diagnostics.Debugger.Break();
                //}
                if (grdview01.GetRowCellDisplayText(i, "스타일") == "해당") {

                    tmp개정스타일.Add(i);
                    string tmp문구 = grdview01.GetRowCellDisplayText(i, "내용").Trim();

                    DataRow[] 초안스타일 = 초안한글.Select(string.Format("스타일 = '해당' and 내용 = '{0}'", tmp문구), "일련번호");
                    
                    if (초안스타일.Length > 0) {

                        string tmp라벨 = 초안스타일.Length.ToString();
                        grdview01.SetRowCellValue(i, "일치", true);
                        grdview01.SetRowCellValue(i, "레벨", tmp라벨);

                        현재인덱스 = 0;
                        int 일치일련번호 = 0;
                        string 일치위치정보 = "";

                        if (초안스타일.Length >= 2) {

                            // 바로이전에 있는 일치일련번호를 가져온다.
                            int 이전인수 = -1;
                            for (int j = i - 1; j >= 0; j--) {
                                if ((bool)grdview01.GetRowCellValue(j, "일치") == true && 
                                    grdview01.GetRowCellValue(j, "스타일").ToString() == "해당") {
                                    이전인수 = j;
                                    break;
                                }
                            }

                            이전일치일련번호 = Convert.ToInt32(grdview01.GetRowCellValue(이전인수, "일치일련번호"));
                            int 차이일련번호 = 99999999;
                            // 초안스타일값이 여러개 일 경우 이전 일치일련번호와 가장 가까운 일련번호를 선택한다.
                            for (int n = 0; n < 초안스타일.Length; n++)  {

                                일치일련번호 = Convert.ToInt32(초안스타일[n]["일련번호"]);
                                if (일치일련번호 >= 이전일치일련번호)
                                {
                                    int 일련번호차이 = 일치일련번호 - 이전일치일련번호;
                                    if (일련번호차이 < 차이일련번호) {

                                        차이일련번호 = 일련번호차이;
                                        현재인덱스 = n;
                                    }
                                }
                            }
                        }

                        일치일련번호 = Convert.ToInt32(초안스타일[현재인덱스]["일련번호"]);
                        일치위치정보 = 초안스타일[현재인덱스]["위치정보"].ToString();

                        grdview01.SetRowCellValue(i, "일치일련번호", string.Format("{0:0000}",일치일련번호));
                        grdview01.SetRowCellValue(i, "일치위치정보", 일치위치정보);

                        int 초안인수 = 일치일련번호 - 1;
                        일치일련번호 = Convert.ToInt32(grdview01.GetRowCellValue(i, "일련번호"));
                        일치위치정보 = grdview01.GetRowCellValue(i, "위치정보").ToString();
                        grdview02.SetRowCellValue(초안인수, "일치", true);
                        grdview02.SetRowCellValue(초안인수, "일치일련번호", string.Format("{0:0000}", 일치일련번호));
                        grdview02.SetRowCellValue(초안인수, "일치위치정보", 일치위치정보);
                        grdview02.SetRowCellValue(초안인수, "레벨", tmp라벨);
                    }
                }
            }

            DataTable 개정한글 = (DataTable)grd개정한글정보.DataSource;
            DataView dv01 = new DataView(개정한글);
            dv01.Sort = "위치정보 ASC";
            개정한글 = dv01.ToTable();

            초안한글 = (DataTable)grd초안한글정보.DataSource;
            dv02 = new DataView(초안한글);
            dv02.Sort = "위치정보 ASC";
            초안한글 = dv02.ToTable();

            int arrSize = 15;
            int 제한cnt = arrSize / 2;
            DataTable 서브개정한글 = 서브개정한글datatable();
            
            int 담보시작지점 = 30; //쪽수


            int 개정시작Row = -1, 개정종료Row = -1;
            int 초안시작Row = -1, 초안종료Row = -1;

            List<string> 이전확인 = new List<string>();

            for (int Pcnt = 0; Pcnt <= tmp개정스타일.Count; Pcnt++) {

                //if (Pcnt == 77)
                //{
                //    System.Diagnostics.Debugger.Break();
                //}

                if (Pcnt == tmp개정스타일.Count) {
                    개정종료Row = 개정전체cnt;
                    초안종료Row = 초안전체cnt;
                    
                } else {

                    string[] 위치정보분리 = grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt], "위치정보").Split('-');
                    int 현재쪽수 = Convert.ToInt32(위치정보분리[3]); 

                    if (grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt], "스타일") == "해당" &&
                        grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt], "레벨") == "1") {

                        개정종료Row = tmp개정스타일[Pcnt];
                        초안종료Row = Convert.ToInt32(grdview01.GetRowCellValue(개정종료Row, "일치일련번호")) - 1;

                        if (초안시작Row == -99) {

                            // 이전 스타일 해당 조건이 false로 해당 Row를 알 수 가 없어 초안종료Row를 기준으로 이전 Row를 찾는다.
                            // 2) 담보단위로 내용을 비교한다.
                            for (int r = 초안종료Row - 1; r >= 0; r--) {
                                if (grdview02.GetRowCellDisplayText(r, "스타일") == "해당") {
                                    초안시작Row = r;
                                    break;
                                }
                            }
                        }
                        
                    } else if ((string)grdview01.GetRowCellValue(tmp개정스타일[Pcnt], "스타일") == "해당" &&
                                현재쪽수 >= 담보시작지점 ) {

                        if ( ((bool)grdview01.GetRowCellValue(tmp개정스타일[Pcnt - 1], "일치") == true  &&  
                              grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt - 1], "레벨") != "" &&
                              grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt - 2], "레벨") == "1" )  ||
                              grdview01.GetRowCellDisplayText(tmp개정스타일[Pcnt - 1], "레벨") == "1") {  

                            // 담보명에 Enter값이 들어가 있어 여러개로 나누어 지는 경우 

                            개정종료Row = tmp개정스타일[Pcnt];

                            초안시작Row = Convert.ToInt32(grdview01.GetRowCellValue(tmp개정스타일[Pcnt - 1], "일치일련번호")) - 1;
                            // 초안시작Row++ 해서 다음 스타일을 찾는다????
                            for (int r = 초안시작Row + 1; r < 초안전체cnt; r++) {
                                if (grdview02.GetRowCellDisplayText(r, "스타일") == "해당") {
                                    초안종료Row = r;
                                    break;
                                }
                            }
                        } else {
                            개정시작Row = tmp개정스타일[Pcnt];  // 1) 담보단위로 내용을 비교한다.
                            초안시작Row = -99;
                            continue;
                        }
                    } else {
                        continue;
                    }
                }

                for (int i = 개정시작Row + 1; i < 개정종료Row; i++) {

                    string[] 개정내용 = new string[arrSize];
                    string[] 초안내용 = new string[arrSize];

                    개정내용[0] = 개정한글.Rows[i]["내용"].ToString();
                    if (string.IsNullOrWhiteSpace(개정내용[0])) {
                        continue;
                    }

                    int 개정cnt = 0;
                    int 개정Idx = -1;
                    int 다음Idx = i;
                    int 이전Idx = i;

                    do {
                        다음Idx++;
                        개정Idx += 2;   // 홀수인수는 아래쪽내용저장, 짝수(아래로직)는 윗쪽내용 저장한다.
                        개정cnt++;
                        if (다음Idx >= 개정종료Row) {
                            개정내용[개정Idx] = "";   //개정내용[개정Idx - 2 < 0 ? 0  : 개정Idx - 2]; // 공백을 삽입하고  Count에서는 제외한다.(일치레벨제외)
                            break;
                        } else {
                            개정내용[개정Idx] = 개정한글.Rows[다음Idx]["내용"].ToString();
                        }
                    } while (개정cnt < 제한cnt);

                    개정cnt = 0;
                    개정Idx = 0;

                    do {
                        이전Idx--;
                        개정Idx += 2;
                        개정cnt++;
                        if (이전Idx <= 개정시작Row) {
                            개정내용[개정Idx] = "";  //개정내용[개정Idx - 2 < 0 ? 0 : 개정Idx - 2];
                            break;
                        } else {
                            개정내용[개정Idx] = 개정한글.Rows[이전Idx]["내용"].ToString();
                        }
                    } while (개정cnt < 제한cnt);



                    int 초안일치cnt = 0;
                    int 비교일치횟수 = -1;
                    int 인수 = -1;

                    for (int j = 초안시작Row + 1; j < 초안종료Row; j++) {

                        초안내용 = new string[arrSize];
                        초안내용[0] = 초안한글.Rows[j]["내용"].ToString();
                        if (string.IsNullOrWhiteSpace(초안내용[0]) || 개정내용[0].Replace(" ", "") != 초안내용[0].Replace(" ", "")) {
                            continue;
                        }

                        int 초안cnt = 0;
                        int 초안Idx = -1;
                        int 초안다음Idx = j;
                        int 초안이전Idx = j;
                        int 일치횟수 = 0;

                        do {
                            초안다음Idx++;
                            초안Idx += 2;
                            초안cnt++;

                            if (초안다음Idx >= 초안종료Row) {
                                초안내용[초안Idx] = "";  // 초안내용[초안Idx - 2 < 0 ? 0 : 초안Idx - 2]; //공백을 삽입하고  Count에서는 제외한다.(일치레벨제외)
                                break;
                            } else {
                                초안내용[초안Idx] = 초안한글.Rows[초안다음Idx]["내용"].ToString();
                            }
                        } while (초안cnt < 제한cnt);

                        초안cnt = 0;
                        초안Idx = 0;

                        do {
                            초안이전Idx--;
                            초안Idx += 2;
                            초안cnt++;

                            if (초안이전Idx <= 초안시작Row) {
                                초안내용[초안Idx] = "";   // 초안내용[초안Idx - 2 < 0 ? 0 : 초안Idx - 2];
                                break;
                            } else {
                                초안내용[초안Idx] = 초안한글.Rows[초안이전Idx]["내용"].ToString();
                            }
                        } while (초안cnt < 제한cnt);


                        //개정,초안내용이 얼마나 일치하는 검사한다.
                        //기준값(0)을 기준으로 이전,이후값을 차례로 비교한다.
                        for (int b = 1; b < arrSize; b += 1) {
                            if (!string.IsNullOrWhiteSpace(개정내용[b]) && !string.IsNullOrWhiteSpace(초안내용[b])) {
                                if (개정내용[b].Replace(" ", "") == 초안내용[b].Replace(" ", "")) {
                                    일치횟수++;
                                }
                            }
                        }

                        if (일치횟수 > 비교일치횟수) {
                            비교일치횟수 = 일치횟수;
                            인수 = j;
                        }
                        // 비교이전문단 : 이전문단보다 크면서


                        초안일치cnt++;
                        string[] 개정분리 = 초안한글.Rows[j]["위치정보"].ToString().Split('-');

                        서브개정한글.Rows.Add(new object[] {
                            개정한글.Rows[i]["일련번호"],
                            초안한글.Rows[j]["일련번호"],        // 초안일련번호
                            string.Format("{0:00}",초안일치cnt), // 순번
                            개정분리[0],                         // CtrlID
                            개정분리[1],                         // 문단
                            개정분리[2],                         // 컬럼
                            개정분리[3],                         // 쪽
                            개정분리[4],                         // 단
                            개정분리[5],                         // 줄
                            개정분리[6],                         // 칸       
                            일치횟수                             // 일치레벨
                         });


                    }  // j : end

                    if (초안일치cnt >= 1) {

                        개정한글.Rows[i]["일치"] = true;
                        개정한글.Rows[i]["일치일련번호"] = 초안한글.Rows[인수]["일련번호"].ToString();
                        개정한글.Rows[i]["일치위치정보"] = 초안한글.Rows[인수]["위치정보"].ToString();
                        개정한글.Rows[i]["레벨"] = string.Format("{0:00}({1:000})", 비교일치횟수, 초안일치cnt);


                        초안한글.Rows[인수]["일치"] = true;
                        초안한글.Rows[인수]["일치일련번호"] = 개정한글.Rows[i]["일련번호"].ToString();
                        초안한글.Rows[인수]["일치위치정보"] = 개정한글.Rows[i]["위치정보"].ToString();
                    }

                    한글수정메인화면event[0] = "한글정보비교하기";              // 진행상황에 값을 전달한다.
                    한글수정메인화면event[1] = (Convert.ToInt32((i + 1) / (double)개정전체cnt * 100)).ToString();
                    한글수정메인화면_SetMessage.Invoke(한글수정메인화면event);
                    Application.DoEvents();
                }  // i : end


                개정시작Row = 개정종료Row;
                초안시작Row = 초안종료Row;


            } // Pcnt : end

            DataView dv03 = new DataView(서브개정한글);
            dv03.Sort = "일치레벨 DESC";
            서브개정한글 = dv03.ToTable();

            DataSet DS개정한글 = new DataSet();
            DS개정한글.Tables.Add(개정한글);
            DS개정한글.Tables.Add(서브개정한글);

            DataColumn primaryKeyCol = DS개정한글.Tables[0].Columns["일련번호"];
            DataColumn foreignKeyCol = DS개정한글.Tables[1].Columns["일련번호"];

            DataRelation dr한글수정 = DS개정한글.Relations.Add("초안한글View", primaryKeyCol, foreignKeyCol);
             
            grd개정한글정보.DataSource = DS개정한글.Tables[0];
            grd개정한글정보.ForceInitialize();

            GridView gdview = new GridView(grd개정한글정보);
            gdview.OptionsView.ShowGroupPanel = false;
            gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
            gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 9);
            gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
            gdview.Appearance.FocusedRow.BackColor = Color.Orange;

            grd개정한글정보.LevelTree.Nodes.Add("초안한글View", gdview);
            

            grd초안한글정보.DataSource = 초안한글;
            MessageBox.Show("한글정보비교가 완료 되었습니다.\n다음작업을 위해 저장하기를 하세요", "비교완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }


        private void 한글정보저장하기(string 저장경로 , GridView gdview)
        {
            if (string.IsNullOrWhiteSpace(저장경로) || gdview.RowCount == 0) {
                return;
            }

            string 출력 = "";
            if (gdview.Name == "view개정한글정보") {
                출력 = "###개정한글정보###";
            } else if (gdview.Name == "view초안한글정보") {
                출력 = "###초안한글정보###";
            }

            저장경로 = 저장경로.Replace(new System.IO.FileInfo(저장경로).Extension, ".TXT");
            using (System.IO.FileStream fs = new System.IO.FileStream(저장경로, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                using (System.IO.StreamWriter wr = new System.IO.StreamWriter(fs, Encoding.GetEncoding(949))) {

                    wr.WriteLine(출력);
                    wr.Flush();

                    for (int i = 0; i < gdview.RowCount; i++) {
                        출력 = "";
                        출력 += gdview.GetRowCellDisplayText(i, "일치").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "일련번호").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "위치정보").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "내용").ToString().Replace("\r\n", "</br>") + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "레벨").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "일치일련번호").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "일치위치정보").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "문단").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "스타일").ToString() + '|';
                        출력 += gdview.GetRowCellDisplayText(i, "이동").ToString();
                        wr.WriteLine(출력);
                        wr.Flush();
                    }
                }
            }
        }


        private void 편집Hwp파일읽기(string 편집한글경로, string 한글종류)
        {
            AxHwpCtrl axHwp = new AxHwpCtrl();

            if ( 한글종류 == "개정한글") {
                axHwp = AX개정;
            } else if ( 한글종류 == "편집한글") {
                axHwp = AX편집;
            } else {
                MessageBox.Show("한글종류 오류", "뭐지?(datazone천재소년)");
                return;
            }

            axHwp.CreateControl();
            axHwp.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            axHwp.Clear(1);
            axHwp.Open(편집한글경로);
            axHwp.MovePos(3, 0, 0);
        }


        // 개정한글
        // 초안한글 
        private void 한글위치정보(DevExpress.XtraGrid.GridControl grd, string 개정한글경로)
        {
            AxHwpCtrl AXHwp;
            AXHwp = new AxHwpCtrl();
            AXHwp.CreateControl();
            AXHwp.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
            AXHwp.Clear(1);
            AXHwp.Open(개정한글경로);
            AXHwp.MovePos(3, 0, 0);

            string 객체문구 = "";
            string grid명 = grd.Name;
            if (grid명 == "grd개정한글정보") {
                객체문구 = "분석중1";
                AX개정.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                AX개정.Clear(1);
                AX개정.Open(개정한글경로);
                AX개정.MovePos(2, 0, 0);

                HwpParameterSet 이동 = (HwpParameterSet)AX개정.ScrollPosInfo;
                이동.SetItem("VertPos", 100);
                AX개정.ScrollPosInfo = 이동;

            } else if (grd.Name == "grd초안한글정보") {
                객체문구 = "분석중2";
                AX편집.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                AX편집.Clear(1);
                AX편집.Open(개정한글경로);
                AX편집.MovePos(2, 0, 0);

                HwpParameterSet 이동 = (HwpParameterSet)AX편집.ScrollPosInfo;
                이동.SetItem("VertPos", 100);
                AX편집.ScrollPosInfo = 이동;
            }


            int CtrlID = -1;
            int 문단 = -1;
            int 컬럼 = -1;

            // 캐럿의 위치를 얻어온다.
            AXHwp.GetPos(ref CtrlID, ref 문단, ref 컬럼);

            int 마지막문단 = 문단;

            int 총구역 = -1, 현재구역 = -1;
            int 쪽 = -1, 단 = -1, 줄 = -1, 칸 = -1;
            short 수정 = -1;
            string ctrlname = "";

            
            DataTable table = new DataTable();
            table = 개정한글datatable();

            List<int> 빈문단 = new List<int>();
            double 처리cnt = 0;
            string 위치정보 = "";
            for (int i = 0; i <= 마지막문단; i++) {
                
                //------
                string 내용 = "";

                //0x00 : 본문을 대상으로 검색한다,  0x0010 : 특정 위치부터. (시작 위치) scanSposSpecified

                //강제로 위치를 변경하면 읽기를 못한다(?).
                //위치를 옮기면 다시 원 위치를 해야 한다.  ==> AXHwp.MovePos(3, 0, 0);
                //그래서 아래 다시 처리한다. 인덱스만 저장  
                bool 확인 = AXHwp.InitScan(0x00, 0x0010, i, 0, i + 1, 0);        // AXHwp.InitScan() 이렇게 하면 모든 text를 읽는다? 테스트 필요
                int result = AXHwp.GetText(ref 내용);
                //------
                if (string.IsNullOrWhiteSpace(내용)) {
                    빈문단.Add(i);
                    continue;
                } else {
                    table.Rows.Add(new object[] {
                        false,
                        "",                              // 일련번호 (아래에서 일괄로 처리)
                        string.Format("{0:00000}",i),    // 일단 문단정보를 저장후 아래에서 일관로 위치정보 저장
                        내용.Replace(" ","").Trim(),
                        "",                              // 레벨
                        "",                              // 일치일련번호 
                        "",                              // 일치위치정보
                        ""                               // 이동
                    });
                }

                처리cnt++;
                한글수정메인화면event[0] = 객체문구;              // 진행상황에 값을 전달한다.
                한글수정메인화면event[1] = (Convert.ToInt32(처리cnt / (double)(마지막문단 + 1) * 100)).ToString();
                한글수정메인화면_SetMessage.Invoke(한글수정메인화면event);

                Application.DoEvents();
            }

            AXHwp.ReleaseScan();  // 초기화

            int 현재빈문단 = -1;
            if (빈문단.Count > 0) {
                for (int i = 0; i < 빈문단.Count; i++)  {

                    현재빈문단 = 빈문단[i];
                    Clipboard.Clear();
                    bool 설정 = AXHwp.SelectText(현재빈문단, 0, 현재빈문단 + 1, 0);
                    AXHwp.Run("Copy");
                    string 내용 = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(내용.Trim())) {

                        table.Rows.Add(new object[] {
                            false,
                            "",                                       // 일련번호 (아래에서 일괄로 처리)
                            string.Format("{0:00000}",현재빈문단),    // 일단 문단정보를 저장후 아래에서 일관로 위치정보 저장
                            내용.Replace(" ","").Trim(),
                            "",                                       // 레벨
                            "",                                       // 일치일련번호
                            "",                                       // 일치위치정보
                            "",                                       // 문단
                            "",                                       // 스타일
                            ""                                        // 이동
                        });
                    }
                 

                    Application.DoEvents();
                    처리cnt++;
                    한글수정메인화면event[0] = 객체문구;              // 진행상황에 값을 전달한다.
                    한글수정메인화면event[1] = (Convert.ToInt32(처리cnt / (double)(마지막문단 + 1) * 100)).ToString();
                    한글수정메인화면_SetMessage.Invoke(한글수정메인화면event);
                }
            }

            DataView dv01 = new DataView(table);
            dv01.Sort = "위치정보 ASC";  // 위치정보값(즉 문단값)으로 정렬 후 다시 위치정보값을 입력 한다. 일련번호값도 일련번호로 다시 부여 한다.
            table = dv01.ToTable();

            int 전체문단cnt = table.Rows.Count;
            for ( int i = 0; i < 전체문단cnt; i++) {
                int tmp문단 = Convert.ToInt32(table.Rows[i]["위치정보"].ToString());
                AXHwp.MovePos(1, tmp문단, 0);
                AXHwp.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                AXHwp.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);
                위치정보 = string.Format("{0:0000}-{1:00000}-{2:0000}-{3:0000}-{4:0000}-{5:0000}-{6:0000}", CtrlID, 문단, 컬럼, 쪽, 단, 줄, 칸);

                table.Rows[i]["일련번호"] = string.Format("{0:0000}", i + 1);
                table.Rows[i]["위치정보"] = 위치정보;
                table.Rows[i]["문단"] = 문단;


            }
            grd.DataSource = table;
            AXHwp.Dispose();



        }
        
        private void grd개정한글정보_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void grd개정한글정보_DragDrop(object sender, DragEventArgs e)
        {
            grd개정한글초기화(grd개정한글정보, view개정한글정보);
            string[] 개정한글리스트 = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            string 파일종류 = new System.IO.FileInfo(개정한글리스트[0]).Extension.ToUpper();
            if (파일종류 == ".HWP") {
                개정한글파일경로 = 개정한글리스트[0];
                한글위치정보(grd개정한글정보, 개정한글파일경로);

            } else if (파일종류 == ".TXT") {
                개정TXT파일경로 = 개정한글리스트[0];
                한글정보TXT(개정TXT파일경로, "개정한글정보");
            }

            한글수정메인화면event[0] = "개정한글경로";
            한글수정메인화면event[1] = 개정한글리스트[0];
            한글수정메인화면event[2] = 파일종류;
            한글수정메인화면_SetMessage.Invoke(한글수정메인화면event);


        }

        private void grd초안한글정보_DragDrop(object sender, DragEventArgs e)
        {
            grd개정한글초기화(grd초안한글정보, view초안한글정보); //view한글초안정보
            string[] 초안한글리스트 = (string[])e.Data.GetData(DataFormats.FileDrop, false);

           
            string 파일종류 = new System.IO.FileInfo(초안한글리스트[0]).Extension.ToUpper();
            if (파일종류 == ".HWP") {
                초안한글파일경로 = 초안한글리스트[0];
                한글위치정보(grd초안한글정보, 초안한글파일경로);

            } else if (파일종류 == ".TXT") {
                초안TXT파일경로 = 초안한글리스트[0];
                한글정보TXT(초안TXT파일경로, "초안한글정보");
            }

            한글수정메인화면event[0] = "초안한글경로";
            한글수정메인화면event[1] = 초안한글리스트[0];
            한글수정메인화면event[2] = 파일종류;
            한글수정메인화면_SetMessage.Invoke(한글수정메인화면event);
        }

        private void grd초안한글정보_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void 한글정보TXT (string 한글수정정보경로, string grd이름)
        {
            string[] data = System.IO.File.ReadAllLines(한글수정정보경로, System.Text.Encoding.GetEncoding(949));
            if (data.Length <= 0 ) {
                MessageBox.Show("리스트가 올바르지 않습니다.", "리스트오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DataTable table = new DataTable();
            table = 개정한글datatable();

            if ( !(grd이름 == "개정한글정보" && data[0] == "###개정한글정보###") &&
                 !(grd이름 == "초안한글정보" && data[0] == "###초안한글정보###") )  {
                MessageBox.Show(grd이름 + " 테이블에 \n" + data[0] + " 파일을 올렸습니다.", "리스트오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            for ( int i = 1; i < data.Length; i++) {
                string[] 분리 = data[i].Split('|');

                table.Rows.Add(new object[]
                    {
                        (분리[0] == "Unchecked") ? false : true,
                        분리[1],
                        분리[2],
                        분리[3].Replace("</br>","\r\n"),
                        분리[4],
                        분리[5],
                        분리[6],
                        분리[7],
                        분리[8],
                        ""
                    });
            }

            if (grd이름 == "개정한글정보") {
                grd개정한글정보.DataSource = table;
            } else if (grd이름 == "초안한글정보") {
                grd초안한글정보.DataSource = table;
            }

        }



        private void grd개정한글초기화(DevExpress.XtraGrid.GridControl grd, GridView gdview)
        {
            string 타이틀문구 = "";
            string grid명 = grd.Name;
            if (grid명 == "grd개정한글정보") {
                타이틀문구 = "개정한글 파일(Hwp)을 올려주세요.(Ctrl-ID, 문단, 컬럼, 쪽, 단, 줄, 칸)";
            }
            else {
                타이틀문구 = "초안(편집용)한글 파일(Hwp)을 올려주세요.(Ctrl-ID, 문단, 컬럼, 쪽, 단, 줄, 칸)";
            }

            gdview.GroupPanelText = 타이틀문구;

            gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.GroupPanel.ForeColor = Color.DarkBlue;
            gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
            gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
            gdview.Appearance.FocusedRow.BackColor = Color.Orange;

            DataTable table = new DataTable();
            table = 개정한글datatable();

            gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
            //gdview.OptionsBehavior.Editable = false;

            grd.DataSource = table;

            int grdwidth = grd.Width;
            gdview.Columns["일치"].Width = Convert.ToInt32(grdwidth * 0.05);
            gdview.Columns["일련번호"].Width = Convert.ToInt32(grdwidth * 0.1);
            gdview.Columns["위치정보"].Width = Convert.ToInt32(grdwidth * 0.2);
            gdview.Columns["내용"].Width = Convert.ToInt32(grdwidth * 0.5);

            if (grd.Name == "grd개정한글정보") {
                gdview.Columns["레벨"].Width = Convert.ToInt32(grdwidth * 0.1);
            } else {
                gdview.Columns["레벨"].Visible = false;

                for (int i = 0; i < gdview.Columns.Count; i++) {
                    gdview.Columns[i].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;   // sort 막기 
                }

            }
            gdview.Columns["일치일련번호"].Width = Convert.ToInt32(grdwidth * 0.001);
            gdview.Columns["일치위치정보"].Width = Convert.ToInt32(grdwidth * 0.001);
            gdview.Columns["이동"].Width = Convert.ToInt32(grdwidth * 0.05);

            gdview.Columns["일련번호"].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            gdview.Columns["문단"].Visible = false;
            gdview.Columns["스타일"].Visible = false;

            DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit buttonEdit = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            buttonEdit.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            buttonEdit.Buttons[0].ImageOptions.Image = Image.FromFile(@"c:\Easy약관Plus\program\img\이동4.png");
            buttonEdit.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            gdview.Columns["이동"].ColumnEdit = buttonEdit;
            gdview.Columns["이동"].OptionsColumn.AllowEdit = false;


        }

        private DataTable 개정한글datatable()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("일치", typeof(bool)));
            table.Columns.Add(new DataColumn("일련번호", typeof(string)));
            table.Columns.Add(new DataColumn("위치정보", typeof(string)));
            table.Columns.Add(new DataColumn("내용", typeof(string)));
            table.Columns.Add(new DataColumn("레벨", typeof(string)));
            table.Columns.Add(new DataColumn("일치일련번호", typeof(string)));
            table.Columns.Add(new DataColumn("일치위치정보", typeof(string)));
            table.Columns.Add(new DataColumn("문단", typeof(string)));
            table.Columns.Add(new DataColumn("스타일", typeof(string)));
            table.Columns.Add(new DataColumn("이동", typeof(string)));
            return table;
        }

        private DataTable 서브개정한글datatable()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("일련번호", typeof(string)));
            table.Columns.Add(new DataColumn("초안번호", typeof(string)));
            table.Columns.Add(new DataColumn("순번", typeof(string)));
            table.Columns.Add(new DataColumn("CtrlID", typeof(string)));
            table.Columns.Add(new DataColumn("문단", typeof(string)));
            table.Columns.Add(new DataColumn("컬럼", typeof(string)));
            table.Columns.Add(new DataColumn("쪽", typeof(string)));
            table.Columns.Add(new DataColumn("단", typeof(string)));
            table.Columns.Add(new DataColumn("줄", typeof(string)));
            table.Columns.Add(new DataColumn("칸", typeof(string)));
            table.Columns.Add(new DataColumn("일치레벨", typeof(int)));

            return table;
        }

        private void frm한글수정_Resize(object sender, EventArgs e)
        {
            //group한글정보
            int 표시영역 = this.Size.Width;

            int 왼쪽영역 = 0;
            int 오른쪽영역 = 0;

            if (확대여부 == "확대")
            {
                grd초안한글정보.Visible = false;
                왼쪽영역 = Convert.ToInt32((double)표시영역 * 0.31);
                오른쪽영역 = Convert.ToInt32((double)표시영역 * 0.69) - 10;
                
            } else {
                grd초안한글정보.Visible = true;
                왼쪽영역 = Convert.ToInt32((double)표시영역 * 0.6);
                오른쪽영역 = Convert.ToInt32((double)표시영역 * 0.4) - 10;
            }

            group한글정보.Width = 왼쪽영역;
            group편집본.Width = 오른쪽영역;

            group편집본.Location = new Point(왼쪽영역 + 6, 2);

            int grd크기 = 왼쪽영역 / 2;
            if (확대여부 == "확대")
            {
                grd개정한글정보.Width = 왼쪽영역 - 8;
                grd초안한글정보.Width = 0;
            } else {
                //grd개정한글정보
                //grd초안한글정보
                grd개정한글정보.Width = grd크기 - 8;
                grd초안한글정보.Width = grd크기 - 8;
            }

            

            grd초안한글정보.Location = new Point(grd크기 + 3, 27);

            //group편집본
            int Hwp크기 = 오른쪽영역 / 2;
            AX개정.Width = Hwp크기 - 8;
            AX편집.Width = Hwp크기 - 8;

            AX편집.Location = new Point(AX개정.Width + 8, 27);

        }

        private void frm한글수정_FormClosed(object sender, FormClosedEventArgs e)
        {
            frmEasy약관.한글수정_SetMessage -= new frmEasy약관.한글수정_Handler(event_한글수정_setMessage);  
        }

        private void view개정한글정보_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle >= 0 && (bool)view개정한글정보.GetRowCellValue(e.RowHandle, "일치") == false) {
                e.Appearance.ForeColor = Color.White;
            } else {
                e.Appearance.ForeColor = Color.Yellow;
            }

            if ( e.RowHandle >=0 && view개정한글정보.GetRowCellDisplayText(e.RowHandle, "스타일") == "해당") {
                e.Appearance.BackColor = Color.Gray;
            }
        }

       
        private void view초안한글정보_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle >= 0 && (bool)view초안한글정보.GetRowCellValue(e.RowHandle, "일치") == false)  {
                e.Appearance.ForeColor = Color.White;
            } else {
                e.Appearance.ForeColor = Color.Yellow;
            }

            if (e.RowHandle >= 0 && view초안한글정보.GetRowCellDisplayText(e.RowHandle, "스타일") == "해당") {
                e.Appearance.BackColor = Color.Gray;
            }

        }

        private void view개정한글정보_Click(object sender, EventArgs e)
        {
            GridView gdview = view개정한글정보;

            string 클릭필드 = gdview.FocusedColumn.FieldName;
            int 클릭Row = gdview.FocusedRowHandle;            // 타이틀을 클릭해도 0값이 반환된다.

            if (gdview.RowCount == 0 || 클릭필드 != "이동" ||
                gdview.GetRowCellDisplayText(클릭Row, "일치일련번호").ToString() == "") {
                return;
            }
            int 이동라인 = Convert.ToInt32(gdview.GetRowCellDisplayText(클릭Row, "일치일련번호").ToString());

            view초안한글정보Row선택(이동라인, 클릭Row);
        }
        private void view초안한글정보Row선택(int 이동라인, int 클릭Row)
        {
            GridView gdview = view초안한글정보;

            if (이동라인 > view초안한글정보.RowCount) {
                return;
            }

            view초안한글정보.FocusedRowHandle = 이동라인 - 1;
            view초안한글정보.Appearance.FocusedRow.BackColor = Color.Red;


            int CtrlID = -1;
            int 컬럼 = -1;

            if (!string.IsNullOrWhiteSpace(AX개정.Path))
            {
                string[] 위치정보분리 = view개정한글정보.GetRowCellValue(클릭Row, "위치정보").ToString().Split('-');
                int 문단 = Convert.ToInt32(위치정보분리[1]);

                AX개정.MovePos(1, 문단, 0);
                AX개정.MovePos(7, 0, 0);
                AX개정.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                AX개정.SelectText(문단, 0, 문단, 컬럼);

                HwpParameterSet 이동 = (HwpParameterSet)AX개정.ScrollPosInfo;
                이동.SetItem("VertPos", 100);
                AX개정.ScrollPosInfo = 이동;

            }

            if (!string.IsNullOrWhiteSpace(AX편집.Path))
            {
                string[] 위치정보분리 = view개정한글정보.GetRowCellValue(클릭Row, "일치위치정보").ToString().Split('-');
                int 문단 = Convert.ToInt32(위치정보분리[1]);
                              

                AX편집.MovePos(1, 문단, 0);
                AX편집.MovePos(7, 0, 0);
                AX편집.GetPos(ref CtrlID, ref 문단, ref 컬럼);
                AX편집.SelectText(문단, 0, 문단, 컬럼);

                HwpParameterSet 이동 = (HwpParameterSet)AX편집.ScrollPosInfo;
                이동.SetItem("VertPos", 100);
                AX편집.ScrollPosInfo = 이동;

            }

        }

       
    }
}