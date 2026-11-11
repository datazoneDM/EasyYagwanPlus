using AxHWPCONTROLLib;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Easy약관Plus
{
    public partial class frm담보관리 : DevExpress.XtraEditors.XtraForm
    {
        public string[] 담보관리메인화면event = new string[10];
        public static event 담보관리메인화면_Handler 담보관리메인화면_SetMessage;
        public delegate void 담보관리메인화면_Handler(string[] 메인화면event);

        private string[] 담보파일 = null;
        private string[] 담보리스트 = null;
        private Form mainform = new Form();
        private string 약관형태 = "";
        private string 약관DB => 저장.약관DB;

        public frm담보관리(string _약관형태)
        {
            InitializeComponent();
            약관형태 = _약관형태;
        }


        private async void frm담보관리_Load(object sender, EventArgs e)
        {
            string 설정결과 = await NetworkManager.공통설정파일_로드();

            if (설정결과 == "실패" || string.IsNullOrWhiteSpace(this.약관DB))
            {
                MessageBox.Show("DB 접속 정보(localDB.txt)를 불러오지 못했습니다.\n프로그램을 다시 실행하거나 관리자에게 문의하세요.",
                                "초기화 치명적 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.Enabled = false; // 폼 전체 비활성화 (버튼 클릭 방지)
                return;
            }

            frmEasy약관.담보관리_SetMessage += new frmEasy약관.담보관리_Handler(event_담보관리_setMessage);   //<== 메인화면에서 메세지 받음
            진행바초기화();
            mainform = this;

            if (약관형태 == "분리담보")
                SelectedTabPage초기화(담보약관);
            else if (약관형태 == "전체약관")
                SelectedTabPage초기화(전체약관);

        }

        private int 선택전체갯수 = 0;
        private int 완료cnt;
        private const int 최대스레드cnt = 6;
        private string 담보미리보기 = "";
        private AxHwpCtrl[] Ax02 = new AxHwpCtrl[최대스레드cnt];
        private int[] AxHwpCtrl확인 = new int[최대스레드cnt];
        public async void event_담보관리_setMessage(string[] 전달인수)
        {
            담보미리보기 = "";
            string 테이블명 = "";
            switch (전달인수[0])
            {
                case "초기화":
                    진행바초기화();
                    SelectedTabPage초기화(전달인수[1] == "분리담보" ? 담보약관 : 전체약관);
                    break;
                case "담보비교":
                    약관형태 = 전달인수[1];
                    string 결과 = 담보파일_담보리스트_비교(약관형태);
                    if (결과 == "오류")
                    {
                        MessageBox.Show("담보리스트 비교에서 오류가 발생하였습니다", "유효성오류",MessageBoxButtons.OK);
                        return;
                    }
                    담보관리메인화면event[0] = "버튼활성화";
                    담보관리메인화면event[1] = 약관형태 == "분리담보" ? "encode" : "담보upload";      // barEncode 아이콘을 활성화 한다.
                    담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);
                    break;
                case "Encode":
                    선택전체갯수 = 담보전체갯수();
                    if (선택전체갯수 <=0) {
                        MessageBox.Show("변환하고자 하는 담보가 없습니다.", "담보선택오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    완료cnt = 0;
                    txt전체담보갯수.EditValue = 선택전체갯수;
                    await Encode실행();

                    담보관리메인화면event[0] = "버튼활성화";
                    담보관리메인화면event[1] = "담보upload"; 
                    담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);
                    break;
                case "담보upload":

                    약관형태 = 전달인수[1];

                    if(약관형태 == "분리담보")
                    {
                        string tmp개정일 = view담보리스트.GetRowCellDisplayText(0, "개정일").ToString();
                        string tmp테이블인수 = "";

                        if (tmp개정일 != "" && tmp개정일.Length >= 2)
                        {

                            int 현재년도 = Convert.ToInt32(string.Format("{0:yy}", DateTime.Now));
                            int 개정년도 = Convert.ToInt32(tmp개정일.Substring(0, 2));

                            if (개정년도 != 0 && 개정년도 < 현재년도)
                            {
                                tmp테이블인수 = "_" + tmp개정일.Substring(0, 2);
                            }
                            // 개정년도가 현재년도 보다 작으면 예전 Tabel을 가져온다 ex)담보MZ_22
                            // 아니면 기본테이블                                     ex)담보MZ

                        }

                        테이블명 = string.Format("담보{0}{1}", view담보리스트.GetRowCellDisplayText(0, "고객사코드").ToString(), tmp테이블인수);
                        await 담보파일_담보upload(테이블명);

                    } else if(약관형태 == "전체약관")
                    {
                        담보파일_약관upload();
                    }

                    break;                
                case "담보검색":
                    
                    담보미리보기 = "담보미리보기";
                    List<string> Tmp조건 = new List<string> { $"상품코드 = '{전달인수[2]}'" };

                    if (!string.IsNullOrEmpty(전달인수[3])) Tmp조건.Add($"개정일 = '{전달인수[3]}'");
                    if (!string.IsNullOrEmpty(전달인수[4])) Tmp조건.Add($"담보분류 = '{전달인수[4]}'");
                    if (!string.IsNullOrEmpty(전달인수[5])) Tmp조건.Add($"담보코드 = '{전달인수[5]}'");

                    담보코드_검색(전달인수[1], 전달인수[6], Tmp조건, 전달인수[7]); //고객사코드, 약관형태, Tmp조건, 테이블명

                    break;
                case "담보update":

                    //전달인수[1] : 고객사코드
                    //전달인수[2] : 테이블명
                    //전달인수[3] : 약관형태
                    약관형태 = 전달인수[3];

                    담보미리보기 = "담보미리보기";
                    담보파일_담보update(전달인수[1], 전달인수[2], 약관형태);
                    break;
                case "약관형태":
                    약관형태 = 전달인수[1];
                    if (약관형태 == "분리담보")
                    {
                        SelectedTabPage초기화(담보약관);
                    }
                    else if (약관형태 == "전체약관")
                    {
                        SelectedTabPage초기화(전체약관);
                    }
                    break;
            }

        }
        
        private async void SelectedTabPage초기화(DevExpress.XtraTab.XtraTabPage 선택)
        {
            전체_담보약관.SelectedTabPage = 선택;
            await grd약관정보목록View(0);
            grd담보리스트_화면초기화();
            grd담보_화면초기화();
        }

        private async void 담보코드_검색(string 고객사, string 약관형태, List<string> Tmp조건, string 테이블명)
        {
            using (System.Data.SqlClient.SqlConnection AdoNet = new System.Data.SqlClient.SqlConnection(약관DB))   /** 데이터베이스 접속 **/
            {
                AdoNet.Open();

                string 조건 = string.Join(" AND ", Tmp조건);

                string Sqlstr = (약관형태 == "전체약관")
                        ? $"SELECT * FROM 약관_{고객사} WHERE {조건} ORDER BY 상품코드"
                        : $"SELECT * FROM {테이블명} WHERE {조건} ORDER BY 일련번호";

                using (System.Data.SqlClient.SqlCommand sqlcmd = new System.Data.SqlClient.SqlCommand(Sqlstr, AdoNet))
                using (System.Data.SqlClient.SqlDataReader reader = sqlcmd.ExecuteReader())
                {

                    if (!reader.HasRows)
                    {
                        MessageBox.Show("해당 상품, 담보가 존재하지 않습니다.", "해당담보코드 미존재", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    DataTable table;

                    if (약관형태 == "전체약관")
                    {
                        table = await 약관정보목록DT();

                        while (reader.Read())
                        {
                            table.Rows.Add(new object[]
                            {
                                false,
                                reader["약관PK"]?.ToString(),
                                고객사,
                                reader["상품명"]?.ToString(),
                                reader["상품코드"]?.ToString(),
                                reader["개정일"]?.ToString(),
                                reader["담보분류"]?.ToString(),
                                reader["작성일"]?.ToString(),
                                reader["파일경로"]?.ToString()
                            });
                        }
                        grd약관정보목록.DataSource = table;
                        view약관정보목록.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);

                    } else
                    {  // 분리담보

                        table = 담보리스트datatable();
                        while (reader.Read())
                        {
                            table.Rows.Add(new object[]
                            {
                                false,
                                reader["담보PK"]?.ToString(),
                                reader["고객사코드"]?.ToString(),
                                reader["상품명"]?.ToString(),
                                reader["상품코드"]?.ToString(),
                                reader["개정일"]?.ToString(),
                                reader["담보분류"]?.ToString(),
                                reader["담보명"]?.ToString(),
                                reader["담보코드"]?.ToString(),
                                reader["대표담보코드"]?.ToString(),
                                reader["세션"]?.ToString(),
                                reader["구분"]?.ToString(),
                                reader["형"]?.ToString(),
                                reader["종"]?.ToString(),
                                reader["권"]?.ToString(),
                                reader["편"]?.ToString(),
                                reader["장"]?.ToString(),
                                reader["절"]?.ToString(),
                                reader["seq"]?.ToString(),
                                reader["분류01"]?.ToString(),
                                reader["분류02"]?.ToString(),
                                reader["분류03"]?.ToString(),
                                reader["분류04"]?.ToString(),
                                reader["실손담보"]?.ToString(),
                                reader["일자"]?.ToString(),
                                reader["형식"]?.ToString(),
                                reader["담보"]?.ToString(),
                                reader["sortkey"]?.ToString(),
                                ""
                            });
                        } // END while

                        grd담보리스트.DataSource = table;
                        view담보리스트.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
                    }
                }
            }
        }

       
        private int 담보전체갯수()
        {
            int 선택갯수 = 0;
            GridView gdview = view담보리스트;
            for (int i = 0; i < gdview.RowCount; i++) {
                if (gdview.GetRowCellDisplayText(i, "확인").ToString() == "Checked")
                    선택갯수++;
            }
            return 선택갯수;
        }

        private void 진행바초기화()
        {
            barHwp변환진행.Properties.ShowTitle = true;    // 값표시
            barHwp변환진행.Properties.PercentView = true;  // %로 표시 
            barHwp변환진행.Properties.Maximum = 100;
            barHwp변환진행.Properties.Minimum = 0;
            barHwp변환진행.Properties.Step = 1;
            barHwp변환진행.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Broken;
            barHwp변환진행.EditValue = 0;

            txt전체담보갯수.EditValue = 0;
            txt현재변환담보갯수.EditValue = 0;

        }

        private void 담보파일_담보update(string 고객사코드, string 테이블명, string _약관형태)
        {
            
            bool 오류확인 = false;
            int 업데이트cnt = -1;
            using (System.Data.SqlClient.SqlConnection AdoNet = new System.Data.SqlClient.SqlConnection(약관DB))   /** 데이터베이스 접속 **/
            {
                AdoNet.Open();
                    
                string SqlStr = "";
                string 일자 = "";
                if (_약관형태 == "분리담보")
                {
                    SqlStr = $@"Update {테이블명} 
                            Set 일자 = @일자, 형식 = @형식, 담보 = @담보 
                            Where 담보PK = @담보PK and 담보분류 = @담보분류";
                    일자 = $"{DateTime.Now:yyyy-MM-dd}";

                } else if (_약관형태 == "전체약관")
                {
                    SqlStr = $@"Update {테이블명} 
                            Set 작성일 = @작성일 
                            Where 약관PK = @약관PK";
                    일자 = $"{DateTime.Now:yyMMddHHmmss}";
                }

                System.Data.SqlClient.SqlCommand Sqlcmd = new System.Data.SqlClient.SqlCommand(SqlStr, AdoNet);
                
                string Update문구 = "";
                int 전체변환전담보Cnt = view담보.RowCount;

                GridView gdview = _약관형태 == "분리담보" ? view담보리스트 : view약관정보목록;
                string tmpconvhwp = "";
                
                for (int i = 0; i < gdview.RowCount; i++) {
                    if (gdview.GetRowCellDisplayText(i, "확인").ToString() != "Checked")
                        continue;

                    업데이트cnt++;
                    if (_약관형태 == "분리담보")
                    {
                        string tmp파일경로 = gdview.GetRowCellDisplayText(i, "파일경로").ToString();
                        tmpconvhwp = "";
                        Hwpconv읽기(tmp파일경로, out tmpconvhwp);
                        Sqlcmd.Parameters.Add("@일자  ", SqlDbType.VarChar).Value = 일자;
                        Sqlcmd.Parameters.Add("@형식  ", SqlDbType.VarChar).Value = gdview.GetRowCellDisplayText(i, "형식").ToString();
                        Sqlcmd.Parameters.Add("@담보  ", SqlDbType.VarChar).Value = tmpconvhwp;
                        Sqlcmd.Parameters.Add("@담보PK  ", SqlDbType.VarChar).Value = gdview.GetRowCellDisplayText(i, "담보PK").ToString();
                        Sqlcmd.Parameters.Add("@담보분류  ", SqlDbType.VarChar).Value = gdview.GetRowCellDisplayText(i, "담보분류").ToString();
                    }
                    else if (_약관형태 == "전체약관")
                    {
                        Sqlcmd.Parameters.Add("@작성일  ", SqlDbType.VarChar).Value = 일자;
                        Sqlcmd.Parameters.Add("@약관PK  ", SqlDbType.VarChar).Value = gdview.GetRowCellDisplayText(i, "약관PK").ToString();
                    }

                    int 확인Cnt = Sqlcmd.ExecuteNonQuery();

                    if (확인Cnt >= 1)
                        Update문구 = "Update완료";
                    else {
                        Update문구 = "Update오류";
                        오류확인 = true;
                    }
                    Sqlcmd.Parameters.Clear();

                    if (_약관형태 == "분리담보")
                    {
                        gdview.SetRowCellValue(i, "담보", tmpconvhwp);
                        gdview.SetRowCellValue(i, "상품명", Update문구);
                    }

                    담보관리메인화면event[0] = "진행상황";              // 진행상황에 값을 전달한다.
                    담보관리메인화면event[1] = (Convert.ToInt32((double)(i + 1) / (double)전체변환전담보Cnt * 100)).ToString();
                    담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);

                }
            }
            if (오류확인) {
                MessageBox.Show("업데이트를 완료하지 못했습니다.\n해당담보는 상품명을 참조하세요", "Update오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (_약관형태 == "전체약관")
                    담보파일_약관upload(skipMobileUpload: true, 작업형태 : "전체약관업데이트");
                else
                {
                    MessageBox.Show($"[완료] === 전체약관 업데이트 완료 ===\n총 {업데이트cnt}개의 항목이 성공적으로 처리되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
           
        }

        private List<int> 중복에러_RowList = new List<int>();
        private async void 담보파일_약관upload(bool skipMobileUpload = false, string 작업형태 = "")
        {
            // =========================================================================
            // 1. [매우 중요] 설정값이 로드되었는지 확인하고, 비어있다면 무조건 먼저 로드!
            // =========================================================================
            if (저장.설정값 == null || 저장.설정값.Count == 0)
            {
                // frm담보관리 폼에 맞게 기초 변수만 살짝 세팅해 줍니다.
                저장.사용컴퓨터 = SystemInformation.ComputerName.ToLower();
                저장.DB설정파일path = System.IO.Path.Combine(Application.StartupPath, "localDB.txt");

                // 다른 폼(frm담보Merge)을 거칠 필요 없이, 바로 공통 매니저에게 파일 로드를 지시합니다!
                string 설정결과 = await NetworkManager.공통설정파일_로드();

                if (설정결과 == "실패")
                {
                    MessageBox.Show("설정 파일(localDB.txt) 로드에 실패했습니다.\n업로드를 중단합니다.", "설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            List<레코드> 레코드s = new List<레코드>();

            try
            {
                // =========================================================================
                // 2. 그리드에서 체크된 데이터 추출
                // =========================================================================
                for (int i = 0; i < view약관정보목록.DataRowCount; i++)
                {
                    // Null 에러 방지를 위해 object로 먼저 받습니다.
                    object chkObj = view약관정보목록.GetRowCellValue(i, "확인");
                    bool isChecked = chkObj != null && chkObj != DBNull.Value && Convert.ToBoolean(chkObj);

                    if (isChecked)
                    {
                        string 고객사 = view약관정보목록.GetRowCellValue(i, "고객사")?.ToString() ?? "";
                        string 상품명 = view약관정보목록.GetRowCellValue(i, "상품명")?.ToString() ?? "";
                        string 상품코드 = view약관정보목록.GetRowCellValue(i, "상품코드")?.ToString() ?? "";
                        string 종분류코드 = view약관정보목록.GetRowCellValue(i, "종분류코드")?.ToString() ?? "0";
                        string 개정일 = view약관정보목록.GetRowCellValue(i, "개정일")?.ToString() ?? "";
                        string 모바일파일 = view약관정보목록.GetRowCellValue(i, "파일경로")?.ToString() ?? "";

                        string 출력폴더 = $@"{고객사}\YagGwan\{상품코드}\";

                        레코드s.Add(new 레코드()
                        {
                            그리드RowHandle = i,
                            고객사 = 고객사,
                            상품명 = 상품명,
                            상품코드 = 상품코드,
                            종분류코드 = 종분류코드,
                            개정일 = 개정일,
                            모바일파일 = 모바일파일,
                            출력폴더 = 출력폴더,
                            상태 = "전체약관"
                        });
                    }
                }

                // =========================================================================
                // 3. 업로드 로직 시작
                // =========================================================================
                if (레코드s.Count > 0)
                {
                    현재상태 상태 = new 현재상태();
                    상태.처리단계 = "전체약관";
                    상태.고객사 = 레코드s[0].고객사;
                    상태.출력폴더 = 레코드s[0].출력폴더;

                    // [안전한 DB 접속 세팅] TryGetValue를 사용하여 에러 원천 차단
                    if (저장.설정값.TryGetValue($"접속{저장.사용컴퓨터}", out string conn))
                    {
                        상태.dataTable접속 = conn;
                    }
                    else if (저장.설정값.TryGetValue("접속LOCALDB", out string localDb))
                    {
                        상태.dataTable접속 = localDb;
                    }
                    else
                    {
                        throw new Exception("localDB.txt 파일에 DB 접속 설정(접속LOCALDB)이 없습니다.");
                    }

                    // =========================================================================
                    // 4. 안전한 화면 업데이트용 콜백(Action) 정의
                    // =========================================================================
                    Action<string, int> logCallback = (msg, level) =>
                    {
                        // UI 스레드 안전성 보장 (만약 로그를 찍을 리스트박스가 있다면 이 주석을 풀고 사용하세요)
                        // if (this.InvokeRequired)
                        //     this.Invoke(new Action(() => lst로그.Items.Add(msg)));
                        // else
                        //     lst로그.Items.Add(msg);

                        Console.WriteLine(msg); // 기본적으로 콘솔에 출력되게 둡니다.
                    };

                    Action<string[]> progressCallback = (info) =>
                    {
                        // info[0]: "FTP진행", info[1]: 현재(%), info[2]: 전체(%), info[3]: 파일크기진행률
                        if (info[0] == "FTP진행")
                        {
                            // UI 스레드 안전성 보장을 위한 Action 델리게이트 생성
                            Action updateUI = () =>
                            {
                                // [방법 1] 메인 화면(frmEasy약관)으로 진행률 이벤트 전달
                                담보관리메인화면event[0] = "진행상황";
                                담보관리메인화면event[1] = info[2]; // 전체 진행률(%) 전달
                                담보관리메인화면_SetMessage?.Invoke(담보관리메인화면event);
                            };

                            // InvokeRequired를 통해 UI 스레드 접근 검사 후 비동기(BeginInvoke) 실행
                            if (this.InvokeRequired)
                                this.BeginInvoke(updateUI);
                            else
                                updateUI();
                        }
                    };

                    // =========================================================================
                    // 5. 업로드 전 사전 검사 및 필터링
                    // =========================================================================

                    // DB를 찔러서 이미 존재하는 약관은 레코드의 '중복스킵여부'를 true로 만듭니다.
                    // 중복에러 리스트 수집 (그리드에 빨간색 표시를 위함)

                    if (작업형태 != "전체약관업데이트")
                        await NetworkManager.약관_사전중복체크(상태, 레코드s, logCallback);

                    중복에러_RowList.Clear();
                    int 중복카운트 = 0;
                    List<레코드> 실제업로드대상 = new List<레코드>();

                    foreach (var item in 레코드s)
                    {
                        if (item.중복스킵여부)
                        {
                            중복에러_RowList.Add(item.그리드RowHandle);
                            중복카운트++;
                        }
                        else
                        {
                            // 중복이 아닌 것만 진짜 업로드 목록에 담습니다.
                            실제업로드대상.Add(item);
                        }
                    }

                    // 화면 즉시 갱신 (빨간색 줄 먼저 보여주기)
                    view약관정보목록.RefreshData();

                    // =========================================================================
                    // 6. 대망의 NetworkManager 호출! (필터링된 목록만!)
                    // =========================================================================
                    string 문구 = "업로드";

                    if (실제업로드대상.Count > 0)
                    {
                        if (상태.고객사 == "KB")
                        {
                            await NetworkManager.FTP_FILE_UPLOAD2(상태, 실제업로드대상, logCallback, progressCallback);
                        }
                        else
                        {
                            await NetworkManager.FTP_FILE_UPLOAD(상태, 실제업로드대상, logCallback, progressCallback);
                        }

                        if (!skipMobileUpload)
                        {
                            // DB 업로드 시 IF NOT EXISTS는 이중 안전장치로 놔두시면 됩니다.
                            await NetworkManager.모바일데이터업로드(상태, 실제업로드대상, logCallback);
                            문구 = "업로드";
                        }
                        else
                        {
                            문구 = "업데이트";
                        }
                    }


                    // =========================================================================
                    // 7. 결과 메시지 출력
                    // =========================================================================
                    if (중복카운트 > 0)
                    {
                        if (실제업로드대상.Count > 0)
                        {
                            MessageBox.Show($"[완료 및 알림]\n전체 {레코드s.Count}건 중 {중복카운트}건은 중복 스킵되고, 나머지 {실제업로드대상.Count}건 {문구} 완료되었습니다.\n붉은색 표시 항목을 확인하세요.", "처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"[알림] 선택하신 {레코드s.Count}건 모두 이미 등록된 약관입니다.\n업로드할 새 데이터가 없습니다.", "모두 중복", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"[완료] === 전체약관 {문구} 완료 ===\n총 {실제업로드대상.Count}개의 항목이 성공적으로 처리되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                else
                {
                    MessageBox.Show("업로드할 항목이 선택되지 않았습니다.\n그리드에서 업로드할 항목의 [확인] 체크박스를 선택해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] 전체약관 업로드 중 오류가 발생했습니다:\n\n{ex.Message}", "오류 발생", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task 담보파일_담보upload(string 테이블명)
        {
            try
            {
                GridView gdview = view담보리스트;
                int 전체변환전담보Cnt = gdview.RowCount;

                // 공통값 추출
                string 공통_고객사코드 = gdview.GetRowCellDisplayText(0, "고객사코드").ToString();
                string 공통_상품명 = gdview.GetRowCellDisplayText(0, "상품명").ToString();
                string 공통_상품코드 = gdview.GetRowCellDisplayText(0, "상품코드").ToString();
                string 공통_개정일 = gdview.GetRowCellDisplayText(0, "개정일").ToString();

                // [핵심 1] UI 스레드 충돌 방지를 위해, 그리드의 데이터를 List로 먼저 싹 빼냅니다.
                var uploadList = new List<UploadItem>();

                for (int i = 0; i < gdview.RowCount; i++)
                {
                    string 형값 = gdview.GetRowCellDisplayText(i, "형").ToString();
                    string 종값 = gdview.GetRowCellDisplayText(i, "종").ToString();
                    string 권값 = gdview.GetRowCellDisplayText(i, "권").ToString();
                    string 편값 = gdview.GetRowCellDisplayText(i, "편").ToString();
                    string 장값 = gdview.GetRowCellDisplayText(i, "장").ToString();
                    string 절값 = gdview.GetRowCellDisplayText(i, "절").ToString();

                    uploadList.Add(new UploadItem
                    {
                        RowIndex = i,
                        담보PK = gdview.GetRowCellDisplayText(i, "담보PK").ToString(),
                        담보분류 = gdview.GetRowCellDisplayText(i, "담보분류").ToString(),
                        담보명 = gdview.GetRowCellDisplayText(i, "담보명").ToString(),
                        담보코드 = gdview.GetRowCellDisplayText(i, "담보코드").ToString(),
                        대표담보코드 = gdview.GetRowCellDisplayText(i, "대표담보코드").ToString(),
                        세션 = gdview.GetRowCellDisplayText(i, "세션").ToString(),
                        구분 = gdview.GetRowCellDisplayText(i, "구분").ToString(),
                        형 = string.IsNullOrEmpty(형값) ? "00" : 형값,
                        종 = string.IsNullOrEmpty(종값) ? "00" : 종값,
                        권 = string.IsNullOrEmpty(권값) ? "00" : 권값,
                        편 = string.IsNullOrEmpty(편값) ? "00" : 편값,
                        장 = string.IsNullOrEmpty(장값) ? "00" : 장값,
                        절 = string.IsNullOrEmpty(절값) ? "00" : 절값,
                        seq = gdview.GetRowCellDisplayText(i, "seq").ToString(),
                        분류01 = gdview.GetRowCellDisplayText(i, "분류01").ToString(),
                        분류02 = gdview.GetRowCellDisplayText(i, "분류02").ToString(),
                        분류03 = gdview.GetRowCellDisplayText(i, "분류03").ToString(),
                        분류04 = gdview.GetRowCellDisplayText(i, "분류04").ToString(),
                        실손담보 = gdview.GetRowCellDisplayText(i, "실손담보").ToString(),
                        일자 = gdview.GetRowCellDisplayText(i, "일자").ToString(),
                        형식 = gdview.GetRowCellDisplayText(i, "형식").ToString(),
                        파일경로 = gdview.GetRowCellDisplayText(i, "파일경로").ToString(),
                        sortkey = gdview.GetRowCellDisplayText(i, "sortkey").ToString(),
                        일련번호 = gdview.GetRowCellDisplayText(i, "일련번호").ToString()
                    });
                }

                int 대상건수 = uploadList.Count;
                if (대상건수 == 0) return;

                int 완료건수 = 0;
                int 보고된퍼센트 = -1; // 진행률 중복 갱신 방지용 변수

                string SqlStr = $@"Insert Into {테이블명} (담보PK, 고객사코드, 상품명, 상품코드, 개정일, 담보분류, 담보명, 담보코드, 
                                                   대표담보코드, 세션, 구분, 형, 종, 권, 편, 장, 절, seq, 분류01, 분류02, 분류03, 분류04, 실손담보, 일자, 형식, 담보, sortkey, 일련번호) 
                                  Values (@담보PK, @고객사코드, @상품명, @상품코드, @개정일, @담보분류, @담보명, @담보코드, @대표담보코드, @세션, @구분, @형, @종, @권, @편, @장, @절, @seq, @분류01, @분류02, @분류03, @분류04, @실손담보, @일자, @형식, @담보, @sortkey, @일련번호)";

                // [핵심 2] 병렬 처리용 세마포어(파일 읽기) & DB 1줄 서기용 세마포어(교착 상태 원천 방지)
                using (SemaphoreSlim dbLock = new SemaphoreSlim(1, 1)) // DB 인서트는 한 번에 1개씩!
                using (SemaphoreSlim semaphore = new SemaphoreSlim(20)) // 파일 읽기는 한 번에 20개씩 병렬!
                {
                    var tasks = uploadList.Select(async item =>
                    {
                        await semaphore.WaitAsync(); // 20개 슬롯 중 하나 대기
                        try
                        {
                            string tmpconvhwp = "";
                            if (item.구분 != "기본")
                            {
                                try
                                {
                                    // [병렬 구간] 무거운 파일 읽기는 20개가 동시에 처리하여 속도 확보
                                    if (System.IO.File.Exists(item.파일경로))
                                    {
                                        using (var reader = new System.IO.StreamReader(item.파일경로, System.Text.Encoding.Default))
                                        {
                                            tmpconvhwp = await reader.ReadToEndAsync();
                                        }
                                    }
                                }
                                catch { }
                            }


                            // [직렬 구간] DB 접근 시에는 dbLock으로 1줄 서기를 강제 (Deadlock 1205 에러 원천 차단)
                            await dbLock.WaitAsync();
                            try
                            {
                                using (var conn = new System.Data.SqlClient.SqlConnection(저장.약관DB))
                                {
                                    await conn.OpenAsync();
                                    using (var cmd = new System.Data.SqlClient.SqlCommand(SqlStr, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@담보PK", item.담보PK);
                                        cmd.Parameters.AddWithValue("@고객사코드", 공통_고객사코드);
                                        cmd.Parameters.AddWithValue("@상품명", 공통_상품명);
                                        cmd.Parameters.AddWithValue("@상품코드", 공통_상품코드);
                                        cmd.Parameters.AddWithValue("@개정일", 공통_개정일);
                                        cmd.Parameters.AddWithValue("@담보분류", item.담보분류);
                                        cmd.Parameters.AddWithValue("@담보명", item.담보명);
                                        cmd.Parameters.AddWithValue("@담보코드", item.담보코드);
                                        cmd.Parameters.AddWithValue("@대표담보코드", item.대표담보코드);
                                        cmd.Parameters.AddWithValue("@세션", item.세션);
                                        cmd.Parameters.AddWithValue("@구분", item.구분);
                                        cmd.Parameters.AddWithValue("@형", item.형);
                                        cmd.Parameters.AddWithValue("@종", item.종);
                                        cmd.Parameters.AddWithValue("@권", item.권);
                                        cmd.Parameters.AddWithValue("@편", item.편);
                                        cmd.Parameters.AddWithValue("@장", item.장);
                                        cmd.Parameters.AddWithValue("@절", item.절);
                                        cmd.Parameters.AddWithValue("@seq", item.seq);
                                        cmd.Parameters.AddWithValue("@분류01", item.분류01);
                                        cmd.Parameters.AddWithValue("@분류02", item.분류02);
                                        cmd.Parameters.AddWithValue("@분류03", item.분류03);
                                        cmd.Parameters.AddWithValue("@분류04", item.분류04);
                                        cmd.Parameters.AddWithValue("@실손담보", item.실손담보);
                                        cmd.Parameters.AddWithValue("@일자", item.일자);
                                        cmd.Parameters.AddWithValue("@형식", item.형식);
                                        cmd.Parameters.AddWithValue("@담보", tmpconvhwp);
                                        cmd.Parameters.AddWithValue("@sortkey", item.sortkey);
                                        cmd.Parameters.AddWithValue("@일련번호", item.일련번호);

                                        int 확인Cnt = await cmd.ExecuteNonQueryAsync();
                                        item.업로드결과 = 확인Cnt >= 1 ? "업로드완료" : "업로드오류";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                item.업로드결과 = "업로드오류";
                                System.Diagnostics.Debug.WriteLine($"DB Insert Error: {ex.Message}");
                                MessageBox.Show($"상품코드 : {공통_상품코드} - {ex.Message}");

                            }
                            finally
                            {
                                dbLock.Release(); // 내 차례 끝, 다음 작업자 입장
                            }
                        }
                        catch (Exception ex)
                        {
                            item.업로드결과 = "업로드오류";
                            System.Diagnostics.Debug.WriteLine($"System Error: {ex.Message}");
                        }
                        finally
                        {
                            // 진행상황 안전하게 카운트 상승
                            int current = Interlocked.Increment(ref 완료건수);
                            int percent = (int)(((double)current / 대상건수) * 100);

                            int 몫 = percent / 5;

                            // 몫이 이전과 달라졌을 때(5% 구간 돌파) 또는 마지막 건수일 때 화면 업데이트
                            if (몫 != 보고된퍼센트 || current == 대상건수)
                            {
                                보고된퍼센트 = 몫; // 중복 호출 방지를 위해 현재 몫으로 갱신

                                // UI 업데이트를 위한 대리자(Action) 생성
                                Action updateUI = () =>
                                {
                                    담보관리메인화면_SetMessage?.Invoke(new string[] { "진행상황", percent.ToString() });
                                };

                                // UI 스레드 충돌 방지
                                if (this.InvokeRequired)
                                {
                                    this.BeginInvoke(updateUI);
                                }
                                else
                                {
                                    updateUI();
                                }
                            }

                            semaphore.Release(); // 20개 슬롯 중 하나 반환
                        }
                    });

                    // 추출한 모든 항목이 완료될 때까지 동시다발적으로 대기
                    await Task.WhenAll(tasks);
                }

                // 모든 병렬 작업이 끝난 후, 모아둔 결과를 한 번에 그리드에 반영
                foreach (var item in uploadList)
                {
                    gdview.SetRowCellValue(item.RowIndex, "상품코드", item.업로드결과);
                }

                // 소요시간 종료 이벤트
                담보관리메인화면_SetMessage?.Invoke(new string[] { "업로드소요시간" });
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, "업로드오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Hwpconv읽기(string tmp파일경로, out string _tmpconvhwp)
        {
            try {

                _tmpconvhwp = System.IO.File.ReadAllText(tmp파일경로);              
            } catch (Exception) {
                _tmpconvhwp = "";
            }
        }



        private async Task Encode실행()
        {
            int 보고된몫 = -1; // 진행률 변화 감지용

            // 1. 변환 대상 리스트 수집
            var 대상리스트 = new List<EncodeItem>();
            GridView gdview = view담보리스트;
            for (int i = 0; i < gdview.RowCount; i++)
            {
                if (gdview.GetRowCellDisplayText(i, "확인").ToString() == "Checked")
                {
                    대상리스트.Add(new EncodeItem
                    {
                        RowIndex = i,
                        파일경로 = gdview.GetRowCellDisplayText(i, "파일경로").ToString(),
                        확장자 = gdview.GetRowCellDisplayText(i, "형식").ToString().ToUpper()
                    });
                }
            }

            // =========================================================================
            // [이동됨] AxHwpCtrl 초기화 (UI 스레드에서 안전하게 실행됨)
            // =========================================================================
            for (int i = 0; i < 최대스레드cnt; i++)
            {
                if (Ax02[i] == null)
                {
                    Ax02[i] = new AxHwpCtrl();
                    Ax02[i].CreateControl();
                    Ax02[i].RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                }
                Ax02[i].Clear(1);
            }

            // try-finally로 묶어 에러 발생 시에도 무조건 메모리를 해제하도록 보장합니다.
            try
            {
                // 2. 사용 가능한 AxHwpCtrl 인덱스 풀 (0 ~ 5)
                var availableAxHwp = new System.Collections.Concurrent.ConcurrentQueue<int>(Enumerable.Range(0, 최대스레드cnt));

                // 3. 동시에 최대 6개씩 비동기 병렬 처리
                using (SemaphoreSlim semaphore = new SemaphoreSlim(최대스레드cnt))
                {
                    var tasks = 대상리스트.Select(async item =>
                    {
                        await semaphore.WaitAsync(); // 슬롯 대기
                        int ax인덱스 = -1;
                        try
                        {
                            string 저장파일경로 = "";

                            if (item.확장자 == "PDF")
                            {
                                저장파일경로 = await Task.Run(() =>
                                {
                                    byte[] bytepdf = System.IO.File.ReadAllBytes(item.파일경로);
                                    string base64Str = Convert.ToBase64String(bytepdf);
                                    return 파일저장및경로반환(item.파일경로, base64Str);
                                });
                            }
                            else if (item.확장자 == "HWP")
                            {
                                while (!availableAxHwp.TryDequeue(out ax인덱스)) { await Task.Delay(10); }

                                string base64Hwp = "";

                                this.Invoke(new Action(() =>
                                {
                                    Ax02[ax인덱스].Clear(1);
                                    Ax02[ax인덱스].Open(item.파일경로);
                                    object obj = Ax02[ax인덱스].GetTextFile("HWP", "");
                                    base64Hwp = obj?.ToString() ?? "";
                                }));

                                if (!string.IsNullOrEmpty(base64Hwp))
                                {
                                    저장파일경로 = await Task.Run(() => 파일저장및경로반환(item.파일경로, base64Hwp));
                                }
                            }

                            item.새저장경로 = 저장파일경로;

                            int currentCnt = Interlocked.Increment(ref 완료cnt);
                            int 현재퍼센트 = (int)(((double)currentCnt / 선택전체갯수) * 100);
                            int 몫 = 현재퍼센트 / 5;

                            if (몫 != 보고된몫 || currentCnt == 선택전체갯수)
                            {
                                보고된몫 = 몫;
                                this.BeginInvoke(new Action(() =>
                                {
                                    txt현재변환담보갯수.EditValue = currentCnt;
                                    barHwp변환진행.EditValue = 현재퍼센트;
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => MessageBox.Show($"파일 변환 오류 ({item.파일경로}):\n" + ex.Message)));
                        }
                        finally
                        {
                            if (ax인덱스 != -1) availableAxHwp.Enqueue(ax인덱스);
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }

                view담보리스트.BeginUpdate();
                foreach (var item in 대상리스트)
                {
                    if (!string.IsNullOrEmpty(item.새저장경로))
                    {
                        view담보리스트.SetRowCellValue(item.RowIndex, "파일경로", item.새저장경로);
                    }
                }
                view담보리스트.EndUpdate();

                담보관리메인화면event[0] = "소요시간종료";
                담보관리메인화면_SetMessage?.Invoke(담보관리메인화면event);
            }
            finally
            {
                // =========================================================================
                // 인코딩 작업이 모두 끝나면(또는 실패해도) 무조건 HWP 메모리 정리
                // =========================================================================
                for (int i = 0; i < 최대스레드cnt; i++)
                {
                    if (Ax02[i] != null)
                    {
                        try
                        {
                            Ax02[i].Clear(1);
                            Ax02[i].Dispose();
                        }
                        catch { }
                        finally
                        {
                            Ax02[i] = null;
                        }
                    }
                }
            }
        }

        // 파일 경로와 Base64 텍스트를 받아 파일을 쓰고 경로를 반환하는 공통 함수
        private string 파일저장및경로반환(string 원본경로, string base64Data)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(원본경로);
            string con저장경로 = fi.DirectoryName + "\\Hwpcnv\\";
            string con파일명 = fi.Name;

            // 안전한 문자열 자르기 방어코드 추가 (IndexOutOfRange 에러 방지)
            if (con파일명.StartsWith("sn", StringComparison.OrdinalIgnoreCase) && con파일명.Length > 7)
                con파일명 = con파일명.Substring(7);

            // 파일명만 추출 (확장자 제거)
            con파일명 = System.IO.Path.GetFileNameWithoutExtension(con파일명);

            if (System.IO.Directory.Exists(con저장경로) == false)
                System.IO.Directory.CreateDirectory(con저장경로);

            string con저장파일 = System.IO.Path.Combine(con저장경로, con파일명 + ".hwc");

            // 디스크에 한 번에 쓰기 (속도 개선)
            System.IO.File.WriteAllText(con저장파일, base64Data);

            return con저장파일;
        }

        private string 담보파일_담보리스트_비교(string _약관형태)
        {

            GridView targetView = _약관형태 == "분리담보" ? view담보리스트 : view약관정보목록;
            string 검색필드 = _약관형태 == "분리담보" ? "담보코드" : "약관PK";

            // 1. 데이터 유효성 검사
            if (view담보.RowCount <= 0 || targetView.RowCount <= 0)
            {
                MessageBox.Show("담보(약관) 파일 또는 리스트가 없습니다.", "목록오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "오류";
            }

            // 2. 검색 속도 극대화를 위해 리스트의 데이터를 Dictionary 형식으로 수집 (O(1) 검색)
            // Key: 담보코드(약관PK), Value: 해당 코드를 가진 행(RowHandle)의 인덱스 리스트
            var 담보리스트맵 = new Dictionary<string, List<int>>();
            for (int i = 0; i < targetView.RowCount; i++)
            {
                string 코드 = targetView.GetRowCellDisplayText(i, 검색필드)?.ToString() ?? "";
                if (string.IsNullOrEmpty(코드)) continue;

                if (!담보리스트맵.ContainsKey(코드))
                {
                    담보리스트맵[코드] = new List<int>();
                }
                담보리스트맵[코드].Add(i);
            }

            // 3. 뷰 업데이트 성능 향상 (대량 변경 시 깜빡임 방지)
            view담보.BeginUpdate();
            targetView.BeginUpdate();

            bool 중복확인 = false;

            try
            {
                for (int i = 0; i < view담보.RowCount; i++)
                {
                    string 원본파일명 = view담보.GetRowCellDisplayText(i, "파일명")?.ToString() ?? "";
                    if (string.IsNullOrEmpty(원본파일명)) continue;

                    // 4. 안전한 문자열 처리: Substring 대신 Path.GetFileNameWithoutExtension 등 사용
                    string 확장자 = Path.GetExtension(원본파일명).Replace(".", "").ToUpper(); // 예: ".PDF" -> "PDF"
                    string tmp담보코드 = Path.GetFileNameWithoutExtension(원본파일명);

                    // 파일명이 "sn"으로 시작하고 길이가 충분한 경우 앞부분(sn0001_) 자르기
                    if (tmp담보코드.StartsWith("sn", StringComparison.OrdinalIgnoreCase) && tmp담보코드.Length > 7)
                    {
                        tmp담보코드 = tmp담보코드.Substring(7);
                    }

                    // 5. Dictionary를 이용한 초고속 검색
                    if (!담보리스트맵.TryGetValue(tmp담보코드, out List<int> 매칭된행들))
                    {
                        // 미존재 (Case 0)
                        view담보.SetRowCellValue(i, "확인", false);
                        view담보.SetRowCellValue(i, "상태", "미존재");
                    }
                    else if (매칭된행들.Count == 1)
                    {
                        // 정상 매칭 (Case 1)
                        view담보.SetRowCellValue(i, "확인", true);

                        int 담보리스트위치 = 매칭된행들[0];
                        string 파일경로 = view담보.GetRowCellDisplayText(i, "파일경로")?.ToString() ?? "";

                        targetView.SetRowCellValue(담보리스트위치, "확인", true);
                        targetView.SetRowCellValue(담보리스트위치, "형식", 확장자);
                        targetView.SetRowCellValue(담보리스트위치, "파일경로", 파일경로);

                        // (선택) FocusedRow 변경은 UI를 산만하게 할 수 있으므로 필요시에만 켜세요.
                        targetView.FocusedRowHandle = 담보리스트위치;
                    }
                    else
                    {
                        // 중복 매칭 (Default)
                        view담보.SetRowCellValue(i, "확인", false);
                        view담보.SetRowCellValue(i, "상태", "중복");

                        foreach (int rowHandle in 매칭된행들)
                        {
                            // "담보PK" 컬럼명은 _약관형태에 따라 다를 수 있으므로 확인이 필요합니다.
                            targetView.SetRowCellValue(rowHandle, "담보PK", "중복");
                        }
                        중복확인 = true;
                    }
                }
            }
            finally
            {
                // 업데이트 잠금 해제
                view담보.EndUpdate();
                targetView.EndUpdate();
            }

            string 결과 = 중복확인 == true ? "오류" : "완료";

            return 결과;
        }


        private DataTable grd담보datatable()
        {
            System.Data.DataTable _table = new DataTable();
            _table.Columns.Add(new DataColumn("확인", typeof(bool)));
            _table.Columns.Add(new DataColumn("상태", typeof(string)));
            _table.Columns.Add(new DataColumn("파일명", typeof(string)));
            _table.Columns.Add(new DataColumn("파일경로", typeof(string)));

            return _table;
        }
        private void grd담보_화면초기화()
        {
            GridView gdview = view담보;

            gdview.GroupPanelText = "담보파일를 올려주세요";
            gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.GroupPanel.ForeColor = Color.DarkBlue;
            gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
            gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
            gdview.Appearance.FocusedRow.BackColor = Color.Orange;

            System.Data.DataTable table = new DataTable();
            table = grd담보datatable();

            gdview.OptionsBehavior.Editable = false;
            gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 

            grd담보.DataSource = table;

            int grdwidth = grd담보.Width;
            gdview.Columns["확인"].Width = Convert.ToInt32(grdwidth * 0.15);     //확인    
            gdview.Columns["상태"].Width = Convert.ToInt32(grdwidth * 0.15);     //상태    
            gdview.Columns["파일명"].Width = Convert.ToInt32(grdwidth * 0.7);   //파일명  
            gdview.Columns["파일경로"].Visible = false;  //파일경로 숨김

            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit 체크박스 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            gdview.Columns["확인"].ColumnEdit = 체크박스;
        }

        private DataTable 담보리스트datatable()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("확인", typeof(bool)));
            table.Columns.Add(new DataColumn("담보PK", typeof(string)));
            table.Columns.Add(new DataColumn("고객사코드", typeof(string)));
            table.Columns.Add(new DataColumn("상품명", typeof(string)));
            table.Columns.Add(new DataColumn("상품코드", typeof(string)));
            table.Columns.Add(new DataColumn("개정일", typeof(string)));
            table.Columns.Add(new DataColumn("담보분류", typeof(string)));
            table.Columns.Add(new DataColumn("담보명", typeof(string)));
            table.Columns.Add(new DataColumn("담보코드", typeof(string)));
            table.Columns.Add(new DataColumn("대표담보코드", typeof(string)));
            table.Columns.Add(new DataColumn("세션", typeof(string)));
            table.Columns.Add(new DataColumn("구분", typeof(string)));
            table.Columns.Add(new DataColumn("형", typeof(string)));
            table.Columns.Add(new DataColumn("종", typeof(string)));
            table.Columns.Add(new DataColumn("권", typeof(string)));
            table.Columns.Add(new DataColumn("편", typeof(string)));
            table.Columns.Add(new DataColumn("장", typeof(string)));
            table.Columns.Add(new DataColumn("절", typeof(string)));
            table.Columns.Add(new DataColumn("seq", typeof(string)));
            table.Columns.Add(new DataColumn("분류01", typeof(string)));
            table.Columns.Add(new DataColumn("분류02", typeof(string)));
            table.Columns.Add(new DataColumn("분류03", typeof(string)));
            table.Columns.Add(new DataColumn("분류04", typeof(string)));
            table.Columns.Add(new DataColumn("실손담보", typeof(string)));
            table.Columns.Add(new DataColumn("일자", typeof(string)));
            table.Columns.Add(new DataColumn("형식", typeof(string)));
            table.Columns.Add(new DataColumn("담보", typeof(string)));
            table.Columns.Add(new DataColumn("sortkey", typeof(string)));
            table.Columns.Add(new DataColumn("일련번호", typeof(string)));
            table.Columns.Add(new DataColumn("파일경로", typeof(string)));

            return table;
        }

        private void grd담보리스트_화면초기화()
        {
            GridView gdview = view담보리스트;
            gdview.BeginUpdate();

            gdview.GroupPanelText = "담보리스트를 올려주세요";
            gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.GroupPanel.ForeColor = Color.Blue;
            //gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
            gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
            gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            gdview.OptionsSelection.MultiSelect = true; ; /** 멀티라인 선택가능 **/
            gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;
            gdview.Appearance.SelectedRow.BackColor = Color.White;
            gdview.Appearance.SelectedRow.ForeColor = Color.Blue;


            gdview.Appearance.FocusedRow.ForeColor = Color.Blue;
            gdview.Appearance.FocusedRow.BackColor = Color.Orange;

            gdview.OptionsBehavior.Editable = false;
            gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
          

            System.Data.DataTable table = new DataTable();
            table = 담보리스트datatable();
            grd담보리스트.DataSource = table;

            int grdwidth = grd담보리스트.Width;
            gdview.Columns["확인"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["담보PK"].Width = Convert.ToInt32(grdwidth * 0.045);
            gdview.Columns["고객사코드"].Width = Convert.ToInt32(grdwidth * 0.05);
            gdview.Columns["상품명"].Width = Convert.ToInt32(grdwidth * 0.145);
            gdview.Columns["상품코드"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["개정일"].Width = Convert.ToInt32(grdwidth * 0.04);
            gdview.Columns["담보분류"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["담보명"].Width = Convert.ToInt32(grdwidth * 0.145);
            gdview.Columns["담보코드"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["대표담보코드"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["세션"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["구분"].Width = Convert.ToInt32(grdwidth * 0.03);
            gdview.Columns["형"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["종"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["권"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["편"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["장"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["절"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["seq"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["분류01"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["분류02"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["분류03"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["분류04"].Width = Convert.ToInt32(grdwidth * 0.02);
            gdview.Columns["실손담보"].Width = Convert.ToInt32(grdwidth * 0.045);
            gdview.Columns["일자"].Width = Convert.ToInt32(grdwidth * 0.045);
            gdview.Columns["형식"].Width = Convert.ToInt32(grdwidth * 0.04);
            gdview.Columns["담보"].Width = Convert.ToInt32(grdwidth * 0.045);
            gdview.Columns["sortkey"].Width = Convert.ToInt32(grdwidth * 0.045);
            gdview.Columns["일련번호"].Visible = false;
            gdview.Columns["파일경로"].Visible = false;  //파일경로 숨김

            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit 체크박스 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            gdview.Columns["확인"].ColumnEdit = 체크박스;
            gdview.EndUpdate();
        }

        private void grd담보_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void grd담보_DragDrop(object sender, DragEventArgs e)
        {
            grd담보_화면초기화();
            담보파일 = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            GridView gdview = view담보;

            System.Data.DataTable table = new DataTable();
            table = grd담보datatable();

            Array.Sort(담보파일);

            for (int i = 0; i < 담보파일.Length; i++)
            {
                string 확장자만 = Path.GetExtension(담보파일[i]).ToLower();
                if(확장자만 != ".hwp" && 확장자만 != ".pdf")
                {
                    MessageBox.Show($"알수없는 파일 : {new FileInfo(담보파일[i]).Name}","파일오류",MessageBoxButtons.OK);
                    return;
                }
                table.Rows.Add(new object[]
                {
                    false,                                           // 확인
                    "",                                              // 상태
                    new System.IO.FileInfo(담보파일[i]).Name,        // 파일명
                    new System.IO.FileInfo(담보파일[i]).FullName     // 파일경로
                });
            }
            grd담보.DataSource = table;
            gdview.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);   /** 셀서식 **/


            gdview = 약관형태 == "분리담보" ? view담보리스트 : view약관정보목록; // 양쪽모두 데이터 또는 리스트가 채워져 있는 상태

            if (gdview.RowCount > 0)
            {
                담보관리메인화면event[0] = "버튼활성화";
                담보관리메인화면event[1] = "담보비교";
                담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);
            }

        }

        private void grd담보리스트_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private async void grd담보리스트_DragDrop(object sender, DragEventArgs e)
        {
            담보리스트 = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            string[] data = System.IO.File.ReadAllLines(담보리스트[0], System.Text.Encoding.GetEncoding(949));
            if (data.Length <= 0)
                return;

            // =========================================================
            // 1. 드래그 앤 드롭 시점에서 DB 사전 중복 검사 실행
            // =========================================================
            bool 중복존재 = await 드래그앤드롭_사전중복검사(data);

            // 중복이 발견되면 메세지(검사 로직 내장)를 띄우고 즉시 로드를 중단합니다.
            if (중복존재) return;


            string[] 임시분리 = data[0].Split('\t');
            분리약관리스트(data);

            GridView gdview = view담보;  // 양쪽모두 데이터 또는 리스트가 채워져 있는 상태
            if (gdview.RowCount > 0)
            {
                담보관리메인화면event[0] = "버튼활성화";
                담보관리메인화면event[1] = "담보비교";
                담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);
            }
        }

        private async Task<bool> 드래그앤드롭_사전중복검사(string[] data)
        {
            // 1. 방어 코드: 데이터가 없거나 형식이 맞지 않으면 패스
            if (data == null || data.Length == 0) return false;

            string[] 첫줄 = data[0].Split('\t');
            if (첫줄.Length < 6) return false;

            // 2. 단일 상품 기준이므로 '첫 번째 줄'에서만 값을 추출 (Trim()으로 안전하게 공백 처리)
            string 고객사코드 = (첫줄[1] ?? "").Trim();
            string 상품코드 = (첫줄[3] ?? "").Trim();
            string 개정일 = (첫줄[4] ?? "").Trim();
            string 담보분류 = (첫줄[5] ?? "").Trim();

            // 3. 검사 대상 테이블명 동적 계산
            string tmp테이블인수 = "";

            // 개정일이 공백이 아니고, "000000"도 아니며, 길이가 2자리 이상일 때만 연도 계산 진행
            if (!string.IsNullOrEmpty(개정일) && 개정일 != "000000" && 개정일.Length >= 2)
            {
                // 숫자 변환 실패 시 에러 방지를 위해 TryParse 사용
                if (int.TryParse(개정일.Substring(0, 2), out int 개정년도))
                {
                    int 현재년도 = Convert.ToInt32(DateTime.Now.ToString("yy"));

                    // 과거 연도일 경우에만 테이블명에 꼬리표(_YY) 붙임
                    if (개정년도 != 0 && 개정년도 < 현재년도)
                    {
                        tmp테이블인수 = "_" + 개정일.Substring(0, 2);
                    }
                }
            }
            string 테이블명 = $"담보{고객사코드}{tmp테이블인수}";

            // 4. 단 한 번의 DB 조회로 전체 중복 판별
            using (var conn = new System.Data.SqlClient.SqlConnection(저장.약관DB))
            {
                await conn.OpenAsync();

                // 파라미터 쿼리는 값이 공백("")이어도 문법 에러 없이 완벽하게 작동합니다.
                string checkSql = $"SELECT TOP 1 1 FROM {테이블명} WHERE 상품코드 = @상품코드 AND 개정일 = @개정일 AND 담보분류 = @담보분류";
                using (var cmd = new System.Data.SqlClient.SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@상품코드", 상품코드);
                    cmd.Parameters.AddWithValue("@개정일", 개정일);
                    cmd.Parameters.AddWithValue("@담보분류", 담보분류);

                    var exists = await cmd.ExecuteScalarAsync();
                    if (exists != null)
                    {
                        // 메시지창에 공백 데이터가 알아보기 쉽게 표시되도록 괄호 [] 로 감쌌습니다.
                        MessageBox.Show($"[목록 로드 중단]\n이미 테이블({테이블명})에 등록된 상품입니다.\n\n- 상품코드 : [{상품코드}]\n- 개정일 : [{개정일}]\n- 담보분류 : [{담보분류}]",
                                        "중복 데이터 발견", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return true; // 중복이므로 로드 중단
                    }
                }
            }

            return false; // 중복 없음, 로드 계속 진행
        }




        // ===============
        //    분리약관
        // ===============
        private void 분리약관리스트(string[] data)
        {
            grd담보리스트_화면초기화();
            GridView gdview = view담보리스트;

            DataTable table = 담보리스트datatable();
            table.BeginLoadData();
            string 일자 = DateTime.Now.ToString("yyyy-MM-dd");

            for (int i = 0; i < data.Length; i++)
            {
                string[] 분리 = data[i].Split('\t');

                // (방어코드) 데이터 배열 길이가 짧아서 발생하는 런타임 에러 방지
                if (분리.Length < 28) {
                    MessageBox.Show($"데이터 길이가 28 보다작음 : {data[i]} .", "길이오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                } 

                if (String.IsNullOrEmpty(분리[0]) && 분리[10] != "기본")  // 담보PK
                {
                    MessageBox.Show("담보PK는 Empty이거나 Null일수 없습니다.", "PK오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (String.IsNullOrEmpty(분리[2])) // 상품코드
                {
                    MessageBox.Show("상품코드는 Empty이거나 Null일수 없습니다.", "PK오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (String.IsNullOrEmpty(분리[3])) // 개정일
                {
                    MessageBox.Show("개정일은 Empty이거나 Null일수 없습니다.", "PK오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (String.IsNullOrEmpty(분리[6]))
                {
                    MessageBox.Show("담보코드는 Empty이거나 Null일수 없습니다.", "PK오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                table.Rows.Add(new object[]
                {
                    false,                                       // 확인     
                    분리[0],                                     // 담보PK  
                    분리[1],                                     // 고객사코드  
                    분리[2],                                     // 상품명  
                    분리[3],                                     // 상품코드
                    분리[4],                                     // 개정일 
                    분리[5],                                     // 담보분류 
                    분리[6],                                     // 담보명  
                    분리[7],                                     // 담보코드
                    분리[8],                                     // 대표담보코드
                    분리[9],                                     // 세션
                    분리[10],                                    // 구분    
                    분리[11].PadLeft(2, '0'),                    // 형      
                    분리[12].PadLeft(2, '0'),                    // 종      
                    분리[13].PadLeft(2, '0'),                    // 권      
                    분리[14].PadLeft(2, '0'),                    // 편      
                    분리[15].PadLeft(2, '0'),                    // 장      
                    분리[16].PadLeft(2, '0'),                    // 절
                    분리[17].PadLeft(3, '0'),                    // seq
                    분리[18],                                    // 분류01  
                    분리[19],                                    // 분류02  
                    분리[20],                                    // 분류03  
                    분리[21],                                    // 분류04  
                    분리[22],                                    // 실손담보
                    일자,                                        // 일자 (밖에서 미리 계산한 값 사용)   
                    "",                                          // 형식    
                    "",                                          // 담보    
                    분리[26],                                    // sortkey 
                    분리[27],                                    // 일련번호 
                    ""                                           // 파일경로
                });

                if (i % 100 == 0) Application.DoEvents();
            }

            table.EndLoadData();
            gdview.BeginUpdate();

            grd담보리스트.DataSource = table;
            gdview.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);

            gdview.EndUpdate();
        }

        private void view담보리스트_RowStyle(object sender, RowStyleEventArgs e)
        {
            GridView gdview = view담보리스트;
            if (e.RowHandle < 0) { return; }

            if (gdview.GetRowCellDisplayText(e.RowHandle, "확인").ToString() == "Checked")
                e.Appearance.BackColor = Color.SlateBlue;

            if (gdview.GetRowCellDisplayText(e.RowHandle, "담보PK").ToString() == "중복")
                e.Appearance.BackColor = Color.Red;
            if (gdview.GetRowCellDisplayText(e.RowHandle, "상품코드").ToString() == "업로드완료" ||
                gdview.GetRowCellDisplayText(e.RowHandle, "상품명").ToString() == "Update완료") {
                e.Appearance.BackColor = Color.AliceBlue;
                e.Appearance.ForeColor = Color.Red;
            }
        }

        private void view담보_RowStyle(object sender, RowStyleEventArgs e)
        {
            GridView gdview = view담보;
            if (e.RowHandle < 0) { return; }

            if (gdview.GetRowCellDisplayText(e.RowHandle, "상태").ToString() == "미존재" ||
                gdview.GetRowCellDisplayText(e.RowHandle, "상태").ToString() == "중복")
                e.Appearance.BackColor = Color.Red;
        }

        private void frm담보관리_FormClosed(object sender, FormClosedEventArgs e)
        {
            frmEasy약관.담보관리_SetMessage -= new frmEasy약관.담보관리_Handler(event_담보관리_setMessage);
            for (int i = 0; i < Ax02.Length; i++)
            {
                if (Ax02[i] != null)
                {
                    try
                    {
                        Ax02[i].Dispose(); // 컨트롤 파괴
                    }
                    catch
                    {
                        // 파괴 중 혹시 모를 오류는 무시
                    }
                    finally
                    {
                        Ax02[i] = null; // 완벽하게 null 처리
                    }
                }
            }
        }

        private void grd담보리스트_DoubleClick(object sender, EventArgs e)
        {
            if (담보미리보기 != "담보미리보기" || view담보리스트.RowCount <=0)
                return;

            string 담보 = "";
            byte[] pdfByte = null;
            string 형식 = view담보리스트.GetRowCellDisplayText(view담보리스트.FocusedRowHandle, "형식").ToUpper();

            if ( view담보리스트.GetRowCellDisplayText(view담보리스트.FocusedRowHandle, "담보") != "") {
                if (형식 == "HWP")
                    담보 = view담보리스트.GetRowCellDisplayText(view담보리스트.FocusedRowHandle, "담보");
                else if (형식 == "PDF")
                    pdfByte = Convert.FromBase64String(view담보리스트.GetRowCellDisplayText(view담보리스트.FocusedRowHandle, "담보"));
                else
                    return;
            } else {
                return;
            }

            frm담보View 담보View = new frm담보View(담보, pdfByte, 형식);
            담보View.Width = Screen.AllScreens[0].WorkingArea.Width - 100;
            담보View.Height = Screen.AllScreens[0].WorkingArea.Height - 100;

            담보View.StartPosition = FormStartPosition.CenterScreen;
            담보View.ShowDialog();
        }

        private async Task<DataTable> 약관정보목록DT()
        {
            return await Task.Run(() =>
            {
                var table = new DataTable("약관정보목록");

                // 컬럼 정의를 배열로 묶기
                (string name, Type type)[] columns =
                {
                    ("확인", typeof(bool)),
                    ("약관PK", typeof(string)),
                    ("고객사", typeof(string)),
                    ("상품명", typeof(string)),
                    ("상품코드", typeof(string)),
                    ("개정일", typeof(string)),
                    ("종분류코드", typeof(string)),
                    ("작업일", typeof(string)),
                    ("파일경로", typeof(string)),
                    ("URL", typeof(string))

                };

                foreach (var (name, type) in columns)
                    table.Columns.Add(name, type);

                return table;
            });
        }


        private async Task grd약관정보목록View(int 인수)
        {
            try
            {
                if (인수 == 0)
                {
                    DataTable table = null;
                    table = await Task.Run(() => 약관정보목록DT());
                    grd약관정보목록.DataSource = table;
                }

                GridView gdview = new GridView();
                gdview = view약관정보목록;
                gdview.GroupPanelText = "약관정보목록 파일을 올려놓으세요.";
                gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.GroupPanel.ForeColor = System.Drawing.Color.DarkBlue;
                gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
                gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;  // 멀티라인 선택
                gdview.OptionsSelection.MultiSelect = true;
                gdview.Appearance.SelectedRow.BackColor = System.Drawing.Color.White;
                gdview.Appearance.SelectedRow.ForeColor = System.Drawing.Color.Blue;
                gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                gdview.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.Click; //원클릭

                //gdview.Columns["파일경로"].Visible = false;

                int grdwidth = grd약관정보목록.Width;
                // 컬럼 존재 체크 후 사이즈 적용 (안전)
                void SetColWidth(string fieldName, double ratio)
                {
                    var col = gdview.Columns.ColumnByFieldName(fieldName);
                    if (col != null) { col.Width = Convert.ToInt32(grdwidth * ratio); }
                }
                SetColWidth("확인", 0.06);
                SetColWidth("약관PK", 0.1);
                SetColWidth("고객사", 0.05);
                SetColWidth("상품명", 0.4);
                SetColWidth("상품코드", 0.15);
                SetColWidth("개정일", 0.10);
                SetColWidth("종분류코드", 0.10);
                SetColWidth("작업일", 0.10);
                SetColWidth("파일경로", 0.3);
                SetColWidth("URL", 0.3);
            }
            catch (Exception){
            }
        }

        // ===============
        //    전체약관
        // ===============

        private async void grd약관정보목록_DragDrop(object sender, DragEventArgs e)
        {
            string[] 약관정보파일 = null;
            await grd약관정보목록View(1);

            약관정보파일 = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            string[] data = System.IO.File.ReadAllLines(약관정보파일[0], System.Text.Encoding.GetEncoding(949));
            if (data.Length <= 0)
                return;


            GridView gdview = view약관정보목록;
            DataTable table = null;
            table = await Task.Run(() => 약관정보목록DT());

            string 작업일 = DateTime.Now.ToString("yy.MM.dd");
            foreach (string 개별 in data)
            {
                string[] 분리 = 개별.Split('\t', '|');
                if (분리.Length != 5 || 분리.Any(s => string.IsNullOrWhiteSpace(s)))
                {
                    MessageBox.Show("🚫진행불가 올바른 데이터 형식이 아닙니다.", "데이터오류", MessageBoxButtons.OK);
                    return;
                }

                table.Rows.Add(new object[] {
                    false,                            // 확인
                    $"{분리[2]}_{분리[3]}_{분리[4]}",   // 약관PK (상품코드_종분류코드_개정일)
                    분리[0],    // 고객사
                    분리[1],    // 상품명
                    분리[2],    // 상품코드
                    분리[4],    // 개정일
                    분리[3],    // 종분류코드
                    작업일,     // 작업일
                    ""          // 파일경로
                });
            }
            grd약관정보목록.DataSource = table;
            gdview.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 10);

            gdview = view담보; // 양쪽모두 데이터 또는 리스트가 채워져 있는 상태

            if (gdview.RowCount > 0)
            {
                담보관리메인화면event[0] = "버튼활성화";
                담보관리메인화면event[1] = "담보비교";
                담보관리메인화면_SetMessage.Invoke(담보관리메인화면event);
            }
        }

        private void grd약관정보목록_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        public class UploadItem
        {
            public int RowIndex { get; set; }
            public string 담보PK { get; set; }
            public string 담보분류 { get; set; }
            public string 담보명 { get; set; }
            public string 담보코드 { get; set; }
            public string 대표담보코드 { get; set; }
            public string 세션 { get; set; }
            public string 구분 { get; set; }
            public string 형 { get; set; }
            public string 종 { get; set; }
            public string 권 { get; set; }
            public string 편 { get; set; }
            public string 장 { get; set; }
            public string 절 { get; set; }
            public string seq { get; set; }
            public string 분류01 { get; set; }
            public string 분류02 { get; set; }
            public string 분류03 { get; set; }
            public string 분류04 { get; set; }
            public string 실손담보 { get; set; }
            public string 일자 { get; set; }
            public string 형식 { get; set; }
            public string 파일경로 { get; set; }
            public string sortkey { get; set; }
            public string 일련번호 { get; set; }
            public string 업로드결과 { get; set; } = "";
        }

        private class EncodeItem
        {
            public int RowIndex { get; set; }
            public string 파일경로 { get; set; }
            public string 확장자 { get; set; }
            public string 새저장경로 { get; set; }
        }

        private void view약관정보목록_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (중복에러_RowList.Contains(e.RowHandle))
            {
                // 바탕색을 연한 빨간색(LightCoral)으로 변경
                e.Appearance.BackColor = Color.LightCoral;
                e.Appearance.ForeColor = Color.White;
            }
        }
    }

}